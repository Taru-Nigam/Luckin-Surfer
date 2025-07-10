using GameCraft.Data;
using GameCraft.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore; // Needed for Include
using Newtonsoft.Json; // For JSON serialization

namespace GameCraft.Controllers
{
    public class CartController : Controller
    {
        private readonly GameCraftDbContext _context;

        public CartController(GameCraftDbContext context)
        {
            _context = context;
        }

        // Helper method to get the current cart from the database
        private List<CartItem> GetCartFromDatabase(int customerId)
        {
            return _context.CartItems
                .Where(c => c.CustomerId == customerId)
                .Include(c => c.Product) // Include product details
                .ToList();
        }

        // Helper method to get the current cart from session
        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }
            return JsonConvert.DeserializeObject<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        // Helper method to save the cart to session
        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(cart));
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            var customerEmail = HttpContext.Session.GetString("Email");
            var customer = _context.Customers.FirstOrDefault(c => c.Email == customerEmail);

            if (customer == null)
            {
                // Store the current URL to redirect back after login
                TempData["ReturnUrl"] = Url.Action("Details", "Product", new { id = productId });
                return Json(new { success = false, redirectToLogin = true, message = "Please log in to add items to cart." });
            }

            var product = _context.Products.FirstOrDefault(p => p.ProductId == productId);
            if (product == null)
            {
                return NotFound(new { success = false, message = "Product not found." });
            }

            // Check if the item already exists in the database cart
            var existingItem = _context.CartItems
                .FirstOrDefault(ci => ci.CustomerId == customer.CustomerId && ci.ProductId == productId);

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
                    ImageData = product.ImageData, // Use ImageData instead of ImageUrl
                    CustomerId = customer.CustomerId // Set the CustomerId
                };
                _context.CartItems.Add(newCartItem);
            }

            _context.SaveChanges(); // Save changes to the database

            // Update session cart as well (though database is primary for logged-in users)
            var cart = GetCart();
            var sessionItem = cart.FirstOrDefault(item => item.ProductId == productId);
            if (sessionItem != null)
            {
                sessionItem.Quantity += quantity; // Increase quantity in session cart
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    Price = product.Price,
                    Quantity = quantity,
                    ImageData = product.ImageData // Use ImageData instead of ImageUrl
                });
            }
            SaveCart(cart); // Save updated cart to session

            return Json(new { success = true, cartCount = _context.CartItems.Count(ci => ci.CustomerId == customer.CustomerId) });
        }

        // GET: /Cart/Cart
        public IActionResult Cart()
        {
            var customerEmail = HttpContext.Session.GetString("Email");
            var customer = _context.Customers.FirstOrDefault(c => c.Email == customerEmail);

            if (customer == null)
            {
                TempData["ReturnUrl"] = Url.Action("Cart", "Cart");
                TempData["ErrorMessage"] = "Please log in to view your cart."; // Keep this message for direct cart access
                return RedirectToAction("Login", "Account");
            }

            var cartItems = GetCartFromDatabase(customer.CustomerId);
            ViewBag.CartItems = cartItems; // Pass cart items to the view
            return View(cartItems);
        }

        // POST: /Cart/RemoveFromCart
        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var customerEmail = HttpContext.Session.GetString("Email");
            var customer = _context.Customers.FirstOrDefault(c => c.Email == customerEmail);

            if (customer == null)
            {
                return NotFound(new { success = false, message = "User not found." });
            }

            var itemToRemove = _context.CartItems
                .FirstOrDefault(item => item.CustomerId == customer.CustomerId && item.ProductId == productId);
            if (itemToRemove != null)
            {
                _context.CartItems.Remove(itemToRemove);
                _context.SaveChanges();
            }

            // Update session cart
            var cart = GetCart();
            var sessionItemToRemove = cart.FirstOrDefault(item => item.ProductId == productId);
            if (sessionItemToRemove != null)
            {
                cart.Remove(sessionItemToRemove);
                SaveCart(cart);
            }

            return Json(new { success = true, cartCount = _context.CartItems.Count(ci => ci.CustomerId == customer.CustomerId) });
        }

        // POST: /Cart/UpdateCartQuantity
        [HttpPost]
        public IActionResult UpdateCartQuantity(int productId, int newQuantity)
        {
            var customerEmail = HttpContext.Session.GetString("Email");
            var customer = _context.Customers.FirstOrDefault(c => c.Email == customerEmail);

            if (customer == null)
            {
                return NotFound(new { success = false, message = "User not found." });
            }

            var itemToUpdate = _context.CartItems
                .FirstOrDefault(item => item.CustomerId == customer.CustomerId && item.ProductId == productId);
            if (itemToUpdate != null)
            {
                if (newQuantity <= 0)
                {
                    _context.CartItems.Remove(itemToUpdate);
                }
                else
                {
                    itemToUpdate.Quantity = newQuantity;
                }
                _context.SaveChanges();
            }

            // Update session cart
            var cart = GetCart();
            var sessionItemToUpdate = cart.FirstOrDefault(item => item.ProductId == productId);
            if (sessionItemToUpdate != null)
            {
                if (newQuantity <= 0)
                {
                    cart.Remove(sessionItemToUpdate);
                }
                else
                {
                    sessionItemToUpdate.Quantity = newQuantity;
                }
                SaveCart(cart);
            }

            return Json(new { success = true, cartCount = _context.CartItems.Count(ci => ci.CustomerId == customer.CustomerId) });
        }

        // Action to get the current cart count for initial load or manual refresh
        [HttpGet]
        public IActionResult GetCartCount()
        {
            var customerEmail = HttpContext.Session.GetString("Email");
            var customer = _context.Customers.FirstOrDefault(c => c.Email == customerEmail);

            if (customer == null)
            {
                return Json(new { cartCount = 0 });
            }

            var cartCount = _context.CartItems.Count(ci => ci.CustomerId == customer.CustomerId);
            return Json(new { cartCount });
        }
    }
}
