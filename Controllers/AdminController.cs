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


        // GET: /Admin/ManageEmployees
        public IActionResult ManageEmployees()
        {
            // Fetch all customers who are employees (User  Type for Employee)
            // Get the Employee role's Id first
            var employeeRole = _context.UserTypes.FirstOrDefault(rt => rt.Name == "Employee");
            if (employeeRole == null)
            {
                // Handle null case if needed
                // Maybe return empty list or throw exception
                return View(new List<Customer>());
            }
            int employeeRoleId = employeeRole.Id;

            // Use employeeRoleId in query without `?.`
            var employees = _context.Customers
                                    .Where(c => c.UserType == employeeRoleId)
                                    .ToList();

            return View(employees);
        }

        // GET: /Admin/ManageUsers
        public IActionResult ManageUsers()
        {
            var users = _context.Customers.ToList();

            ViewBag.UserTypes = _context.UserTypes.Select(ut => new SelectListItem
            {
                Value = ut.Id.ToString(),
                Text = ut.Name
            }).ToList();

            return View(users);
        }


        // GET: /Admin/CreateEmployee
        public IActionResult CreateEmployee()
        {
            ViewBag.UserTypes = _context.UserTypes.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Name
            }).ToList();

            return View(new Customer()); // Return a new Customer instance for creating an employee
        }

        // POST: /Admin/CreateEmployee
        [HttpPost]
        public IActionResult CreateEmployee(Customer newEmployee)
        {
            if (ModelState.IsValid)
            {
                // Hash the password and set the UserType
                var (PasswordHash, salt) = PasswordHelper.HashPassword(newEmployee.PasswordHash);
                newEmployee.PasswordHash = PasswordHash;
                newEmployee.Salt = salt;

                _context.Customers.Add(newEmployee);
                _context.SaveChanges();
                TempData["Message"] = "Employee account created successfully!";
                return RedirectToAction("ManageUsers");
            }

            // If ModelState is not valid, re-populate ViewBag.UserTypes before returning the view
            ViewBag.UserTypes = _context.UserTypes.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Name
            }).ToList();
            return View(newEmployee);
        }

        // GET: /Admin/EditUser /{id}
        public IActionResult EditUser(int id)
        {
            var user = _context.Customers.FirstOrDefault(c => c.CustomerId == id);
            if (user == null)
            {
                return NotFound(); // Return 404 if user not found
            }

            // Populate UserTypes for the dropdown
            ViewBag.UserTypes = _context.UserTypes.Select(ut => new SelectListItem
            {
                Value = ut.Id.ToString(),
                Text = ut.Name
            }).ToList();

            return View(user);
        }

        // POST: /Admin/EditUser 
        [HttpPost]
        public IActionResult EditUser(Customer editedUser)
        {
            if (ModelState.IsValid)
            {
                var existingUser = _context.Customers.FirstOrDefault(c => c.CustomerId == editedUser.CustomerId);
                if (existingUser == null)
                {
                    return NotFound(); // Return 404 if user not found
                }

                // Update user details
                existingUser.Name = editedUser.Name;
                existingUser.Email = editedUser.Email;
                existingUser.Phone = editedUser.Phone;
                existingUser.Address = editedUser.Address;
                existingUser.City = editedUser.City;
                existingUser.PostCode = editedUser.PostCode;
                existingUser.UserType = editedUser.UserType;

                // Update password if provided
                if (!string.IsNullOrEmpty(editedUser.PasswordHash))
                {
                    var (passwordHash, salt) = PasswordHelper.HashPassword(editedUser.PasswordHash);
                    existingUser.PasswordHash = passwordHash;
                    existingUser.Salt = salt;
                }

                _context.Customers.Update(existingUser);
                _context.SaveChanges();

                TempData["Message"] = "User  updated successfully!";
                return RedirectToAction("ManageUsers");
            }

            // If ModelState is not valid, re-populate ViewBag.UserTypes before returning the view
            ViewBag.UserTypes = _context.UserTypes.Select(ut => new SelectListItem
            {
                Value = ut.Id.ToString(),
                Text = ut.Name
            }).ToList();
            return View(editedUser);
        }


        // POST: /Admin/EditUser /{id}
        [HttpPost]
        public IActionResult EditUser(int id, Customer editedUser)
        {
            if (id != editedUser.CustomerId)
                return BadRequest();

            if (ModelState.IsValid)
            {
                var existingUser = _context.Customers.FirstOrDefault(c => c.CustomerId == id);
                if (existingUser == null)
                    return NotFound();

                // Update user details
                existingUser.Name = editedUser.Name;
                existingUser.Email = editedUser.Email;
                existingUser.Phone = editedUser.Phone;
                existingUser.Address = editedUser.Address;
                existingUser.City = editedUser.City;
                existingUser.PostCode = editedUser.PostCode;
                existingUser.UserType = editedUser.UserType;

                _context.Customers.Update(existingUser);
                _context.SaveChanges();

                TempData["Message"] = "User  updated successfully!";
                return RedirectToAction("ManageUsers");
            }

            ViewBag.UserTypes = _context.UserTypes.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Name
            }).ToList();
            return View(editedUser);
        }

        // POST: /Admin/DeleteUser /{id}
        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Customers.FirstOrDefault(c => c.CustomerId == id);
            if (user == null)
                return NotFound();

            _context.Customers.Remove(user);
            _context.SaveChanges();

            TempData["Message"] = "User  deleted successfully!";
            return RedirectToAction("ManageUsers");
        }


        // GET: /Admin/EditEmployee/5
        public IActionResult EditEmployee(int id)
        {
            var employee = _context.Customers.FirstOrDefault(c => c.CustomerId == id);
            if (employee == null)
            {
                return NotFound();
            }

            // Populate UserTypes for the dropdown, converting to SelectListItem
            ViewBag.UserTypes = _context.UserTypes.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Name
            }).ToList();

            return View(employee);
        }

        // POST: /Admin/EditEmployee/5
        [HttpPost]
        public IActionResult EditEmployee(int id, Customer editedEmployee)
        {
            if (id != editedEmployee.CustomerId)
                return BadRequest();

            if (ModelState.IsValid)
            {
                var existingEmployee = _context.Customers.FirstOrDefault(c => c.CustomerId == id);
                if (existingEmployee == null)
                    return NotFound();

                // We update basic fields but not password here for simplicity
                existingEmployee.Name = editedEmployee.Name;
                existingEmployee.Email = editedEmployee.Email;
                existingEmployee.Phone = editedEmployee.Phone;
                existingEmployee.Address = editedEmployee.Address;
                existingEmployee.City = editedEmployee.City;
                existingEmployee.PostCode = editedEmployee.PostCode;

                // Allow role change including permission level
                if (_context.UserTypes.Any(rt => rt.Id == editedEmployee.UserType))
                {
                    existingEmployee.UserType = editedEmployee.UserType;
                }

                _context.Customers.Update(existingEmployee);
                _context.SaveChanges();

                return RedirectToAction("ManageEmployees");
            }

            // If ModelState is not valid, re-populate ViewBag.UserTypes before returning the view
            ViewBag.UserTypes = _context.UserTypes.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Name
            }).ToList();
            return View(editedEmployee);
        }

        // POST: /Admin/DeleteEmployee/5
        [HttpPost]
        public IActionResult DeleteEmployee(int id)
        {
            var employee = _context.Customers.FirstOrDefault(c => c.CustomerId == id);
            if (employee == null)
                return NotFound();

            _context.Customers.Remove(employee);
            _context.SaveChanges();

            return RedirectToAction("ManageEmployees");
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

                // If ModelState is not valid, re-populate ViewBag.Categories before returning the view
                ViewBag.Categories = _context.Categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToList();
                return View(model); // Return the model to the view for correction
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
