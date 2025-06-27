using GameCraft.Data;
using GameCraft.Models;
using GameCraft.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace GameCraft.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GameCraftDbContext _context;

        public HomeController(ILogger<HomeController> logger, GameCraftDbContext context)
            : base(context)
        {
            _logger = logger;
            _context = context; // Initialize the context
        }

        public async Task<IActionResult> IndexAsync()
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

            // Retrieve 3 random products
            var randomProducts = await _context.Products
                .OrderBy(r => Guid.NewGuid()) // Randomize the order
                .Take(3) // Take 3 products
                .ToListAsync();
            ViewBag.RandomProducts = randomProducts; // Pass the random products to the view

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

        public async Task<IActionResult> Prizes()
        {
            // Retrieve products without including categories
            var products = await _context.Products.ToListAsync();
            var categories = await _context.Categories.ToListAsync();
            var viewModel = new PrizeCatalogViewModel
            {
                Products = products,
                Categories = categories
            };
            return View("~/Views/Home/Prizes.cshtml", viewModel); // Ensure you are passing the view model
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
