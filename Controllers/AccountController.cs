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
                    PostCode = "",
                    UserType = 1 // Set UserType to 1 for regular users
                };

                _context.Customers.Add(customer);
                _context.SaveChanges();

                // Automatically log in the user after registration using session
                HttpContext.Session.SetString("UserName", customer.Name);
                HttpContext.Session.SetString("Email", customer.Email);
                HttpContext.Session.SetString("PrizePoints", customer.PrizePoints.ToString()); // Initialize PrizePoints if needed
                HttpContext.Session.SetString("AvatarUrl", customer.AvatarUrl ?? "/images/default-avatar.png");

                ViewBag.RegistrationSuccess = true;

                CookieOptions options = new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddDays(7),
                    IsEssential = true
                };
                Response.Cookies.Append("UserToken", customer.CustomerId.ToString(), options);

                // Redirect to home page after successful registration and login
                return RedirectToAction("Index", "Home");
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
            // Check if the customer exists
            if (customer == null)
            {
                ModelState.AddModelError("", "User  does not exist."); // Add error message
                return View(); // Return to the login view with the error
            }
            // Verify the password
            if (!PasswordHelper.VerifyPassword(password, customer.PasswordHash, customer.Salt))
            {
                ModelState.AddModelError("", "Invalid login or password.");
                return View();
            }
            // Store user info in session
            HttpContext.Session.SetString("User Name", customer.Name);
            HttpContext.Session.SetString("Email", customer.Email);
            HttpContext.Session.SetString("PrizePoints", customer.PrizePoints.ToString());
            HttpContext.Session.SetString("AvatarUrl", customer.AvatarUrl ?? "/images/default-avatar.png");
            CookieOptions options = new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(7),
                IsEssential = true
            };
            Response.Cookies.Append("UserToken", customer.CustomerId.ToString(), options);
            return RedirectToAction("Index", "Home");
        }


        // GET: /Account/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();
            Response.Cookies.Delete("UserToken");
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
            
            if (!ModelState.IsValid)
            {
                return View("MyAccount", customer);
            }

            // check whether email been taken
            var existingEmail = _context.Customers
                .FirstOrDefault(c => c.Email == customer.Email && c.CustomerId != customer.CustomerId);
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "This email is already in use.");
                return View("MyAccount", customer);
            }

            // check username been taken
            var existingName = _context.Customers
                .FirstOrDefault(c => c.Name == customer.Name && c.CustomerId != customer.CustomerId);
            if (existingName != null)
            {
                ModelState.AddModelError("Name", "This username is already taken.");
                return View("MyAccount", customer);
            }

            // keep hidden data not be replaced by null
            var existingCustomer = _context.Customers.FirstOrDefault(c => c.CustomerId == customer.CustomerId);
            if (existingCustomer == null)
            {
                return NotFound();
            }

            // update data
            existingCustomer.Name = customer.Name;
            existingCustomer.Email = customer.Email;
            existingCustomer.Phone = customer.Phone;
            existingCustomer.Address = customer.Address;
            existingCustomer.City = customer.City;
            existingCustomer.PostCode = customer.PostCode;
            existingCustomer.AvatarUrl = customer.AvatarUrl;

            _context.Customers.Update(existingCustomer);
            _context.SaveChanges();

            // update Session
            HttpContext.Session.SetString("UserName", existingCustomer.Name);
            HttpContext.Session.SetString("Email", existingCustomer.Email);
            HttpContext.Session.SetString("AvatarUrl", existingCustomer.AvatarUrl ?? "/images/default-avatar.png");

            TempData["SuccessMessage"] = "Account updated successfully.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult ChangePassword(string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                return Json(new { success = false, message = "Passwords do not match." });
            }

            var email = HttpContext.Session.GetString("Email");
            var customer = _context.Customers.FirstOrDefault(c => c.Email == email);
            if (customer == null)
            {
                return Json(new { success = false, message = "User  not found." });
            }

            // Hash the new password
            string passwordHash, salt;
            (passwordHash, salt) = PasswordHelper.HashPassword(newPassword);

            // Update the customer's password
            customer.PasswordHash = passwordHash;
            customer.Salt = salt;

            _context.Customers.Update(customer);
            _context.SaveChanges();

            return Json(new { success = true, message = "Password changed successfully." });
        }

    }
}


       