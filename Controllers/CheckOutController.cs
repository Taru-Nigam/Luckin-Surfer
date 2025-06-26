using GameCraft.Data;
using GameCraft.Helpers;
using GameCraft.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Needed for FirstOrDefaultAsync, FindAsync
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameCraft.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly GameCraftDbContext _context;

        public CheckoutController(GameCraftDbContext context)
        {
            _context = context;
        }

        // GET: /Checkout/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // 1. Check if user is logged in
            var customerEmail = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(customerEmail))
            {
                TempData["ErrorMessage"] = "Please log in to proceed to checkout.";
                return RedirectToAction("Login", "Account");
            }

            // 2. Retrieve current cart items from session
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();

            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty. Please add items before checking out.";
                return RedirectToAction("Cart", "Cart");
            }

            // 3. Get customer details to pre-fill checkout form
            // NOTE: Customer model MUST have properties for Name, Email, Phone, Address, City, PostCode
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == customerEmail);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "User account not found. Please log in again.";
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            // Create CheckoutViewModel and pre-fill with customer's details
            // These details will be collected but NOT saved to your current Order model
            var viewModel = new CheckoutViewModel
            {
                ShippingName = customer.Name,
                ShippingEmail = customer.Email,
                ShippingPhone = customer.Phone,
                ShippingAddress = customer.Address,
                ShippingCity = customer.City,
                ShippingPostCode = customer.PostCode
            };

            // Pass cart items and total to the view
            ViewBag.CartItems = cart;
            ViewBag.CartTotal = cart.Sum(item => item.Price * item.Quantity);

            return View("Checkout", viewModel);
        }

        // POST: /Checkout/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            // 1. Check if user is logged in
            var customerEmail = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(customerEmail))
            {
                TempData["ErrorMessage"] = "Please log in to proceed to checkout.";
                return RedirectToAction("Login", "Account");
            }

            // 2. Validate form data
            if (!ModelState.IsValid)
            {
                var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
                ViewBag.CartItems = cart;
                ViewBag.CartTotal = cart.Sum(item => item.Price * item.Quantity);
                return View("Index", model);
            }

            // 3. Retrieve cart items and customer from DB
            var cartItems = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty. Please add items before checking out.";
                return RedirectToAction("Cart", "Cart");
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == customerEmail);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "User account not found. Please log in again.";
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            // Calculate total amount (CRITICAL: Recalculate based on current product prices)
            decimal totalAmount = 0;
            foreach (var item in cartItems)
            {
                // Get the *current* price from the database to prevent tampering
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                if (product == null)
                {
                    ModelState.AddModelError("", $"Product '{item.Name}' not found in our current catalog.");
                    ViewBag.CartItems = cartItems; // Pass cart items back for display
                    ViewBag.CartTotal = cartItems.Sum(ci => ci.Price * ci.Quantity); // Calculate total using cart item price for re-render
                    return View("Index", model);
                }
                totalAmount += product.Price * item.Quantity; // Use DB price for total
            }

            // 4. Check if customer has enough PrizePoints (assuming Customer model has PrizePoints)
            if (customer.PrizePoints < totalAmount)
            {
                ModelState.AddModelError("", $"Insufficient PrizePoints. You need {totalAmount:F2} Tickets but only have {customer.PrizePoints:F2}.");
                ViewBag.CartItems = cartItems;
                ViewBag.CartTotal = totalAmount;
                return View("Index", model);
            }

            // 5. Create new Order (Your existing simple Order model)
            var order = new Order
            {
                CustomerId = customer.CustomerId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalAmount,
                // Shipping details from CheckoutViewModel are NOT stored in your current Order model
                // Status property is also missing in your provided Order.cs, so we cannot set it.
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Save order to get OrderId

            // 6. Create OrderDetails (Your existing simple OrderDetail model)
            foreach (var item in cartItems)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price // Use price from cart, or re-fetch from DB product if stricter
                };
                _context.OrderDetails.Add(orderDetail);
            }
            await _context.SaveChangesAsync();

            // 7. Deduct PrizePoints from customer
            HttpContext.Session.SetString("PrizePoints", customer.PrizePoints.ToString());

            // 8. Update session with new PrizePoints
            HttpContext.Session.SetString("PrizePoints", customer.PrizePoints.ToString());

            // 9. Clear the cart session
            HttpContext.Session.Remove("Cart");

            // 10. Redirect to a success page or home
            TempData["SuccessMessage"] = "Your order has been placed successfully!";
            // Passing shipping details and order details via TempData/Session for Confirmation page
            // This is a workaround because Order and OrderDetail models lack navigation properties
            TempData.Set("OrderShippingDetails", model); // Store shipping details
            TempData.Set("OrderPlacedDetails", order); // Store basic order (for ID, total)
            TempData.Set("OrderProducts", cartItems.Select(ci => new { ci.ProductId, ci.Name }).ToList()); // Store product names temporarily

            return RedirectToAction("OrderConfirmation", new { orderId = order.OrderId });
        }

        // GET: /Checkout/OrderConfirmation
        [HttpGet]
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction("Index", "Home");
            }

            // Since OrderDetail doesn't have a navigation property to Product,
            // we have to fetch product details separately for each order detail.
            var orderDetails = await _context.OrderDetails
                                        .Where(od => od.OrderId == orderId)
                                        .ToListAsync();

            // To get product names, we'll fetch them based on ProductId
            var productIds = orderDetails.Select(od => od.ProductId).ToList();
            var products = await _context.Products.Where(p => productIds.Contains(p.ProductId)).ToListAsync();
            var productMap = products.ToDictionary(p => p.ProductId, p => p.Name);

            // Retrieve shipping details that were passed via TempData
            var shippingDetails = TempData.Get<CheckoutViewModel>("OrderShippingDetails");

            // Pass data to view
            ViewBag.Order = order; // Basic order info
            ViewBag.OrderDetails = orderDetails; // List of order details
            ViewBag.ProductNames = productMap; // Map of ProductId to Name
            ViewBag.ShippingDetails = shippingDetails; // Shipping details (if available from TempData)

            return View(order); // Pass the order model, but use ViewBags for other data
        }
    }
}