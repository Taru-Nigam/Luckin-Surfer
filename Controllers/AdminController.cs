using Microsoft.AspNetCore.Mvc;
using GameCraft.Data;
using GameCraft.Models;
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

        // GET: /Admin/ManageEmployees
        public IActionResult ManageEmployees()
        {
            // Fetch all customers who are employees (UserType for Employee)
            // Get the Employee role's Id first
            var employeeRole = _context.RoleId.FirstOrDefault(rt => rt.Name == "Employee");
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

        // GET: /Admin/EditEmployee/5
        public IActionResult EditEmployee(int id)
        {
            var employee = _context.Customers.FirstOrDefault(c => c.CustomerId == id);
            if (employee == null)
            {
                return NotFound();
            }

            ViewBag.UserTypes = _context.RoleId.ToList(); // List of all roles for permission editing

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
                if (_context.RoleId.Any(rt => rt.Id == editedEmployee.UserType))
                {
                    existingEmployee.UserType = editedEmployee.UserType;
                }

                _context.Customers.Update(existingEmployee);
                _context.SaveChanges();

                return RedirectToAction("ManageEmployees");
            }

            ViewBag.UserTypes = _context.RoleId.ToList();
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
            var prizes = _context.Products.ToList();
            return View(prizes);
        }

        // GET: /Admin/AddOrEditPrize/{id?}
        public IActionResult AddOrEditPrize(int? id)
        {
            // Populate categories for the dropdown
            ViewBag.Categories = _context.Categories.ToList();

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

        // POST: /Admin/AddOrEditPrize/{id?}
        [HttpPost]
        public IActionResult AddOrEditPrize(int? id, Product model)
        {
            if (ModelState.IsValid)
            {
                if (id == null || id == 0)
                {
                    // Add
                    _context.Products.Add(model);
                    _context.SaveChanges();
                    TempData["Message"] = "Prize added successfully!";
                }
                else
                {
                    // Edit
                    var existingPrize = _context.Products.FirstOrDefault(p => p.ProductId == id);
                    if (existingPrize == null)
                        return NotFound();

                    existingPrize.Name = model.Name;
                    existingPrize.Description = model.Description;
                    existingPrize.Price = model.Price;
                    existingPrize.CategoryId = model.CategoryId;

                    _context.Products.Update(existingPrize);
                    _context.SaveChanges();
                    TempData["Message"] = "Prize updated successfully!";
                }
                return RedirectToAction("ManagePrizes");
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View(model);
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

        // Optional: Admin dashboard or home redirect
        public IActionResult Index()
        {
            return RedirectToAction("ManageEmployees");
        }
    }
}

