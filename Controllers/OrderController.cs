using GameCraft.Data;
using GameCraft.Models; // Assuming your Order model is here
using Microsoft.AspNetCore.Mvc;
using System.Linq; // For LINQ queries
using Microsoft.EntityFrameworkCore; // <--- Add this line

namespace GameCraft.Controllers
{
    public class OrderController : Controller
    {
        private readonly GameCraftDbContext _context; // Your database context

        public OrderController(GameCraftDbContext context)
        {
            _context = context;
        }

        // This action method is what the OrderDetail.cshtml page will be linked to
        public IActionResult OrderDetail(int id)
        {
            // Fetch the specific order from your database
            // Include OrderDetails if you need to display individual items
            var order = _context.Orders
                                 .Include(o => o.OrderDetails) // Make sure to include related order details
                                 .ThenInclude(od => od.Product) // Include product info if needed for details
                                 .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound(); // Or redirect to an error page
            }

            return View(order); // Pass the order object to your OrderDetail.cshtml view
        }

        // ... potentially other actions for OrderHistory, etc.
    }
}