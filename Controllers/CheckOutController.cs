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
        public async Task<IActionResult> Index(int? productId, int quantity = 1) // Added productId and quantity
        {
            // 1. Check if user is logged in
            var customerEmail = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(customerEmail))
            {
                // Store the current URL to redirect back after login
                TempData["ReturnUrl"] = Url.Action("Index", "Checkout", new { productId = productId, quantity = quantity });
                return RedirectToAction("Login", "Account");
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == customerEmail);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "User account not found. Please log in again.";
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            // Handle "Buy Now" scenario: Add product to cart if productId is provided
            if (productId.HasValue && productId.Value > 0)
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId.Value);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction("Index", "Home"); // Redirect to home if product not found
                }

                // Check if the item already exists in the database cart
                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CustomerId == customer.CustomerId && ci.ProductId == productId.Value);

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity; // Increase quantity if item already in cart
                }
                else
                {
                    var newCartItem = new CartItem
                    {
                        ProductId = product.ProductId,
                        Name = product.Name,
                        Price = product.Price,
                        Quantity = quantity,
                        ImageData = product.ImageData,
                        CustomerId = customer.CustomerId
                    };
                    _context.CartItems.Add(newCartItem);
                }
                await _context.SaveChangesAsync();

                // Update session cart as well
                var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
                var sessionItem = cart.FirstOrDefault(item => item.ProductId == productId.Value);
                if (sessionItem != null)
                {
                    sessionItem.Quantity += quantity;
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        ProductId = product.ProductId,
                        Name = product.Name,
                        Price = product.Price,
                        Quantity = quantity,
                        ImageData = product.ImageData
                    });
                }
                HttpContext.Session.Set("Cart", cart); // Save updated cart to session
            }

            // Retrieve current cart items from the database (primary source for logged-in users)
            var cartItems = await _context.CartItems
                                    .Where(c => c.CustomerId == customer.CustomerId)
                                    .Include(c => c.Product) // Include product details
                                    .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty. Please add items before checking out.";
                return RedirectToAction("Cart", "Cart"); // Redirect to cart if empty, even after "Buy Now" attempt
            }

            // Create CheckoutViewModel and pre-fill with customer's details
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
            ViewBag.CartItems = cartItems;
            ViewBag.CartTotal = cartItems.Sum(item => item.Price * item.Quantity);

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
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == customerEmail);
                var cartItems = await _context.CartItems
                                        .Where(c => c.CustomerId == customer.CustomerId)
                                        .Include(c => c.Product)
                                        .ToListAsync();
                ViewBag.CartItems = cartItems;
                ViewBag.CartTotal = cartItems.Sum(item => item.Price * item.Quantity);
                return View("Checkout", model); // Changed from "Index" to "Checkout"
            }

            // 3. Retrieve cart items and customer from DB
            var customerLoggedIn = await _context.Customers.FirstOrDefaultAsync(c => c.Email == customerEmail);
            if (customerLoggedIn == null)
            {
                TempData["ErrorMessage"] = "User account not found. Please log in again.";
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            var cartItemsFromDb = await _context.CartItems
                                        .Where(c => c.CustomerId == customerLoggedIn.CustomerId)
                                        .ToListAsync();

            if (!cartItemsFromDb.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty. Please add items before checking out.";
                return RedirectToAction("Cart", "Cart");
            }

            // Calculate total amount (CRITICAL: Recalculate based on current product prices)
            decimal totalAmount = 0;
            foreach (var item in cartItemsFromDb)
            {
                // Get the *current* price from the database to prevent tampering
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                if (product == null)
                {
                    ModelState.AddModelError("", $"Product '{item.Name}' not found in our current catalog.");
                    ViewBag.CartItems = cartItemsFromDb; // Pass cart items back for display
                    ViewBag.CartTotal = cartItemsFromDb.Sum(ci => ci.Price * ci.Quantity); // Calculate total using cart item price for re-render
                    return View("Checkout", model); // Changed from "Index" to "Checkout"
                }
                totalAmount += product.Price * item.Quantity; // Use DB price for total
            }

            // 4. Check if customer has enough PrizePoints (assuming Customer model has PrizePoints)
            if (customerLoggedIn.PrizePoints < totalAmount)
            {
                ModelState.AddModelError("", $"Insufficient PrizePoints. You need {totalAmount:F2} Tickets but only have {customerLoggedIn.PrizePoints:F2}.");
                ViewBag.CartItems = cartItemsFromDb;
                ViewBag.CartTotal = totalAmount;
                return View("Checkout", model); // Changed from "Index" to "Checkout"
            }

            // Deduct PrizePoints from customer
            customerLoggedIn.PrizePoints -= (int)totalAmount;
            _context.Customers.Update(customerLoggedIn);


            // 5. Create new Order (Your existing simple Order model)
            var order = new Order
            {
                CustomerId = customerLoggedIn.CustomerId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = totalAmount,
                // Shipping details from CheckoutViewModel are NOT stored in your current Order model
                // Status property is also missing in your provided Order.cs, so we cannot set it.
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Save order to get OrderId

            // 6. Create OrderDetails (Your existing simple OrderDetail model)
            foreach (var item in cartItemsFromDb)
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

            // 7. Clear the cart from the database
            _context.CartItems.RemoveRange(cartItemsFromDb);
            await _context.SaveChangesAsync();

            // 8. Update session with new PrizePoints
            HttpContext.Session.SetString("PrizePoints", customerLoggedIn.PrizePoints.ToString());

            // 9. Clear the cart session
            HttpContext.Session.Remove("Cart");

            // 10. Redirect to a success page or home
            TempData["SuccessMessage"] = "Your order has been placed successfully!";
            // Passing shipping details and order details via TempData/Session for Confirmation page
            TempData.Set("OrderShippingDetails", model); // Store shipping details
            TempData.Set("OrderPlacedDetails", order); // Store basic order (for ID, total)
            TempData.Set("OrderProducts", cartItemsFromDb.Select(ci => new { ci.ProductId, ci.Name }).ToList()); // Store product names temporarily

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

            var orderDetails = await _context.OrderDetails
                                        .Where(od => od.OrderId == orderId)
                                        .ToListAsync();

            var productIds = orderDetails.Select(od => od.ProductId).ToList();
            var products = await _context.Products.Where(p => productIds.Contains(p.ProductId)).ToListAsync();
            var productMap = products.ToDictionary(p => p.ProductId, p => p.Name);

            var shippingDetails = TempData.Get<CheckoutViewModel>("OrderShippingDetails");

            ViewBag.Order = order;
            ViewBag.OrderDetails = orderDetails;
            ViewBag.ProductNames = productMap;
            ViewBag.ShippingDetails = shippingDetails;

            return View(order);
        }
    }
}
