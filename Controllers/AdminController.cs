using GameCraft.Data;
using GameCraft.Helpers;
using GameCraft.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Add this using statement!
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic; // Add this using statement!
using System.Linq;

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

            // Log the admin key for debugging
            Console.WriteLine($"Admin Key Entered: {adminKey}");

            if (adminKey == ValidAdminKey)
            {
                // Optionally, set a session variable to indicate the admin is logged in
                HttpContext.Session.SetString("IsAdmin", "true");
                HttpContext.Session.SetString("UserName", "Admin"); // You can set a more descriptive name if needed

                // Redirect to the admin index page
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "Invalid admin key.");
                return View();
            }
        }


        // GET: /Admin/ManageUsers
        public IActionResult ManageUsers()
        {
            var users = _context.Customers.ToList();
            ViewBag.UserTypes = GetUserTypes();
            return View(users);
        }

        // GET: /Admin/CreateOrEditUser /{id?}
        public IActionResult CreateOrEditUser(int? id)
        {
            ViewBag.UserTypes = GetUserTypes();
            if (id == null)
            {
                return View(new Customer()); // Create new user
            }
            else
            {
                var user = _context.Customers.FirstOrDefault(c => c.CustomerId == id);
                if (user == null) return NotFound();
                return View(user); // Edit existing user
            }
        }

        // POST: /Admin/CreateOrEditUser 
        [HttpPost]
        public IActionResult CreateOrEditUser(Customer user)
        {
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    var (passwordHash, salt) = PasswordHelper.HashPassword(user.PasswordHash);
                    user.PasswordHash = passwordHash;
                    user.Salt = salt;
                }

                if (user.CustomerId == 0) // New user
                {
                    _context.Customers.Add(user);
                    TempData["Message"] = "User created successfully!";
                }
                else // Existing user
                {
                    var existingUser = _context.Customers.FirstOrDefault(c => c.CustomerId == user.CustomerId);
                    if (existingUser == null) return NotFound();

                    existingUser.Name = user.Name;
                    existingUser.Email = user.Email;
                    existingUser.Phone = user.Phone;
                    existingUser.Address = user.Address;
                    existingUser.City = user.City;
                    existingUser.PostCode = user.PostCode;
                    existingUser.UserType = user.UserType;

                    _context.Customers.Update(existingUser);
                    TempData["Message"] = "User  updated successfully!";
                }

                _context.SaveChanges();
                return RedirectToAction("ManageUsers");
            }

            ViewBag.UserTypes = GetUserTypes();
            return View(user);
        }

        // POST: /Admin/DeleteUser /{id}
        [HttpPost]
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



        // GET: /Admin/ManagePrizes
        public IActionResult ManagePrizes()
        {
            var prizes = _context.Products.ToList(); // Fetch all prizes from the database
            return View(prizes); // Return the list of prizes to the view
        }

        // GET: /Admin/AddOrEditPrize/{id?}
        public IActionResult AddOrEditPrize(int? id)
        {
            // Populate categories for the dropdown, converting to SelectListItem
            ViewBag.Categories = _context.Categories.Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.Name
            }).ToList();

            if (id == null || id == 0)
            {
                // If id is null or 0, create a new Product instance for adding a new prize
                return View(new Product());
            }
            else
            {
                // If id is provided, fetch the existing product for editing
                var prize = _context.Products.FirstOrDefault(p => p.ProductId == id);
                if (prize == null)
                {
                    return NotFound(); // Return 404 if the product is not found
                }
                return View(prize); // Return the existing product for editing
            }
        }

        [HttpPost]
        public IActionResult AddOrEditPrize(int? id, Product model, IFormFile imageUpload)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (imageUpload != null && imageUpload.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            imageUpload.CopyTo(memoryStream); // Copy the uploaded file to memory
                            model.ImageData = memoryStream.ToArray(); // Save the image data
                        }
                    }

                    if (id == null || id == 0)
                    {
                        // Add new prize
                        _context.Products.Add(model);
                        TempData["Message"] = "Prize added successfully!";
                    }
                    else
                    {
                        // Edit existing prize
                        var existingPrize = _context.Products.FirstOrDefault(p => p.ProductId == id);
                        if (existingPrize == null)
                            return NotFound();

                        // Update product details
                        existingPrize.Name = model.Name;
                        existingPrize.Description = model.Description;
                        existingPrize.Price = model.Price;
                        existingPrize.CategoryId = model.CategoryId;

                        if (imageUpload != null && imageUpload.Length > 0)
                        {
                            existingPrize.ImageData = model.ImageData; // Update the ImageData
                        }

                        _context.Products.Update(existingPrize);
                        TempData["Message"] = "Prize updated successfully!";
                    }

                    _context.SaveChanges(); // Save changes to the database
                    return RedirectToAction("ManagePrizes"); // Redirect to ManagePrizes
                }
                else
                {
                    // Log the errors for debugging
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine(error.ErrorMessage);
                    }
                    // Re-populate ViewBag.Categories before returning the view
                    ViewBag.Categories = _context.Categories.Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.Name
                    }).ToList();
                    return View(model); // Return the model to the view for correction
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                // Optionally, set an error message in TempData
                TempData["Message"] = "An error occurred while processing your request.";
                return View(model);
            }
        }



        // POST: /Admin/DeletePrize/5
        [HttpPost]
        public IActionResult DeletePrize(int id)
        {
            var prize = _context.Products.FirstOrDefault(p => p.ProductId == id);
            if (prize == null)
                return NotFound();

            _context.Products.Remove(prize);
            _context.SaveChanges();

            return RedirectToAction("ManagePrizes");
        }

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
                return View(new Category()); // Create a new category
            }
            else
            {
                var category = _context.Categories.FirstOrDefault(c => c.CategoryId == id);
                if (category == null)
                {
                    return NotFound();
                }
                return View(category); // Edit existing category
            }
        }

        // POST: /Admin/AddOrEditCategory/{id?}
        [HttpPost]
        public IActionResult AddOrEditCategory(int? id, Category model)
        {
            if (ModelState.IsValid)
            {
                if (id == null || id == 0)
                {
                    // Add new category
                    _context.Categories.Add(model);
                    TempData["Message"] = "Category added successfully!";
                }
                else
                {
                    // Edit existing category
                    var existingCategory = _context.Categories.FirstOrDefault(c => c.CategoryId == id);
                    if (existingCategory == null)
                        return NotFound();

                    existingCategory.Name = model.Name; // Update category name
                    TempData["Message"] = "Category updated successfully!";
                }
                _context.SaveChanges();
                return RedirectToAction("ManageCategories");
            }
            return View(model);
        }

        // POST: /Admin/DeleteCategory/5
        [HttpPost]
        public IActionResult DeleteCategory(int id)
        {
            var category = _context.Categories.FirstOrDefault(c => c.CategoryId == id);
            if (category == null)
                return NotFound();

            _context.Categories.Remove(category);
            _context.SaveChanges();

            return RedirectToAction("ManageCategories");
        }


        // GET: /Admin/Index
        public IActionResult Index()
        {
            var users = _context.Customers.ToList();
            var prizes = _context.Products.ToList();
            var model = new Tuple<List<Customer>, List<Product>>(users, prizes);
            return View(model);
        }

    }
}
