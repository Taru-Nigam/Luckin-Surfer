using GameCraft.Data;
using GameCraft.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace GameCraft.Controllers
{
    public class DebitCardCheckoutController : Controller
    {
        private readonly GameCraftDbContext _context;

        public DebitCardCheckoutController(GameCraftDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int productId)
        {
            var customerEmail = HttpContext.Session.GetString("Email");
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == customerEmail);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "User  account not found. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "No product found for purchase. Please try again.";
                return RedirectToAction("GetCard", "Home");
            }

            var totalAmount = product.Price;

            var order = new Order
            {
                CustomerId = customer.CustomerId,
                TotalAmount = totalAmount,
                OrderDate = DateTime.UtcNow,
                ShippingName = customer.Name,
                ShippingEmail = customer.Email,
                ShippingPhone = customer.Phone,
                ShippingPostCode = customer.PostCode,
                ShippingAddress = customer.Address,
                ShippingCity = customer.City,
                OrderDetails = new List<OrderDetail> // Initialize OrderDetails
                {
                    new OrderDetail
                    {
                        ProductId = productId, // Set the ProductId for the order detail
                        Quantity = 1, // Assuming quantity is 1 for a single product purchase
                        UnitPrice = product.Price // Set the unit price
                    }
                }
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // --- CRITICAL NEW LOGIC: GENERATE AND SAVE CARD NUMBER ---
            var random = new Random();
            string gameCardNumber = string.Join("", Enumerable.Range(0, 16).Select(_ => random.Next(0, 10)));

            customer.GameCraftCardNumber = gameCardNumber;

            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
            // --- END NEW LOGIC ---

            TempData["SuccessMessage"] = "Debit card payment processed successfully! Your GameCraft Card has been purchased.";

            return RedirectToAction("PaymentConfirmation", "Payment", new { orderId = order.OrderId });
        }
    }
}
