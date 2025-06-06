using Microsoft.AspNetCore.Mvc;
using GameCraft.Data;
using GameCraft.Models;
using System.Linq;
using GameCraft.Helpers;

namespace GameCraft.Controllers
{
    public class AccountController : Controller
    {
        private readonly GameCraftDbContext _context;

        public AccountController(GameCraftDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // check email
                var existingCustomer = _context.Customers.FirstOrDefault(c => c.Email == model.Email);
                if (existingCustomer != null)
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(model);  //  return
                }

                // Hash + Salt
                string passwordHash, salt;
                (passwordHash, salt) = PasswordHelper.HashPassword(model.Password);

                
                var customer = new Customer
                {
                    Name = model.Username,
                    Email = model.Email,
                    PasswordHash = passwordHash,
                    Salt = salt,
                    Phone = "",
                    Address = "",
                    City = "",
                    PostCode = ""
                };

                _context.Customers.Add(customer);
                _context.SaveChanges();


                ViewBag.RegistrationSuccess = true;

                return View(model);
            }

            
            return View(model);
        }



        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var customer = _context.Customers.FirstOrDefault(c => c.Email == email);
            if (customer == null || !PasswordHelper.VerifyPassword(password, customer.PasswordHash, customer.Salt))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View();
            }

            // Here you can set up authentication cookies or session as needed

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/MyAccount
        [HttpGet]
        public IActionResult MyAccount()
        {
            // Retrieve the customer data from the database or session
            var customer = new Customer(); // Replace with actual retrieval logic
            return View(customer);
        }

        // POST: /Account/UpdateAccount
        [HttpPost]
        public IActionResult UpdateAccount(Customer customer, bool isEmployee)
        {
            // Set RoleId based on employee status
            customer.UserType = isEmployee ? 2 : 1; // 2 for Employee, 1 for Customer
            if (ModelState.IsValid)
            {
                // Save the customer data to the database
                _context.Customers.Update(customer);
                _context.SaveChanges();
                return RedirectToAction("Index", "Home"); // Redirect after successful update
            }
            return View("MyAccount", customer); // Return to the view with validation errors
        }
    }
}
