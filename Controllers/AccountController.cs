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
        public async Task<IActionResult> UpdateAccount(Customer customer, IFormFile avatarFile)
        {
            if (!ModelState.IsValid)
            {
                return View("MyAccount", customer);
            }

            // Check whether email has been taken
            var existingEmail = _context.Customers
                .FirstOrDefault(c => c.Email == customer.Email && c.CustomerId != customer.CustomerId);
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "This email is already in use.");
                return View("MyAccount", customer);
            }

            // Check username has been taken
            var existingName = _context.Customers
                .FirstOrDefault(c => c.Name == customer.Name && c.CustomerId != customer.CustomerId);
            if (existingName != null)
            {
                ModelState.AddModelError("Name", "This username is already taken.");
                return View("MyAccount", customer);
            }

            // Keep hidden data not be replaced by null
            var existingCustomer = _context.Customers.FirstOrDefault(c => c.CustomerId == customer.CustomerId);
            if (existingCustomer == null)
            {
                return NotFound();
            }

            // Update data
            existingCustomer.Name = customer.Name;
            existingCustomer.Email = customer.Email;
            existingCustomer.Phone = customer.Phone;
            existingCustomer.Address = customer.Address;
            existingCustomer.City = customer.City;
            existingCustomer.PostCode = customer.PostCode;

            // Handle avatar file upload
            if (avatarFile != null && avatarFile.Length > 0)
            {
                // Define the path to save the uploaded file
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/avatars");

                // Check if the directory exists, if not, create it
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(avatarFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(fileStream);
                }

                // Update the AvatarUrl in the customer object
                existingCustomer.AvatarUrl = "/images/avatars/" + uniqueFileName; // Update with the new path
            }

            _context.Customers.Update(existingCustomer);
            await _context.SaveChangesAsync();

            // Update Session
            HttpContext.Session.SetString("User Name", existingCustomer.Name);
            HttpContext.Session.SetString("Email", existingCustomer.Email);
            HttpContext.Session.SetString("AvatarUrl", existingCustomer.AvatarUrl ?? "/images/default-avatar.png");

            TempData["SuccessMessage"] = "Account updated successfully.";
            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        public IActionResult ChangePassword(string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return RedirectToAction("MyAccount");
            }

            var email = HttpContext.Session.GetString("Email");
            var customer = _context.Customers.FirstOrDefault(c => c.Email == email);
            if (customer == null)
            {
                return RedirectToAction("Login"); // Redirect to login if customer not found
            }

            // Hash the new password
            string passwordHash, salt;
            (passwordHash, salt) = PasswordHelper.HashPassword(newPassword);

            // Update the customer's password
            customer.PasswordHash = passwordHash;
            customer.Salt = salt;

            _context.Customers.Update(customer);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction("MyAccount"); // Redirect to MyAccount to show the success message
        }

        // GET: /Account/GetUser names
        [HttpGet]
        public IActionResult GetUser(string searchTerm)
        {
            var usernames = _context.Customers
                .Where(c => c.Name.Contains(searchTerm))
                .Select(c => c.Name)
                .ToList();
            return Json(usernames);
        }
        // POST: /Account/ConnectAccount
        [HttpPost]
        public IActionResult ConnectAccount(string cardNumber, string username)
        {
            var customer = _context.Customers.FirstOrDefault(c => c.Name == username);
            if (customer == null)
            {
                return Json(new { success = false, message = "Username not found." });
            }
            // Here you would typically save the card number to the customer's record
            // Assuming you have a property in the Customer model for the card number
            customer.GameCraftCardNumber = cardNumber; // Add this property to your Customer model
            _context.Customers.Update(customer);
            _context.SaveChanges();
            return Json(new { success = true });
        }
    }
}