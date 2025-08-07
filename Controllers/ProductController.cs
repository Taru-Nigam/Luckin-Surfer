using GameCraft.Data;
using GameCraft.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
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

        public IActionResult GetImage(int id)
        {
            var product = _context.Products.Find(id);
            if (product?.ImageData != null)
            {
                // Determine the content type based on the image data
                string contentType = "application/octet-stream"; // Default content type

                // Check the first few bytes to determine the image type
                if (product.ImageData.Length >= 4)
                {
                    // Check for PNG signature
                    if (product.ImageData[0] == 0x89 && product.ImageData[1] == 0x50 && product.ImageData[2] == 0x4E && product.ImageData[3] == 0x47)
                    {
                        contentType = "image/png";
                    }
                    // Check for JPEG signature
                    else if (product.ImageData[0] == 0xFF && product.ImageData[1] == 0xD8)
                    {
                        contentType = "image/jpeg";
                    }
                }

                return File(product.ImageData, contentType); // Return the image with the correct content type
            }
            return NotFound();
        }



        // GET: Product/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    CategoryId = model.CategoryId
                };

                // Convert the uploaded image to a byte array
                if (imageFile != null && imageFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await imageFile.CopyToAsync(memoryStream);
                        product.ImageData = memoryStream.ToArray(); // Save the image data
                    }
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
    }
}
