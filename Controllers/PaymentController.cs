using GameCraft.Data;
using GameCraft.Helpers;
using GameCraft.Models;
using GameCraft.Services;
using GameCraft.ViewModels; // Added using directive for ViewModels
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // For HttpContext.Session
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameCraft.Controllers
{
    public class PaymentController : Controller
    {
        private readonly GameCraftDbContext _context;
        private readonly ILogger<PaymentController> _logger;
        private readonly IEmailService _emailService;

        public PaymentController(GameCraftDbContext context, ILogger<PaymentController> logger, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        // POST action to start the product checkout.
        [HttpPost]
        public async Task<IActionResult> StartCardCheckout(int cardId)
        {
            var card = await _context.Cards.FirstOrDefaultAsync(c => c.CardId == cardId);
            if (card == null)
            {
                TempData["ErrorMessage"] = "Card not found. Please try again.";
                return RedirectToAction("Catalog", "Card");
            }

            // Assuming the user is logged in, you would get their email from the session
            var customerEmail = HttpContext.Session.GetString("Email");

            if (string.IsNullOrEmpty(customerEmail))
            {
                // Store the current URL to redirect the user back here after login
                TempData["ReturnUrl"] = Url.Action("Details", "Card", new { id = cardId });
                return RedirectToAction("Login", "Account");
            }

            // Set TempData with the card ID to be used in the CardPayment action.
            // This is the corrected flow, as the view model cannot be passed directly
            // across a redirect.
            TempData["PurchaseCardId"] = cardId;
            TempData["PurchaseCardPrice"] = card.Price.ToString();
            TempData["PurchaseCardName"] = card.Name;


            // Redirect to the debit card payment page
            return RedirectToAction("CardPayment");
        }

        // GET: /Payment/CardPayment
        [HttpGet]
        public async Task<IActionResult> CardPayment()
        {
            // Retrieve card information from TempData
            if (TempData["PurchaseCardId"] is int cardId && TempData["PurchaseCardPrice"] is string cardPriceString)
            {
                if (!decimal.TryParse(cardPriceString, out decimal cardPrice))
                {
                    _logger.LogWarning("Failed to parse card price from TempData.");
                    TempData["ErrorMessage"] = "Invalid card data. Please try again.";
                    return RedirectToAction("Catalog", "Card");
                }

                var card = await _context.Cards.FirstOrDefaultAsync(c => c.CardId == cardId);
                if (card == null)
                {
                    _logger.LogWarning($"Card with ID {cardId} not found for payment selection.");
                    TempData["ErrorMessage"] = "Card not found for payment. Please try again.";
                    return RedirectToAction("Catalog", "Card");
                }

                // Keep TempData for the next action.
                TempData.Keep("PurchaseCardId");
                TempData.Keep("PurchaseCardPrice");
                TempData.Keep("PurchaseCardName");


                // Create a DebitCardViewModel and set the card
                var viewModel = new DebitCardViewModel
                {
                    Card = card, // Set the card in the view model
                    Email = HttpContext.Session.GetString("Email") // Pre-fill email from session
                };

                return View(viewModel); // Return the view with the DebitCardViewModel
            }
            else
            {
                _logger.LogWarning("No card ID or price found in TempData for CardPayment. Redirecting to Catalog.");
                TempData["ErrorMessage"] = "No card selected for payment. Please start from the Cards Catalog page.";
                return RedirectToAction("Catalog", "Card");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessDebitCardDetails(DebitCardViewModel model)
        {
            // Manual validation
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                errors.Add("Email is required.");
            }
            if (string.IsNullOrWhiteSpace(model.CardNumber))
            {
                errors.Add("Card Number is required.");
            }
            if (string.IsNullOrWhiteSpace(model.CardholderName))
            {
                errors.Add("Cardholder Name is required.");
            }
            if (model.ExpiryMonth < 1 || model.ExpiryMonth > 12)
            {
                errors.Add("Valid Expiry Month (MM) is required.");
            }
            if (model.ExpiryYear < DateTime.UtcNow.Year)
            {
                errors.Add("Valid Expiry Year (YYYY) is required.");
            }
            if (string.IsNullOrWhiteSpace(model.CVV) || !int.TryParse(model.CVV, out _))
            {
                errors.Add("CVV is required.");
            }

            // If there are validation errors, return to the CardPayment view with the model and errors
            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                }
                return View("CardPayment", model); // Return to CardPayment view with the model
            }

            // Fetch the card from the database to verify the price
            var card = await _context.Cards.FirstOrDefaultAsync(c => c.CardId == model.Card.CardId);
            if (card == null || card.Price != model.Card.Price)
            {
                _logger.LogError($"Card details mismatch or not found for card ID: {model.Card.CardId}.");
                ModelState.AddModelError("", "Card details mismatch or not found. Please try again.");
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

            // Generate a unique game card number based on the card type
            string gameCardNumber;
            int prizePoints = 0;
            if (card.Name == "Silver GameCraft Card")
            {
                prizePoints = 300;
                gameCardNumber = "SV" + new Random().Next(100000000, 999999999).ToString(); // SV followed by 9 digits
            }
            else if (card.Name == "Gold GameCraft Card")
            {
                prizePoints = 800;
                gameCardNumber = "GD" + new Random().Next(100000000, 999999999).ToString(); // GD followed by 9 digits
            }
            else
            {
                gameCardNumber = new string(Enumerable.Range(0, 9).Select(_ => (char)('0' + new Random().Next(0, 10))).ToArray());
            }

            // Save the game card number to the customer
            var customerEmail = model.Email; // Get the email from the model
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == customerEmail);
            if (customer != null)
            {
                customer.GameCraftCardNumber = gameCardNumber;
                customer.PrizePoints += prizePoints; // Add prize points to the customer
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();
            }

            // Create an order after successful payment simulation
            var order = new Order
            {
                CustomerId = customer.CustomerId,
                TotalAmount = card.Price,
                OrderDate = DateTime.UtcNow,
                CardOrderDetails = new List<CardOrderDetail>
                {
                    new CardOrderDetail
                    {
                        CardId = card.CardId, // Assuming CardId is also the ProductId
                        Quantity = 1, // Assuming quantity is 1 for a single product purchase
                        UnitPrice = card.Price
                    }
                }
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Save the order to the database

            // Send confirmation email
            await _emailService.SendEmailAsync(customerEmail, "Thank you for your GameCraft Card purchase!",
                $"Thank you for purchasing your GameCraft Card! Your game card number is: {gameCardNumber}");

            // Log the purchase activity
            var auditLog = new AuditLog
            {
                UserId = customer.CustomerId.ToString(),
                UserName = customer.Name,
                Action = "Purchased Game Card",
                Details = $"User purchased a game card with number: {gameCardNumber}.",
                Timestamp = DateTime.UtcNow,
                UserRole = "Customer"
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Debit card payment simulated successfully for card ID: {model.Card.CardId}.");

            // Redirect to the PaymentConfirmation page with the new order ID
            return RedirectToAction("PaymentConfirmation", "Payment", new { orderId = order.OrderId });
        }


        [HttpGet]
        public async Task<IActionResult> PaymentConfirmation(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.CardOrderDetails) // Include CardOrderDetails
                .ThenInclude(cod => cod.Card) // Include Card details
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction("Catalog", "Card");
            }

            // Get the card details from the order details
            var purchasedCard = order.CardOrderDetails.FirstOrDefault()?.Card; // Use Card instead of Product
            if (purchasedCard == null)
            {
                TempData["ErrorMessage"] = "Purchased card details not found.";
                return RedirectToAction("Catalog", "Card");
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == order.CustomerId);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found for this order.";
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new PaymentConfirmationViewModel
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                CustomerName = customer.Name,
                GameCardNumber = customer.GameCraftCardNumber,
                PurchasedProduct = purchasedCard // Use purchasedCard instead of a new product object
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
