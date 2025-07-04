using GameCraft.Data;
using GameCraft.Helpers;
using GameCraft.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore; // Ensure this is included for FirstOrDefault
using System.Collections.Generic;
using System.Linq;
using System.IO; // For MemoryStream and FileStream

namespace GameCraft.Controllers
{
    // TODO: Add authorization attribute [Authorize(Roles = "Admin")] when implemented
    public class AdminController : Controller
    {
        private readonly GameCraftDbContext _context;

        public AdminController(GameCraftDbContext context)
        {
            _context = context;
        }

        private const string ValidAdminKey = "YourSecureAdminKey123"; // Set your actual admin key here

        [HttpGet]
        public IActionResult Login()
        {
            ViewData["Title"] = "Admin Login";
            return View();
        }

        [HttpPost]
        public IActionResult Login(string adminKey)
        {
            if (string.IsNullOrWhiteSpace(adminKey))
            {
                ModelState.AddModelError("", "Admin key is required.");
                return View();
            }

            if (adminKey == ValidAdminKey)
            {
                HttpContext.Session.SetString("IsAdmin", "true");
                HttpContext.Session.SetString("UserName", "Admin");
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "Invalid admin key.");
                return View();
            }
        }

        // GET: /Admin/Index (Admin Dashboard)
        public IActionResult Index()
        {
            var users = _context.Customers.ToList();
            var prizes = _context.Products.ToList();
            var model = new Tuple<List<Customer>, List<Product>>(users, prizes);
            return View(model);
        }

        // --- User Management ---

        // GET: /Admin/ManageUsers
        public IActionResult ManageUsers()
        {
            var users = _context.Customers.ToList();
            ViewBag.UserTypes = GetUserTypes(); // Populate for display if needed, though not directly used in table
            return View(users);
        }

        // GET: /Admin/CreateOrEditUser/{id?}
        public IActionResult CreateOrEditUser(int? id)
        {
            ViewBag.UserTypes = GetUserTypes();
            if (id == null || id == 0)
            {
                ViewData["Title"] = "Create User";
                return View(new Customer()); // Create new user
            }
            else
            {
                ViewData["Title"] = "Edit User Profile";
                var user = _context.Customers.FirstOrDefault(c => c.CustomerId == id);
                if (user == null) return NotFound();
                return View(user); // Edit existing user
            }
        }

        // POST: /Admin/CreateOrEditUser
        [HttpPost]
        [ValidateAntiForgeryToken] // Good practice for POST methods
        public IActionResult CreateOrEditUser(Customer user)
        {
            // The framework automatically checks ModelState.IsValid before entering the action method
            // If ModelState.IsValid is false, the method will be re-executed with the invalid model
            // and validation errors will be available in the view.
            // So, we don't need an explicit 'if (ModelState.IsValid)' block here.

            // Repopulate ViewBag.UserTypes for the view in case of validation errors
            ViewBag.UserTypes = GetUserTypes();

            // If validation fails, the view will be returned automatically with errors
            // If validation passes, proceed to save
            if (user.CustomerId == 0) // New user
            {
                // Hash password only for new users or if password is explicitly changed for existing
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    var (passwordHash, salt) = PasswordHelper.HashPassword(user.PasswordHash);
                    user.PasswordHash = passwordHash;
                    user.Salt = salt;
                }
                else
                {
                    // If creating a new user, password is required. Add a model error if not provided.
                    ModelState.AddModelError("PasswordHash", "Password is required for new users.");
                    return View(user); // Return to view with error
                }

                _context.Customers.Add(user);
                TempData["Message"] = "User created successfully!";
            }
            else // Existing user
            {
                var existingUser = _context.Customers.AsNoTracking().FirstOrDefault(c => c.CustomerId == user.CustomerId);
                if (existingUser == null) return NotFound();

                // Only update password if a new one is provided
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    var (passwordHash, salt) = PasswordHelper.HashPassword(user.PasswordHash);
                    user.PasswordHash = passwordHash;
                    user.Salt = salt;
                }
                else
                {
                    // Retain existing password hash and salt if not provided in the form
                    user.PasswordHash = existingUser.PasswordHash;
                    user.Salt = existingUser.Salt;
                }

                // Update all properties from the form model
                _context.Entry(user).State = EntityState.Modified;
                TempData["Message"] = "User updated successfully!";
            }

            try
            {
                _context.SaveChanges();
                return RedirectToAction("ManageUsers");
            }
            catch (DbUpdateException ex)
            {
                // Log the exception details (e.g., to a file or monitoring system)
                Console.WriteLine($"Database update error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                ModelState.AddModelError("", "An error occurred while saving the user. Please try again.");
                // Re-populate ViewBag.UserTypes before returning the view
                ViewBag.UserTypes = GetUserTypes();
                return View(user); // Return to the form with error message
            }
        }

        // POST: /Admin/DeleteUser/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Customers.FirstOrDefault(c => c.CustomerId == id);
            if (user == null) return NotFound();

            _context.Customers.Remove(user);
            _context.SaveChanges();
            TempData["Message"] = "User deleted successfully!";
            return RedirectToAction("ManageUsers");
        }

        // Helper method to get user types
        private List<SelectListItem> GetUserTypes()
        {
            return _context.UserTypes.Select(ut => new SelectListItem
            {
                Value = ut.Id.ToString(),
                Text = ut.Name
            }).ToList();
        }

        // --- Prize Management ---

        // GET: /Admin/ManagePrizes
        public IActionResult ManagePrizes()
        {
            var prizes = _context.Products.ToList();
            return View(prizes);
        }

        // GET: /Admin/AddOrEditPrize/{id?}
        public IActionResult AddOrEditPrize(int? id)
        {
            ViewBag.Categories = _context.Categories.Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.Name
            }).ToList();

            if (id == null || id == 0)
            {
                ViewData["Title"] = "Add Prize";
                return View(new Product());
            }
            else
            {
                ViewData["Title"] = "Edit Prize";
                var prize = _context.Products.FirstOrDefault(p => p.ProductId == id);
                if (prize == null) return NotFound();
                return View(prize);
            }
        }

        // POST: /Admin/AddOrEditPrize
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddOrEditPrize(Product model, IFormFile imageUpload)
        {
            // Repopulate ViewBag.Categories for the view in case of validation errors
            ViewBag.Categories = _context.Categories.Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.Name
            }).ToList();

            // If validation fails, the view will be returned automatically with errors
            // If validation passes, proceed to save

            if (model.ProductId == 0) // Add new prize
            {
                if (imageUpload == null || imageUpload.Length == 0)
                {
                    ModelState.AddModelError("ImageData", "Image is required for new prizes.");
                    return View(model); // Return to view with error
                }

                using (var memoryStream = new MemoryStream())
                {
                    imageUpload.CopyTo(memoryStream);
                    model.ImageData = memoryStream.ToArray();
                }
                _context.Products.Add(model);
                TempData["Message"] = "Prize added successfully!";
            }
            else // Edit existing prize
            {
                var existingPrize = _context.Products.AsNoTracking().FirstOrDefault(p => p.ProductId == model.ProductId);
                if (existingPrize == null) return NotFound();

                // Update image only if a new one is uploaded
                if (imageUpload != null && imageUpload.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        imageUpload.CopyTo(memoryStream);
                        model.ImageData = memoryStream.ToArray();
                    }
                }
                else
                {
                    // Retain existing image data if no new image is uploaded
                    model.ImageData = existingPrize.ImageData;
                }

                // Update all properties from the form model
                _context.Entry(model).State = EntityState.Modified;
                TempData["Message"] = "Prize updated successfully!";
            }

            try
            {
                _context.SaveChanges();
                return RedirectToAction("ManagePrizes");
            }
            catch (DbUpdateException ex)
            {
                // Log the exception details
                Console.WriteLine($"Database update error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                ModelState.AddModelError("", "An error occurred while saving the prize. Please try again.");
                // Re-populate ViewBag.Categories before returning the view
                ViewBag.Categories = _context.Categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToList();
                return View(model); // Return to the form with error message
            }
        }

        // POST: /Admin/DeletePrize/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePrize(int id)
        {
            var prize = _context.Products.FirstOrDefault(p => p.ProductId == id);
            if (prize == null) return NotFound();

            _context.Products.Remove(prize);
            _context.SaveChanges();
            TempData["Message"] = "Prize deleted successfully!";
            return RedirectToAction("ManagePrizes");
        }

        // --- Category Management ---

        // GET: /Admin/ManageCategories
        public IActionResult ManageCategories()
        {
            var categories = _context.Categories.ToList();
            return View(categories);
        }

        // GET: /Admin/AddOrEditCategory/{id?}
        public IActionResult AddOrEditCategory(int? id)
        {
            if (id == null || id == 0)
            {
                ViewData["Title"] = "Add Category";
                return View(new Category());
            }
            else
            {
                ViewData["Title"] = "Edit Category";
                var category = _context.Categories.FirstOrDefault(c => c.CategoryId == id);
                if (category == null) return NotFound();
                return View(category);
            }
        }

        // POST: /Admin/AddOrEditCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddOrEditCategory(Category model)
        {
            // If validation fails, the view will be returned automatically with errors
            // If validation passes, proceed to save

            if (model.CategoryId == 0) // Add new category
            {
                _context.Categories.Add(model);
                TempData["Message"] = "Category added successfully!";
            }
            else // Edit existing category
            {
                var existingCategory = _context.Categories.AsNoTracking().FirstOrDefault(c => c.CategoryId == model.CategoryId);
                if (existingCategory == null) return NotFound();

                _context.Entry(model).State = EntityState.Modified;
                TempData["Message"] = "Category updated successfully!";
            }

            try
            {
                _context.SaveChanges();
                return RedirectToAction("ManageCategories");
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Database update error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                ModelState.AddModelError("", "An error occurred while saving the category. Please try again.");
                return View(model);
            }
        }

        // POST: /Admin/DeleteCategory/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCategory(int id)
        {
            var category = _context.Categories.FirstOrDefault(c => c.CategoryId == id);
            if (category == null) return NotFound();

            _context.Categories.Remove(category);
            _context.SaveChanges();
            TempData["Message"] = "Category deleted successfully!";
            return RedirectToAction("ManageCategories");
        }
    }
}
