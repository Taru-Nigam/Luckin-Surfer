using GameCraft.Data;
using GameCraft.Helpers;
using GameCraft.Models;
using GameCraft.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GameCraft.Controllers
{
    public class PaymentController : Controller
    {
        private readonly GameCraftDbContext _context;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(GameCraftDbContext context, ILogger<PaymentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // POST action to start the product checkout.
        [HttpPost]
        public async Task<IActionResult> StartProductCheckout(int productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found. Please try again.";
                return RedirectToAction("GetCard", "Home");
            }

            // Set TempData with product details.
            TempData["PurchaseProductId"] = product.ProductId;
            TempData["PurchaseProductPrice"] = product.Price.ToString(); // Convert to string
            TempData["PurchaseProductTitle"] = product.Name;

            _logger.LogInformation($"Starting checkout for product ID {product.ProductId}. Redirecting to CardPayment.");

            return RedirectToAction("CardPayment");
        }

        // GET: /Payment/CardPayment
        [HttpGet]
        public async Task<IActionResult> CardPayment()
        {
            if (TempData["PurchaseProductId"] is int productId && TempData["PurchaseProductPrice"] is string productPriceString)
            {
                if (!decimal.TryParse(productPriceString, out decimal productPrice))
                {
                    _logger.LogWarning("Failed to parse product price from TempData.");
                    TempData["ErrorMessage"] = "Invalid product data. Please try again.";
                    return RedirectToAction("GetCard", "Home");
                }

                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
                if (product == null)
                {
                    _logger.LogWarning($"Product with ID {productId} not found for payment selection.");
                    TempData["ErrorMessage"] = "Product not found for payment. Please try again.";
                    return RedirectToAction("GetCard", "Home");
                }

                // Keep TempData for the next action.
                TempData.Keep("PurchaseProductId");
                TempData.Keep("PurchaseProductPrice");
                TempData.Keep("PurchaseProductTitle");

                // Create a DebitCardViewModel and set the product
                var viewModel = new DebitCardViewModel
                {
                    Product = product // Set the product in the view model
                };

                return View(viewModel); // Return the view with the DebitCardViewModel
            }
            else
            {
                _logger.LogWarning("No product ID or price found in TempData for CardPayment. Redirecting to GetCard.");
                TempData["ErrorMessage"] = "No product selected for payment. Please start from the GameCard page.";
                return RedirectToAction("GetCard", "Home");
            }
        }

        // POST action to handle debit card payment submission.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessDebitCardDetails(DebitCardViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state invalid for ProcessDebitCardDetails.");
                return View("CardPayment", model); // Return to CardPayment view with the model
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == model.Product.ProductId);
            if (product == null || product.Price != model.Product.Price)
            {
                _logger.LogError($"Product details mismatch or not found for product ID: {model.Product.ProductId}.");
                ModelState.AddModelError("", "Product details mismatch or not found. Please try again.");
                return View("CardPayment", model); // Return to CardPayment view with the model
            }

            // --- SIMULATE EXTERNAL DEBIT CARD PAYMENT GATEWAY CALL ---
            bool paymentSuccessful = true; // Assume success for demonstration
            if (!paymentSuccessful)
            {
                _logger.LogError("Debit card payment simulation failed.");
                ModelState.AddModelError("", "Debit card payment failed. Please check your details and try again.");
                return View("CardPayment", model); // Return to CardPayment view with the model
            }
            // --- END SIMULATION ---

            _logger.LogInformation($"Debit card payment simulated successfully for product ID: {model.Product.ProductId}.");

            // Redirect to the DebitCardCheckoutController to handle order creation
            return RedirectToAction("ConfirmPayment", "DebitCardCheckout", new { productId = model.Product.ProductId });
        }

        [HttpGet]
        public async Task<IActionResult> PaymentConfirmation(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails) // Include OrderDetails
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction("Index", "Home");
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == order.CustomerId);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found for this order.";
                return RedirectToAction("Index", "Home");
            }

            // Assuming you want to show the first product in the order details
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == order.OrderDetails.First().ProductId);
            if (product == null)
            {
                _logger.LogWarning($"Product not found for order {orderId}.");
                product = new Product { Name = "Purchased Product" }; // Fallback
            }

            var viewModel = new PaymentConfirmationViewModel
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                CustomerName = customer.Name,
                GameCardNumber = customer.GameCraftCardNumber,
                PurchasedProduct = product // Use product instead of promotion
            };

            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
