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
                TempData["ErrorMessage"] = "User  account not found. Please log in again.";
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

                // Log the addition to cart activity
                var auditLog = new AuditLog
                {
                    UserId = customer.CustomerId.ToString(),
                    UserName = customer.Name,
                    Action = "Add to Cart",
                    Details = $"User  added {quantity} of {product.Name} to cart.",
                    Timestamp = DateTime.UtcNow,
                    UserRole = "Customer"
                };
                _context.AuditLogs.Add(auditLog);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var customerEmail = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(customerEmail))
            {
                TempData["ErrorMessage"] = "Please log in to proceed to checkout.";
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                // reload cart for redisplay
                var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
                ViewBag.CartItems = cart;
                ViewBag.CartTotal = cart.Sum(i => i.Price * i.Quantity);
                return View("Checkout", model);
            }

            var cartItems = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("Cart", "Cart");
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == customerEmail);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "User  account not found.";
                return RedirectToAction("Login", "Account");
            }

            decimal totalAmount = 0;
            foreach (var item in cartItems)
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                if (product == null)
                {
                    ModelState.AddModelError("", $"Product {item.Name} no longer exists.");
                    ViewBag.CartItems = cartItems;
                    ViewBag.CartTotal = cartItems.Sum(i => i.Price * i.Quantity);
                    return View("Checkout", model);
                }
                totalAmount += product.Price * item.Quantity;
            }

            if (customer.PrizePoints < totalAmount)
            {
                ModelState.AddModelError("", "Not enough PrizePoints.");
                ViewBag.CartItems = cartItems;
                ViewBag.CartTotal = totalAmount;
                return View("Checkout", model);
            }

            var order = new Order
            {
                CustomerId = customer.CustomerId,
                TotalAmount = totalAmount,
                OrderDate = DateTime.UtcNow,

                ShippingName = model.ShippingName,
                ShippingEmail = model.ShippingEmail,
                ShippingPhone = model.ShippingPhone,
                ShippingPostCode = model.ShippingPostCode,
                ShippingAddress = model.ShippingAddress,
                ShippingCity = model.ShippingCity
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                var detail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                };
                _context.OrderDetails.Add(detail);
            }
            await _context.SaveChangesAsync();

            customer.PrizePoints -= (int)totalAmount;
            HttpContext.Session.SetString("PrizePoints", customer.PrizePoints.ToString());
            await _context.SaveChangesAsync();

            // Log the order placement activity
            var auditLog = new AuditLog
            {
                UserId = customer.CustomerId.ToString(),
                UserName = customer.Name,
                Action = "Place Order",
                Details = $"User  placed an order with total amount: {totalAmount}.",
                Timestamp = DateTime.UtcNow,
                UserRole = "Customer"
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("Cart");
            var dbCartItems = _context.CartItems.Where(ci => ci.CustomerId == customer.CustomerId).ToList();
            _context.CartItems.RemoveRange(dbCartItems);
            await _context.SaveChangesAsync();

            return RedirectToAction("OrderConfirmation", new { orderId = order.OrderId });
        }

        // GET: /Checkout/OrderConfirmation
        [HttpGet]
        public IActionResult OrderConfirmation(int orderId)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order == null)
            {
                return NotFound();
            }

            var viewModel = new OrderSummaryViewModel
            {
                OrderId = order.OrderId,
                ShippingName = order.ShippingName,
                ShippingEmail = order.ShippingEmail,
                ShippingPhone = order.ShippingPhone,
                ShippingPostCode = order.ShippingPostCode,
                ShippingAddress = order.ShippingAddress,
                ShippingCity = order.ShippingCity,
                TotalAmount = order.TotalAmount
            };

            // Log the order confirmation activity
            var auditLog = new AuditLog
            {
                UserId = order.CustomerId.ToString(),
                UserName = order.ShippingName,
                Action = "Order Confirmation",
                Details = $"User  confirmed order with ID: {order.OrderId}.",
                Timestamp = DateTime.UtcNow,
                UserRole = "Customer"
            };
            _context.AuditLogs.Add(auditLog);
            _context.SaveChanges();

            return View(viewModel);
        }
    }
}
