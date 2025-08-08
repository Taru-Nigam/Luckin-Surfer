// FileName: /Controllers/AdminController.cs
using GameCraft.Data;
using GameCraft.Helpers;
using GameCraft.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore; // Ensure this is included for FirstOrDefault
using System.Collections.Generic;
using System.IO; // For MemoryStream and FileStream
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting; // For IWebHostEnvironment

namespace GameCraft.Controllers
{
    // TODO: Add authorization attribute [Authorize(Roles = "Admin")] when implemented
    public class AdminController : Controller
    {
        private readonly GameCraftDbContext _context;
        private readonly IWebHostEnvironment _env; // Inject IWebHostEnvironment

        public AdminController(GameCraftDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private const string ValidAdminKey = "YourSecureAdminKey123"; // Set your actual admin key here

        // Helper to get default avatar image data
        private byte[] GetDefaultAvatarImageData()
        {
            var defaultAvatarPath = Path.Combine(_env.WebRootPath, "images", "default-avatar.png");
            if (System.IO.File.Exists(defaultAvatarPath))
            {
                return System.IO.File.ReadAllBytes(defaultAvatarPath);
            }
            return null; // Or throw an exception if default avatar is mandatory
        }

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

            // Check if the entered admin key matches the stored admin key in the database
            var adminUser = _context.Customers.FirstOrDefault(c => c.AdminKey == adminKey);
            if (adminUser != null)
            {
                HttpContext.Session.SetString("IsAdmin", "true");
                HttpContext.Session.SetString("UserName", adminUser.Name); // Store the admin's name or any other relevant info
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

        // GET: /Admin/CreateOrEditUser  /{id?}
        public IActionResult CreateOrEditUser(int? id)
        {
            ViewBag.UserTypes = GetUserTypes();
            if (id == null || id == 0)
            {
                ViewData["Title"] = "Create User";
                // For new users, pre-fill with default avatar data and set UserType to regular user
                var defaultAvatarData = GetDefaultAvatarImageData();
                var newUser = new Customer
                {
                    AvatarImageData = defaultAvatarData,
                    UserType = 1 // Assuming 1 is the value for regular User
                };
                return View(newUser);
            }
            else
            {
                ViewData["Title"] = "Edit User Profile";
                var user = _context.Customers.FirstOrDefault(c => c.CustomerId == id);
                if (user == null) return NotFound();

                // If the user is an admin, generate an admin key
                if (user.UserType == 0) // Assuming 0 is the value for Admin
                {
                    user.AdminKey = GenerateRandomAdminKey(); // Generate and assign the admin key
                }

                return View(user); // Edit existing user
            }
        }



        // Method to generate a random admin key
        private string GenerateRandomAdminKey(int length = 16)
        {
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder result = new StringBuilder(length);
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] randomBytes = new byte[length];
                rng.GetBytes(randomBytes);
                for (int i = 0; i < length; i++)
                {
                    result.Append(validChars[randomBytes[i] % validChars.Length]);
                }
            }
            return result.ToString();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateOrEditUser(Customer user, IFormFile? avatarFile, string? selectedAvatarPath)
        {
            // Repopulate ViewBag.UserTypes for the view in case of validation errors
            ViewBag.UserTypes = GetUserTypes();

            // Manual validation
            var errors = new List<string>();

            // Check for duplicate email/name before adding
            if (user.CustomerId == 0) // New user
            {
                if (_context.Customers.Any(c => c.Email == user.Email))
                {
                    errors.Add("This email is already in use.");
                }
                if (_context.Customers.Any(c => c.Name == user.Name))
                {
                    errors.Add("This username is already taken.");
                }
            }
            else // Existing user
            {
                var existingUser = _context.Customers.AsNoTracking().FirstOrDefault(c => c.CustomerId == user.CustomerId);
                if (existingUser == null) return NotFound();

                // Check for duplicate email/name if they are changed
                if (existingUser.Email != user.Email && _context.Customers.Any(c => c.Email == user.Email && c.CustomerId != user.CustomerId))
                {
                    errors.Add("This email is already in use by another account.");
                }
                if (existingUser.Name != user.Name && _context.Customers.Any(c => c.Name == user.Name && c.CustomerId != user.CustomerId))
                {
                    errors.Add("This username is already taken by another account.");
                }
            }

            // If there are any validation errors, return to the view with errors
            if (errors.Any())
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError("", error);
                }
                return View(user);
            }

            // Proceed with saving the user
            if (user.CustomerId == 0) // New user
            {
                // Hash password
                var (passwordHash, salt) = PasswordHelper.HashPassword(user.PasswordHash);
                user.PasswordHash = passwordHash;
                user.Salt = salt;

                // Handle avatar file upload
                if (avatarFile != null && avatarFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        avatarFile.CopyTo(memoryStream);
                        user.AvatarImageData = memoryStream.ToArray();
                    }
                }
                else if (!string.IsNullOrEmpty(selectedAvatarPath))
                {
                    var fullPath = Path.Combine(_env.WebRootPath, selectedAvatarPath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        user.AvatarImageData = System.IO.File.ReadAllBytes(fullPath);
                    }
                }
                else
                {
                    user.AvatarImageData = GetDefaultAvatarImageData(); // Ensure default avatar is set
                }

                // Generate a random admin key if the user is an admin
                if (user.UserType == 0) // UserType 0 indicates Admin
                {
                    user.AdminKey = GenerateRandomAdminKey(); // Generate and assign the admin key
                }

                _context.Customers.Add(user);
                TempData["Message"] = "User  created successfully!";
            }
            else // Existing user
            {
                var existingUser = _context.Customers.FirstOrDefault(c => c.CustomerId == user.CustomerId);
                if (existingUser == null) return NotFound();

                // Update UserType and other properties
                existingUser.UserType = user.UserType;
                existingUser.Phone = user.Phone; // Update phone number
                existingUser.Address = user.Address; // Update address
                existingUser.PrizePoints = user.PrizePoints; // Update prize points
                existingUser.GameCraftCardNumber = user.GameCraftCardNumber; // Update GameCraft card number

                // Only update password if a new one is provided
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    var (passwordHash, salt) = PasswordHelper.HashPassword(user.PasswordHash);
                    existingUser.PasswordHash = passwordHash;
                    existingUser.Salt = salt;
                }

                // Handle avatar file upload
                if (avatarFile != null && avatarFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        avatarFile.CopyTo(memoryStream);
                        existingUser.AvatarImageData = memoryStream.ToArray();
                    }
                }
                else if (!string.IsNullOrEmpty(selectedAvatarPath))
                {
                    var fullPath = Path.Combine(_env.WebRootPath, selectedAvatarPath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        existingUser.AvatarImageData = System.IO.File.ReadAllBytes(fullPath);
                    }
                }

                // Update all properties from the form model
                _context.Entry(existingUser).CurrentValues.SetValues(existingUser);
                TempData["Message"] = "User  updated successfully!";
            }

            try
            {
                _context.SaveChanges();
                return RedirectToAction("ManageUsers");
            }
            catch (DbUpdateException ex)
            {
                // Log the exception details
                Console.WriteLine($"Database update error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                ModelState.AddModelError("", "An error occurred while saving the user. Please try again.");
                return View(user); // Return to the form with error message
            }
        }


        // POST: /Admin/DeleteUser/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
            if (user == null) return NotFound();

            _context.Customers.Remove(user);
            await _context.SaveChangesAsync();
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
        public async Task<IActionResult> AddOrEditPrize(Product model, IFormFile imageUpload)
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
                    await imageUpload.CopyToAsync(memoryStream);
                    model.ImageData = memoryStream.ToArray();
                }
                _context.Products.Add(model);
                TempData["Message"] = "Prize added successfully!";
            }
            else // Edit existing prize
            {
                var existingPrize = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == model.ProductId);
                if (existingPrize == null) return NotFound();

                // Update image only if a new one is uploaded
                if (imageUpload != null && imageUpload.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await imageUpload.CopyToAsync(memoryStream);
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
                await _context.SaveChangesAsync();
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
        public async Task<IActionResult> DeletePrize(int id)
        {
            var prize = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id);
            if (prize == null) return NotFound();

            _context.Products.Remove(prize);
            await _context.SaveChangesAsync();
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
        public async Task<IActionResult> AddOrEditCategory(Category model)
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
                var existingCategory = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == model.CategoryId);
                if (existingCategory == null) return NotFound();

                _context.Entry(model).State = EntityState.Modified;
                TempData["Message"] = "Category updated successfully!";
            }

            try
            {
                await _context.SaveChangesAsync();
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
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null) return NotFound();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Category deleted successfully!";
            return RedirectToAction("ManageCategories");
        }
    }
}
