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
                // Check if the email is already registered
                var existingCustomer = _context.Customers.FirstOrDefault(c => c.Email == model.Email);
                if (existingCustomer != null)
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(model);  // Return with error
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

                // Automatically log in the user after registration using session
                HttpContext.Session.SetString("UserName", customer.Name);
                HttpContext.Session.SetString("Email", customer.Email);
                HttpContext.Session.SetString("PrizePoints", customer.PrizePoints.ToString()); // Initialize PrizePoints if needed
                HttpContext.Session.SetString("AvatarUrl", customer.AvatarUrl ?? "/images/default-avatar.png");

                ViewBag.RegistrationSuccess = true;

                return RedirectToAction("Index", "Home"); // Redirect to home after successful registration and login
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public IActionResult Login(string login, string password)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Login and password are required.");
                return View();
            }

            Customer customer = null;

            // Simple check: if input contains '@' and '.', assume it's an email
            if (login.Contains("@") && login.Contains("."))
            {
                customer = _context.Customers.FirstOrDefault(c => c.Email == login);
            }
            else
            {
                customer = _context.Customers.FirstOrDefault(c => c.Name == login);
            }

            if (customer == null || !PasswordHelper.VerifyPassword(password, customer.PasswordHash, customer.Salt))
            {
                ModelState.AddModelError("", "Invalid login or password.");
                return View();
            }

            // Store user info in session, or sign in as per your setup
            HttpContext.Session.SetString("UserName", customer.Name);
            HttpContext.Session.SetString("Email", customer.Email);
            HttpContext.Session.SetString("PrizePoints", customer.PrizePoints.ToString());
            HttpContext.Session.SetString("AvatarUrl", customer.AvatarUrl ?? "/images/default-avatar.png");

            return RedirectToAction("Index", "Home");
        }


        // GET: /Account/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/MyAccount
        [HttpGet]
        public IActionResult MyAccount()
        {
            // Retrieve the customer data from the session
            var userName = HttpContext.Session.GetString("UserName");
            var email = HttpContext.Session.GetString("Email");

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login"); // Redirect to login if not authenticated
            }

            var customer = _context.Customers.FirstOrDefault(c => c.Email == email);
            if (customer == null)
            {
                return RedirectToAction("Login"); // Redirect to login if customer not found
            }

            // Retrieve PrizePoints from session and convert to int
            var prizePointsString = HttpContext.Session.GetString("PrizePoints");
            if (prizePointsString != null && int.TryParse(prizePointsString, out int prizePoints))
            {
                customer.PrizePoints = prizePoints; // Set the PrizePoints in the customer object
            }
            return View(customer);
        }

        // POST: /Account/UpdateAccount
        [HttpPost]
        public IActionResult UpdateAccount(Customer customer)
        {
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
