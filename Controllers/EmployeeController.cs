using GameCraft.Data;
using GameCraft.Helpers;
using GameCraft.Models;
using Microsoft.AspNetCore.Authentication; // Required for HttpContext.SignInAsync
using Microsoft.AspNetCore.Authentication.Cookies; // Required for CookieAuthenticationDefaults
using Microsoft.AspNetCore.Authorization; // Required for [Authorize]
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims; // Required for Claims

// You'll need your DbContext if you're checking credentials against a database
// using GameCraft.Data; // Example: assuming your DbContext is in GameCraft.Data
// using GameCraft.Models; // Example: assuming your Employee/Admin models are in GameCraft.Models

namespace GameCraft.Controllers
{
    public class EmployeeController : Controller
    {

        private readonly GameCraftDbContext _context;

        public EmployeeController(GameCraftDbContext context)
        {
            _context = context;
        }
        private const string ValidAdminKey = "YourSecureAdminKey123"; // Make sure this is a secure, secret key

        [HttpGet]
        public IActionResult Login()
        {
            // Fix for CS8602: Check if User.Identity is not null before accessing IsAuthenticated
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Employee"))
                {
                    return RedirectToAction("Dashboard");
                }
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
            }
            return View(); // Display your Login.cshtml view
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string email, string password, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // Fetch user from the database based on username or email
            var user = await _context.Customers
                .FirstOrDefaultAsync(u => u.Email == email || u.Name == username); // Corrected LINQ query

            // Check if user exists and verify password
            if (user == null || !PasswordHelper.VerifyPassword(password, user.PasswordHash, user.Salt)) // Verify password
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View();
            }

            // Check if the user has the appropriate UserType (e.g., UserType 2 for employees)
            if (user.UserType == 2) // Assuming 2 is the UserType for employees
            {
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, user.Email), // Use the user's email
            new Claim(ClaimTypes.Name, user.Name), // Use the user's name
            new Claim(ClaimTypes.Role, "Employee"), // Assign the "Employee" role
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                if (Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Dashboard"); // Redirect to the employee dashboard
            }
            else
            {
                ModelState.AddModelError("", "You do not have permission to access this area.");
                return View();
            }
        }


        [HttpPost]
        public async Task<IActionResult> AdminLogin(string adminKey, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(adminKey))
            {
                ModelState.AddModelError("", "Admin key is required.");
                return View("Login"); // Return to the login view if key is empty
            }

            // Log the admin key for debugging (REMOVE THIS IN PRODUCTION!)
            Console.WriteLine($"Admin Key Entered: {adminKey}");

            // Check if the entered admin key matches the static valid admin key
            if (adminKey == ValidAdminKey)
            {
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Admin"), // Name for the authenticated Admin
            new Claim(ClaimTypes.Role, "Admin"), // Assign the "Admin" role
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                return RedirectToAction("Index", "Admin"); // Redirect to the admin dashboard
            }

            // Check if the entered admin key matches the stored admin key in the database
            var adminUser = await _context.Customers.FirstOrDefaultAsync(c => c.AdminKey == adminKey);
            if (adminUser != null)
            {
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, adminUser .Name), // Use the admin's name
            new Claim(ClaimTypes.Role, "Admin"), // Assign the "Admin" role
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                return RedirectToAction("Index", "Admin"); // Redirect to the admin dashboard
            }
            else
            {
                ModelState.AddModelError("", "Invalid admin key.");
                return View("Login"); // Return to the login view if key is incorrect
            }
        }


        [HttpPost] // Logout action
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login"); // Redirect back to the employee login page after logout
        }

        // --- SECURED ACTIONS ---
        // These actions will now only be accessible to authenticated users with "Employee" or "Admin" roles

        [Authorize(Roles = "Employee,Admin")] // Requires "Employee" OR "Admin" role
        public IActionResult Dashboard()
        {
            // You can still use ViewBag.IsAdmin if your view needs to render different UI
            // based on whether it's an admin or a regular employee
            ViewBag.IsAdmin = User.IsInRole("Admin");
            return View();
        }

        [Authorize(Roles = "Employee,Admin")]
        public IActionResult PrizeStock()
        {
            return View();
        }

        [Authorize(Roles = "Employee,Admin")]
        public IActionResult AddPrize()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Employee,Admin")] // Don't forget to protect POST actions too!
        public IActionResult AddPrize(string prizeName, int quantity, string imageUrl, decimal ticketCost)
        {
            // Your prize adding logic goes here (e.g., saving to database)
            // Example if you uncommented DbContext:
            // var newPrize = new GameCraft.Models.Prize // Replace with your actual Prize model name
            // {
            //     Name = prizeName,
            //     Quantity = quantity,
            //     ImageUrl = imageUrl,
            //     Price = ticketCost // Assuming 'Price' field for ticket cost
            // };
            // _context.Prizes.Add(newPrize);
            // _context.SaveChanges();

            ViewBag.Message = "Prize '" + prizeName + "' added successfully!";
            return View(); // Or RedirectToAction("ManagePrizes") to show updated list
        }

        [Authorize(Roles = "Employee,Admin")]
        public IActionResult RedeemedPrizes()
        {
            return View();
        }

        [Authorize(Roles = "Employee,Admin")]
        public IActionResult AuditLog()
        {
            return View();
        }

        // Optional: An Access Denied page for when a user is authenticated but lacks the required role
        // [HttpGet]
        // public IActionResult AccessDenied()
        // {
        //    return View();
        // }
    }
}