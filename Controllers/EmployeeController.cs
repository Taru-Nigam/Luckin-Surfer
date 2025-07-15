using GameCraft.Data;
using GameCraft.Helpers;
using GameCraft.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Required for ToListAsync, FirstOrDefaultAsync
using System.Security.Claims;
using System.Collections.Generic; // Required for List<Claim>
using System.Linq; // Required for .FirstOrDefaultAsync
using System.Threading.Tasks; // Required for Task<IActionResult> and async/await
using System; // Required for Console.WriteLine, Exception

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

            var user = await _context.Customers
                .FirstOrDefaultAsync(u => u.Email == email || u.Name == username);

            if (user == null || !PasswordHelper.VerifyPassword(password, user.PasswordHash, user.Salt))
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View();
            }

            if (user.UserType == 2)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Role, "Employee"),
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
                return RedirectToAction("Dashboard");
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
                return View("Login");
            }

            Console.WriteLine($"Admin Key Entered: {adminKey}");

            if (adminKey == ValidAdminKey)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "Admin"),
                    new Claim(ClaimTypes.Role, "Admin"),
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                return RedirectToAction("Index", "Admin");
            }

            var adminUser = await _context.Customers.FirstOrDefaultAsync(c => c.AdminKey == adminKey);
            if (adminUser != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, adminUser .Name),
                    new Claim(ClaimTypes.Role, "Admin"),
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                return RedirectToAction("Index", "Admin");
            }
            else
            {
                ModelState.AddModelError("", "Invalid admin key.");
                return View("Login");
            }
        }


        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // --- SECURED ACTIONS ---

        [Authorize(Roles = "Employee,Admin")]
        public IActionResult Dashboard()
        {
            ViewBag.IsAdmin = User.IsInRole("Admin");
            return View();
        }

        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> PrizeStock() // <-- This action was updated
        {
            // Fetch all products from the database, ordered by ProductId
            var products = await _context.Products
                                       .OrderBy(p => p.ProductId)
                                       .ToListAsync();

            // Pass the list of Product models to the view
            return View(products); // <-- Correctly passing the data to the view
        }

        // API Endpoint for updating product quantity <-- This action was added
        [HttpPost("api/prizes/updatequantity")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> UpdatePrizeQuantity([FromBody] UpdateQuantityRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request data.");
            }

            var productToUpdate = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == request.Id);

            if (productToUpdate == null)
            {
                return NotFound($"Product with ID {request.Id} not found.");
            }

            if (request.Quantity < 0)
            {
                return BadRequest("Quantity cannot be negative.");
            }

            productToUpdate.Quantity = request.Quantity;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Quantity updated successfully!", newQuantity = productToUpdate.Quantity });
            }
            catch (DbUpdateException ex)
            {
                Console.Error.WriteLine($"Database update error for ProductId {request.Id}: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Error saving changes to the database.", detailedError = ex.Message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An unexpected error occurred for ProductId {request.Id}: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An unexpected error occurred.", detailedError = ex.Message });
            }
        }


        [Authorize(Roles = "Employee,Admin")]
        public IActionResult AddPrize()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Employee,Admin")]
        public IActionResult AddPrize(string prizeName, int quantity, string imageUrl, decimal ticketCost)
        {
            // Placeholder for AddPrize logic
            ViewBag.Message = "Prize '" + prizeName + "' added successfully!";
            return View();
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
    }

    // This DTO (Data Transfer Object) is used to receive data from the JavaScript fetch request.
    public class UpdateQuantityRequest
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
    }
}
