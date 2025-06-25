using Microsoft.AspNetCore.Mvc;
using GameCraft.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json; // Ensure you have installed the Newtonsoft.Json NuGet package
using System.Collections.Generic;
using System.Linq;
using GameCraft.Data; // Assuming your DbContext is here

namespace GameCraft.Controllers
{
    public class CartController : Controller
    {
        private readonly GameCraftDbContext _context;

        public CartController(GameCraftDbContext context)
        {
            _context = context;
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
            var product = _context.Products.FirstOrDefault(p => p.ProductId == productId);
            if (product == null)
            {
                return NotFound(new { success = false, message = "Product not found." });
            }

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(item => item.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity; // Increase quantity if item already in cart
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    Name = product.Name,       // Use 'Name' as per your CartItem model
                    Price = product.Price,
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl // Assuming your Product model has an ImageUrl property
                });
            }

            SaveCart(cart);

            return Json(new { success = true, cartCount = cart.Sum(item => item.Quantity) });
        }

        // GET: /Cart/Cart
        public IActionResult Cart()
        {
            var userName = HttpContext.Session.GetString("UserName");

            if (string.IsNullOrEmpty(userName))
            {
                TempData["ReturnUrl"] = Url.Action("Cart", "Cart");
                TempData["ErrorMessage"] = "Please log in to view your cart.";
                return RedirectToAction("Login", "Account");
            }

            var cart = GetCart();
            return View(cart);
        }

        // POST: /Cart/RemoveFromCart
        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCart();
            var itemToRemove = cart.FirstOrDefault(item => item.ProductId == productId);
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                SaveCart(cart);
            }
            return Json(new { success = true, cartCount = cart.Sum(item => item.Quantity) });
        }

        // POST: /Cart/UpdateCartQuantity
        [HttpPost]
        public IActionResult UpdateCartQuantity(int productId, int newQuantity)
        {
            var cart = GetCart();
            var itemToUpdate = cart.FirstOrDefault(item => item.ProductId == productId);
            if (itemToUpdate != null)
            {
                if (newQuantity <= 0)
                {
                    cart.Remove(itemToUpdate);
                }
                else
                {
                    itemToUpdate.Quantity = newQuantity;
                }
                SaveCart(cart);
            }
            return Json(new { success = true, cartCount = cart.Sum(item => item.Quantity) });
        }

        // Action to get the current cart count for initial load or manual refresh
        [HttpGet]
        public IActionResult GetCartCount()
        {
            var cart = GetCart();
            return Json(new { cartCount = cart.Sum(item => item.Quantity) });
        }
    }
}