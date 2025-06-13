using GameCraft.Data;
using GameCraft.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace GameCraft.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GameCraftDbContext _context;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Check if the user is an admin
            var isAdmin = HttpContext.Session.GetString("IsAdmin");
            if (isAdmin != null)
            {
                ViewBag.IsAdmin = true; // Indicate that the user is an admin
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                var customer = _context.Customers.FirstOrDefault(c => c.CustomerId == int.Parse(userId));
                ViewBag.PrizePoints = customer?.PrizePoints ?? 0; // Pass prize points to the view
            }
            return View();
        }

        public IActionResult ConnectAccount() 
        {
            return View();
        }

        public IActionResult About()
        {
            return View(); // This corresponds to About.cshtml
        }

        public IActionResult Prizes()
        {
            return View(); // You'll create Prizes.cshtml
        }

        public IActionResult MyAccount()
        {
            return View(); // You'll create MyAccount.cshtml
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
