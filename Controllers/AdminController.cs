using GameCraft.Data;
using GameCraft.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System; // Added for DateTime.UtcNow
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using GameCraft.Services;
using System.Text;
using GameCraft.Models;

namespace GameCraft.Controllers
{
    // TODO: Add authorization attribute [Authorize(Roles = "Admin")] when implemented
    public class AdminController : Controller
    {
        private readonly GameCraftDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService; // Declare the IEmailService field

        public AdminController(GameCraftDbContext context, IWebHostEnvironment env, IEmailService emailService) // Inject IEmailService
        {
            _context = context;
            _env = env;
            _emailService = emailService; // Assign the injected service
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

                // Log the login activity
                var auditLog = new AuditLog
                {
                    UserId = "Admin",
                    UserName = "Admin",
                    Action = "Admin Logged In",
                    Details = "Admin logged in successfully.",
                    Timestamp = DateTime.UtcNow,
                    UserRole = "Admin"
                };
                _context.AuditLogs.Add(auditLog);
                _context.SaveChanges();

                return RedirectToAction("Index");
            }

            // Check if the entered admin key matches the stored admin key in the database
            var adminUser = _context.Customers.FirstOrDefault(c => c.AdminKey == adminKey);
            if (adminUser != null)
            {
                HttpContext.Session.SetString("IsAdmin", "true");
                HttpContext.Session.SetString("UserName", adminUser.Name); // Store the admin's name or any other relevant info

                // Log the login activity
                var auditLog = new AuditLog
                {
                    UserId = adminUser.CustomerId.ToString(),
                    UserName = adminUser.Name,
                    Action = "Admin Logged In",
                    Details = "Admin logged in successfully.",
                    Timestamp = DateTime.UtcNow,
                    UserRole = "Admin"
                };
                _context.AuditLogs.Add(auditLog);
                _context.SaveChanges();

                return RedirectToAction("Index");
            }

            else
            {
                ModelState.AddModelError("", "Invalid admin key.");
                return View();
            }

        }

        [HttpPost] // Logout action
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Get the admin's name from the session
            var adminName = HttpContext.Session.GetString("UserName");

            // Log the logout action in the audit log
            if (!string.IsNullOrEmpty(adminName))
            {
                var auditLog = new AuditLog
                {
                    UserId = "Admin", // You can set a specific ID for admin or leave it as a string
                    UserName = adminName,
                    Action = "Admin Logged Out",
                    Details = "Admin logged out successfully.",
                    Timestamp = DateTime.UtcNow,
                    UserRole = "Admin"
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }

            // Clear the session
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
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
        public IActionResult ManageUsers(string searchQuery, int? filterUserType, bool? filterIsEmailVerified, int? minPrizePoints, int? maxPrizePoints)
        {
            IQueryable<Customer> users = _context.Customers;

            // Apply search query
            if (!string.IsNullOrEmpty(searchQuery))
            {
                users = users.Where(u =>
                    u.Name.Contains(searchQuery) ||
                    u.Email.Contains(searchQuery) ||
                    (u.GameCraftCardNumber != null && u.GameCraftCardNumber.Contains(searchQuery)) ||
                    u.CustomerId.ToString().Contains(searchQuery));
            }

            // Apply filters
            if (filterUserType.HasValue)
            {
                users = users.Where(u => u.UserType == filterUserType.Value);
            }
            if (filterIsEmailVerified.HasValue)
            {
                users = users.Where(u => u.IsEmailVerified == filterIsEmailVerified.Value);
            }
            if (minPrizePoints.HasValue)
            {
                users = users.Where(u => u.PrizePoints >= minPrizePoints.Value);
            }
            if (maxPrizePoints.HasValue)
            {
                users = users.Where(u => u.PrizePoints <= maxPrizePoints.Value);
            }

            var viewModel = new UserManagementViewModel
            {
                Users = users.ToList(),
                SearchQuery = searchQuery,
                FilterUserType = filterUserType,
                FilterIsEmailVerified = filterIsEmailVerified,
                MinPrizePoints = minPrizePoints,
                MaxPrizePoints = maxPrizePoints
            };

            ViewBag.UserTypes = GetUserTypes();
            return View(viewModel);
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
                    // Only generate a new key if it's null or empty
                    if (string.IsNullOrEmpty(user.AdminKey))
                    {
                        user.AdminKey = GenerateRandomAdminKey();
                    }
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

                // Log the user creation activity
                var auditLog = new AuditLog
                {
                    UserId = user.CustomerId.ToString(),
                    UserName = user.Name,
                    Action = "Add User",
                    Details = $"Admin added new user: {user.Name}.",
                    Timestamp = DateTime.UtcNow,
                    UserRole = "Admin"
                };
                _context.AuditLogs.Add(auditLog);
            }
            else // Existing user
            {
                var existingUser = _context.Customers.FirstOrDefault(c => c.CustomerId == user.CustomerId);
                if (existingUser == null) return NotFound();

                // Prepare to log changes
                var changes = new List<string>();

                // Update properties and log changes
                if (existingUser.Name != user.Name)
                {
                    changes.Add($"Name changed from '{existingUser.Name}' to '{user.Name}'");
                    existingUser.Name = user.Name; // Update the existing user
                }
                if (existingUser.Email != user.Email)
                {
                    changes.Add($"Email changed from '{existingUser.Email}' to '{user.Email}'");
                    existingUser.Email = user.Email; // Update the existing user
                }
                if (existingUser.UserType != user.UserType)
                {
                    changes.Add($"User  Type changed from '{existingUser.UserType}' to '{user.UserType}'");
                    existingUser.UserType = user.UserType; // Update the existing user
                }
                if (existingUser.Phone != user.Phone)
                {
                    changes.Add($"Phone changed from '{existingUser.Phone}' to '{user.Phone}'");
                    existingUser.Phone = user.Phone; // Update the existing user
                }
                if (existingUser.Address != user.Address)
                {
                    changes.Add($"Address changed from '{existingUser.Address}' to '{user.Address}'");
                    existingUser.Address = user.Address; // Update the existing user
                }
                if (existingUser.PrizePoints != user.PrizePoints)
                {
                    changes.Add($"PrizePoints changed from '{existingUser.PrizePoints}' to '{user.PrizePoints}'");
                    existingUser.PrizePoints = user.PrizePoints; // Update the existing user
                }
                if (existingUser.GameCraftCardNumber != user.GameCraftCardNumber)
                {
                    changes.Add($"GameCraftCardNumber changed from '{existingUser.GameCraftCardNumber}' to '{user.GameCraftCardNumber}'");
                    existingUser.GameCraftCardNumber = user.GameCraftCardNumber; // Update the existing user
                }

                // Update UserType and other properties
                existingUser.Name = user.Name;
                existingUser.Email = user.Email;
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

                // Log the user update activity
                var userDetails = changes.Any() ? string.Join("; ", changes) : "No significant changes detected.";

                // Log the user update activity
                var auditLog = new AuditLog
                {
                    UserId = existingUser.CustomerId.ToString(),
                    UserName = existingUser.Name,
                    Action = "Edit User",
                    Details = $"Admin edited user: {existingUser.Name}. Changes: {userDetails}",
                    Timestamp = DateTime.UtcNow,
                    UserRole = "Admin"
                };
                _context.AuditLogs.Add(auditLog);
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


        // POST: /Admin/DeleteUser /{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
            if (user == null) return NotFound();

            _context.Customers.Remove(user);
            await _context.SaveChangesAsync();
            TempData["Message"] = "User  deleted successfully!";

            // Log the user deletion activity
            var auditLog = new AuditLog
            {
                UserId = user.CustomerId.ToString(),
                UserName = user.Name,
                Action = "Delete User",
                Details = $"Admin deleted user: {user.Name}.",
                Timestamp = DateTime.UtcNow,
                UserRole = "Admin"
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

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
            var prizes = _context.Products
                .Include(p => p.Category) // Include the related Category
                .ToList(); // Fetch all products with their categories
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

                // Log the prize addition activity
                var auditLog = new AuditLog
                {
                    UserId = "Admin", // You can set a specific ID for admin or leave it as a string
                    UserName = "Admin", // Assuming admin's name is "Admin"
                    Action = "Add Prize",
                    Details = $"Admin added new prize: {model.Name}.",
                    Timestamp = DateTime.UtcNow,
                    UserRole = "Admin"
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

            }
            else // Edit existing prize
            {
                var existingPrize = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == model.ProductId);
                if (existingPrize == null) return NotFound();

                // Prepare to log changes
                var changes = new List<string>();

                // Update image only if a new one is uploaded
                if (imageUpload != null && imageUpload.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await imageUpload.CopyToAsync(memoryStream);
                        model.ImageData = memoryStream.ToArray();
                    }
                    changes.Add("Image updated.");
                }
                else
                {
                    // Retain existing image data if no new image is uploaded
                    model.ImageData = existingPrize.ImageData;
                }

                // Check for other changes
                if (existingPrize.Name != model.Name)
                {
                    changes.Add($"Name changed from '{existingPrize.Name}' to '{model.Name}'");
                }
                if (existingPrize.Price != model.Price)
                {
                    changes.Add($"Price changed from '{existingPrize.Price}' to '{model.Price}'");
                }
                if (existingPrize.Description != model.Description)
                {
                    changes.Add($"Description changed from '{existingPrize.Description}' to '{model.Description}'");
                }
                if (existingPrize.CategoryId != model.CategoryId)
                {
                    changes.Add($"Category changed from '{existingPrize.CategoryId}' to '{model.CategoryId}'");
                }


                // Update all properties from the form model
                _context.Entry(model).State = EntityState.Modified;
                TempData["Message"] = "Prize updated successfully!";

                // Log the prize update activity
                var prizeDetails = changes.Any() ? string.Join("; ", changes) : "No significant changes detected.";
                // Log the prize update activity
                var auditLog = new AuditLog
                {
                    UserId = "Admin", // You can set a specific ID for admin or leave it as a string
                    UserName = "Admin", // Assuming admin's name is "Admin"
                    Action = "Edit Prize",
                    Details = $"Admin edited prize: {existingPrize.Name} (ID: {model.ProductId}).",
                    Timestamp = DateTime.UtcNow,
                    UserRole = "Admin"
                };
                _context.AuditLogs.Add(auditLog);
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

            // Log the prize deletion activity
            var auditLog = new AuditLog
            {
                UserId = "Admin", // You can set a specific ID for admin or leave it as a string
                UserName = "Admin", // Assuming admin's name is "Admin"
                Action = "Delete Prize",
                Details = $"Admin deleted prize: {prize.Name}.",
                Timestamp = DateTime.UtcNow,
                UserRole = "Admin"
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

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

                // Log the category addition activity
                var auditLog = new AuditLog
                {
                    UserId = "Admin", // You can set a specific ID for admin or leave it as a string
                    UserName = "Admin", // Assuming admin's name is "Admin"
                    Action = "Add Category",
                    Details = $"Admin added new category: {model.Name}.",
                    Timestamp = DateTime.UtcNow,
                    UserRole = "Admin"
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

            }
            else // Edit existing category
            {
                var existingCategory = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == model.CategoryId);
                if (existingCategory == null) return NotFound();

                _context.Entry(model).State = EntityState.Modified;
                TempData["Message"] = "Category updated successfully!";

                // Log the category update activity
                var auditLog = new AuditLog
                {
                    UserId = "Admin", // You can set a specific ID for admin or leave it as a string
                    UserName = "Admin", // Assuming admin's name is "Admin"
                    Action = "Edit Category",
                    Details = $"Admin edited category: {existingCategory.Name} (ID: {model.CategoryId}).",
                    Timestamp = DateTime.UtcNow,
                    UserRole = "Admin"
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
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

            // Log the category deletion activity
            var auditLog = new AuditLog
            {
                UserId = "Admin", // You can set a specific ID for admin or leave it as a string
                UserName = "Admin", // Assuming admin's name is "Admin"
                Action = "Delete Category",
                Details = $"Admin deleted category: {category.Name}.",
                Timestamp = DateTime.UtcNow,
                UserRole = "Admin"
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageCategories");
        }

        // GET: /Admin/AuditLog
        public IActionResult AuditLog()
        {
            var auditLogs = _context.AuditLogs.ToList();
            return View(auditLogs);
        }

        // NEW: Action to get audit logs for a specific user
        [HttpGet]
        public IActionResult UserAuditLog(int customerId)
        {
            var user = _context.Customers.Find(customerId);
            if (user == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"Audit Log for {user.Name}";
            ViewBag.UserName = user.Name; // Pass user name to view
            ViewBag.CustomerId = customerId; // Pass customer ID to view

            var auditLogs = _context.AuditLogs
                                    .Where(log => log.UserId == customerId.ToString())
                                    .OrderByDescending(log => log.Timestamp)
                                    .ToList();
            return View(auditLogs);
        }

        // NEW: Action for bulk deleting users
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDeleteUsers([FromBody] List<int> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                return Json(new { success = false, message = "No users selected for deletion." });
            }

            var usersToDelete = await _context.Customers.Where(u => userIds.Contains(u.CustomerId)).ToListAsync();

            if (!usersToDelete.Any())
            {
                return Json(new { success = false, message = "No matching users found for deletion." });
            }

            _context.Customers.RemoveRange(usersToDelete);
            await _context.SaveChangesAsync();

            // Log the bulk deletion activity
            var deletedUserNames = string.Join(", ", usersToDelete.Select(u => u.Name));
            var auditLog = new AuditLog
            {
                UserId = "Admin", // Assuming admin is performing this action
                UserName = HttpContext.Session.GetString("UserName") ?? "Admin",
                Action = "Bulk Delete Users",
                Details = $"Admin bulk deleted users: {deletedUserNames} (IDs: {string.Join(", ", userIds)}).",
                Timestamp = DateTime.UtcNow,
                UserRole = "Admin"
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"{usersToDelete.Count} user(s) deleted successfully." });
        }

        // GET: /Admin/ManageCards
        public IActionResult ManageCards()
        {
            var cards = _context.Cards.ToList(); // Fetch all cards from the database
            return View(cards); // Return the view with the list of cards
        }

        // GET: /Admin/CreateOrEditCard/{id?}
        public IActionResult CreateOrEditCard(int? id)
        {
            if (id == null || id == 0)
            {
                ViewData["Title"] = "Add Card";
                return View(new Card()); // Return a new Card object for creating a new card
            }
            else
            {
                ViewData["Title"] = "Edit Card";
                var card = _context.Cards.FirstOrDefault(c => c.CardId == id);
                if (card == null) return NotFound(); // Return 404 if card not found
                return View(card); // Return the existing card for editing
            }
        }

        // POST: /Admin/CreateOrEditCard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrEditCard(Card model, IFormFile imageUpload)
        {
            if (ModelState.IsValid)
            {
                if (model.CardId == 0) // Add new card
                {
                    if (imageUpload != null && imageUpload.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await imageUpload.CopyToAsync(memoryStream);
                            model.ImageData = memoryStream.ToArray(); // Save image data
                        }
                        _context.Cards.Add(model);
                        TempData["Message"] = "Card added successfully!";
                    }
                    else
                    {
                        ModelState.AddModelError("ImageData", "Image is required for new cards.");
                        return View(model); // Return to view with error
                    }
                }
                else // Edit existing card
                {
                    var existingCard = await _context.Cards.FindAsync(model.CardId);
                    if (existingCard == null) return NotFound(); // Return 404 if card not found

                    // Update properties
                    existingCard.Name = model.Name;
                    existingCard.Description = model.Description;
                    existingCard.Price = model.Price;

                    // Update image only if a new one is uploaded
                    if (imageUpload != null && imageUpload.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await imageUpload.CopyToAsync(memoryStream);
                            existingCard.ImageData = memoryStream.ToArray(); // Update image data
                        }
                    }

                    _context.Cards.Update(existingCard);
                    TempData["Message"] = "Card updated successfully!";
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("ManageCards");
            }

            return View(model); // Return to view with validation errors
        }

        // POST: /Admin/DeleteCard/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCard(int id)
        {
            var card = await _context.Cards.FindAsync(id);
            if (card == null) return NotFound();

            _context.Cards.Remove(card);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Card deleted successfully!";

            // Log the card deletion activity
            var auditLog = new AuditLog
            {
                UserId = "Admin", // You can set a specific ID for admin or leave it as a string
                UserName = "Admin", // Assuming admin's name is "Admin"
                Action = "Delete Card",
                Details = $"Admin deleted card: {card.Name}.",
                Timestamp = DateTime.UtcNow,
                UserRole = "Admin"
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            return RedirectToAction("ManageCards");
        }
    }
}
