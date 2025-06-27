using GameCraft.Data;
using GameCraft.Models;
using GameCraft.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

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
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products.ToListAsync();
            var categories = await _context.Categories.ToListAsync();

            var viewModel = new PrizeCatalogViewModel
            {
                Products = products,
                Categories = categories
            };

            return View(viewModel);
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

        // Filter products by category
        public async Task<IActionResult> FilterByCategory(int categoryId)
        {
            var products = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();
            var categories = await _context.Categories.ToListAsync();

            var viewModel = new PrizeCatalogViewModel
            {
                Products = products,
                Categories = categories
            };

            return PartialView("_ProductGrid", viewModel); // Return a partial view
        }


        // Search for products by name
        public async Task<IActionResult> Search(string keyword)
        {
            var results = await _context.Products
                .Where(p => p.Name.Contains(keyword))
                .ToListAsync();

            var categories = await _context.Categories.ToListAsync();

            return View("Index", new PrizeCatalogViewModel { Products = results, Categories = categories });
        }

        public IActionResult Debug()
        {
            var products = _context.Products.ToList();
            return Content("Total products in DB: " + products.Count);
        }

        // This will handle requests to /Products or /Products/Index
        public async Task<IActionResult> Products()
        {
            var products = await _context.Products.ToListAsync();
            var categories = await _context.Categories.ToListAsync();

            var viewModel = new PrizeCatalogViewModel
            {
                Products = products,
                Categories = categories
            };

            return View("~/Views/Home/Products.cshtml", viewModel);
        }
    }
}
