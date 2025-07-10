using Microsoft.AspNetCore.Authentication; // Required for HttpContext.SignInAsync
using Microsoft.AspNetCore.Authentication.Cookies; // Required for CookieAuthenticationDefaults
using Microsoft.AspNetCore.Authorization; // Required for [Authorize]
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims; // Required for Claims
using GameCraft.Data;
using GameCraft.Models;
using Microsoft.EntityFrameworkCore;

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

        // If you are using a database for employees, uncomment and inject your DbContext here
        // private readonly ApplicationDbContext _context; // Replace ApplicationDbContext with your actual DbContext name
        // public EmployeeController(ApplicationDbContext context)
        // {
        //     _context = context;
        // }

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
        public async Task<IActionResult> Login(string username, string password, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // --- IMPORTANT: This is for demonstration. In a real app, fetch from DB and hash passwords ---
            // Example: Simple "employee" username and "password" for employee login
            if (username == "employee" && password == "password")
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, "Employee"), // Assign the "Employee" role
                    // You could add a ClaimTypes.NameIdentifier here if you had an EmployeeId from a DB
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // Keep user logged in across browser sessions
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60) // Cookie expiration (adjust as needed)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Redirect to the original URL they tried to access, or to the Dashboard
                if (Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Dashboard"); // Redirect to your employee dashboard
            }

            ModelState.AddModelError("", "Invalid username or password.");
            return View();
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