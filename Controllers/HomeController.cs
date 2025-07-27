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
            :base(context)
        {
            _logger = logger;
            _context = context; // Initialize the context
        }

        public async Task<IActionResult> IndexAsync()
        {
            Debug.WriteLine("HomeController.Index() started.");

            // Check if the user is an admin
            var isAdmin = HttpContext.Session.GetString("IsAdmin");
            if (isAdmin != null)
            {
                ViewBag.IsAdmin = true; // Indicate that the user is an admin
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == int.Parse(userId));
                ViewBag.PrizePoints = customer?.PrizePoints ?? 0; // Pass prize points to the view
                Debug.WriteLine($"User  ID: {userId}, Prize Points: {ViewBag.PrizePoints}");
            }

            // Fetch "How it works" icons from the database
            var howItWorksIcons = await _context.Icons
                                                .OrderBy(i => i.Order)
                                                .ToListAsync();

            // Convert ImageData to Base64 string for each icon
            var iconViewModels = howItWorksIcons.Select(icon => new
            {
                icon.IconId,
                icon.Name,
                icon.Description,
                ImageData = Convert.ToBase64String(icon.ImageData) // Convert binary data to Base64 string
            }).ToList();

            ViewBag.HowItWorksIcons = iconViewModels;


            // Fetch carousel items from the database
            var carouselItems = new List<CarouselItemViewModel>();

            // Add Promotions to carousel
            var promotions = await _context.Promotions
                                           .OrderBy(pr => pr.PromotionId) // Or by a specific display order field
                                           .Take(2) // Example: take top 2 promotions
                                           .ToListAsync();
            foreach (var promotion in promotions)
            {
                carouselItems.Add(new CarouselItemViewModel
                {
                    Type = "Promotion",
                    Title = promotion.Title,
                    Description = promotion.Description,
                    ImageUrl = Convert.ToBase64String(promotion.ImageData), // Convert binary data to Base64 string
                    ButtonText = "Learn More",
                    ButtonUrl = Url.Action("Details", "Promotion", new { id = promotion.PromotionId }),
                    BackgroundColor = promotion.BackgroundColor,
                    TextColor = promotion.TextColor
                });
            }

            // Pass carousel items to the view
            ViewBag.CarouselItems = carouselItems;

            // Retrieve 3 random products
            var randomProducts = await _context.Products
                .OrderBy(r => Guid.NewGuid()) // Randomize the order
                .Take(3) // Take 3 products
                .ToListAsync();
            ViewBag.RandomProducts = randomProducts; // Pass the random products to the view

            return View();
        }



        public IActionResult GetCard()
        {
        // This action simply returns the GetCard view.
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
