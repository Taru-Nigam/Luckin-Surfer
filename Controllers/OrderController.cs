using GameCraft.Data;
using GameCraft.Models; // Assuming your Order model is here
using GameCraft.Services; // Include the email service
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GameCraft.Controllers
{
    public class OrderController : Controller
    {
        private readonly GameCraftDbContext _context; // Your database context
        private readonly IEmailService _emailService; // Email service for sending emails

        public OrderController(GameCraftDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: /Order/OrderDetail/{id}
        public async Task<IActionResult> OrderDetail(int id)
        {
            // Fetch the specific order from your database
            var order = await _context.Orders
                .Include(o => o.OrderDetails) // Include related order details
                .ThenInclude(od => od.Product) // Include product info if needed for details
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound(); // Or redirect to an error page
            }

            return View(order); // Pass the order object to your OrderDetail.cshtml view
        }

        // POST: /Order/CreateOrder
        [HttpPost]
        public async Task<IActionResult> CreateOrder(Order order)
        {
            if (!ModelState.IsValid)
            {
                return View(order); // Return to the view with validation errors
            }

            // Set the order date and customer ID
            order.OrderDate = DateTime.UtcNow;
            // Assuming you have a way to get the current user's ID
            var customerEmail = HttpContext.Session.GetString("Email");
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == customerEmail);
            if (customer != null)
            {
                order.CustomerId = customer.CustomerId;
            }

            // Add the order to the database
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Send confirmation email
            await SendOrderConfirmationEmail(order, customer);

            return RedirectToAction("OrderConfirmation", new { orderId = order.OrderId });
        }

        // GET: /Order/OrderConfirmation/{orderId}
        [HttpGet]
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails) // Include OrderDetails
                .ThenInclude(od => od.Product) // Include Product details
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

            var viewModel = new OrderSummaryViewModel
            {
                OrderId = order.OrderId,
                TotalAmount = order.TotalAmount,
                ShippingName = customer.Name,
                ShippingEmail = customer.Email,
                ShippingPhone = customer.Phone,
                ShippingAddress = customer.Address,
                ShippingCity = customer.City,
                ShippingPostCode = customer.PostCode
            };

            // Send confirmation email
            await SendOrderConfirmationEmail(order, customer);

            return View(viewModel);
        }

        // Method to send order confirmation email
        private async Task SendOrderConfirmationEmail(Order order, Customer customer)
        {
            var emailSubject = "Order Confirmation - Your Purchase at GameCraft";
            var emailMessage = $"<h1>Thank You for Your Order!</h1>" +
                               $"<p>Your order has been successfully placed.</p>" +
                               $"<p><strong>Order ID:</strong> {order.OrderId}</p>" +
                               $"<p><strong>Total Amount:</strong> ${order.TotalAmount:F2}</p>" +
                               $"<p><strong>Billed To:</strong> {customer.Name}</p>" +
                               $"<p><strong>Email:</strong> {customer.Email}</p>" +
                               $"<p><strong>Phone:</strong> {customer.Phone}</p>" +
                               $"<p><strong>Address:</strong> {customer.Address}</p>" +
                               $"<p><strong>City:</strong> {customer.City}</p>" +
                               $"<p><strong>Postcode:</strong> {customer.PostCode}</p>";

            await _emailService.SendEmailAsync(customer.Email, emailSubject, emailMessage);
        }

        // Error handling
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
