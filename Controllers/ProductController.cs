using Microsoft.AspNetCore.Mvc;
using System.Linq;
using GameCraft.Models;
using GameCraft.Data;

namespace GameCraft.Controllers
{
    public class ProductController : Controller
    {
        private readonly GameCraftDbContext _context;

        public ProductController(GameCraftDbContext context)
        {
            _context = context;
        }

        // Show all products
        public IActionResult Index()
        {
            var products = new List<Product>
    {
        new Product { ProductId = 1, Name = "Book A", Description = "A test book", Price = 19.99M },
        new Product { ProductId = 2, Name = "Movie B", Description = "Sample movie", Price = 29.99M },
        new Product { ProductId = 3, Name = "Game C", Description = "Exciting new game", Price = 49.90M }
    };

            return View(products);
        }
        

        // Show product details
        public IActionResult Details(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // Search for products by name
        public IActionResult Search(string keyword)
        {
            var results = _context.Products
                .Where(p => p.Name.Contains(keyword))
                .ToList();

            return View("Index", results); 
        }

        public IActionResult Debug()
        {
            var products = _context.Products.ToList();
            return Content("Total products in DB: " + products.Count);
        }
    }
}