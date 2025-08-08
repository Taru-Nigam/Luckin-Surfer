using GameCraft.Data;
using GameCraft.Helpers;
using GameCraft.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

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

            if (user.UserType == 2) // Assuming 2 is the UserType for employees
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Role, "Employee"), // Assign the "Employee" role
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                // --- NEW: Log employee login ---
                await _context.AuditLogs.AddAsync(new AuditLog
                {
                    Action = $"Employee '{user.Name}' logged in.",
                    EmployeeName = user.Name,
                    Timestamp = DateTime.Now
                });
                await _context.SaveChangesAsync();
                // --- END NEW ---

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

            Console.WriteLine($"Admin Key Entered: {adminKey}");

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

                // --- NEW: Log admin login (if using hardcoded key) ---
                await _context.AuditLogs.AddAsync(new AuditLog
                {
                    Action = "Admin logged in (via hardcoded key).",
                    EmployeeName = "Admin", // Or a more specific admin user name if you have one
                    Timestamp = DateTime.Now
                });
                await _context.SaveChangesAsync();
                // --- END NEW ---

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

                // --- NEW: Log admin login (if using database key) ---
                await _context.AuditLogs.AddAsync(new AuditLog
                {
                    Action = $"Admin '{adminUser.Name}' logged in (via database key).",
                    EmployeeName = adminUser.Name,
                    Timestamp = DateTime.Now
                });
                await _context.SaveChangesAsync();
                // --- END NEW ---

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
            // --- NEW: Log logout ---
            string employeeName = User.Identity?.Name ?? "Unknown";
            await _context.AuditLogs.AddAsync(new AuditLog
            {
                Action = $"Employee '{employeeName}' logged out.",
                EmployeeName = employeeName,
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();
            // --- END NEW ---

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login"); // Redirect back to the employee login page after logout
        }

        // --- SECURED ACTIONS ---
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.IsAdmin = User.IsInRole("Admin");

            // --- Dashboard Data Fetching ---

            // 1. Current Stock Level (Overall Inventory Status)
            var totalQuantityInStock = await _context.Products.SumAsync(p => p.Quantity);

            string stockStatus;
            if (totalQuantityInStock > 100)
            {
                stockStatus = "HIGH";
            }
            else if (totalQuantityInStock > 30)
            {
                stockStatus = "MEDIUM";
            }
            else
            {
                stockStatus = "LOW";
            }
            ViewBag.CurrentStockLevel = stockStatus;


            // 2. Prizes Redeemed Today
            // Use the new RedeemedPrize table
            var today = DateTime.Today;
            var prizesRedeemedToday = await _context.RedeemedPrizes
                                                    .Where(rp => rp.RedemptionDate.Date == today)
                                                    .SumAsync(rp => rp.Product.Price > 0 ? 1 : 0); // Count distinct prize redemptions, or sum a quantity if RedeemedPrize has a quantity. For simplicity, counting each record as 1 prize redeemed. If prizes are multi-item redemptions, adjust this sum.

            ViewBag.PrizesRedeemedToday = prizesRedeemedToday;


            // 3. Tickets Collected Today
            // Fetch from the new DailyTicketCollection table
            var dailyTicketCollection = await _context.DailyTicketCollections
                                                      .FirstOrDefaultAsync(dtc => dtc.CollectionDate.Date == today);

            ViewBag.TicketsCollectedToday = dailyTicketCollection?.TotalTicketsCollected.ToString("N0") ?? "0"; // Display 0 if no entry for today


            // You can also pass a list of low stock items if you want to display them on the dashboard
            var lowStockPrizesList = await _context.Products
                                                    .Where(p => p.Quantity > 0 && p.Quantity < 5) // Example: less than 5 items, and not out of stock
                                                    .OrderBy(p => p.Name)
                                                    .ToListAsync();
            ViewBag.LowStockPrizesList = lowStockPrizesList;


            // You can now pass these values directly to the view or use a ViewModel if more complex
            // For dashboard, ViewBag is fine since it's a collection of disparate values.
            return View();
        }

        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> PrizeStock()
        {
            var products = await _context.Products
                                         .OrderBy(p => p.ProductId)
                                         .ToListAsync();

            return View(products);
        }

        // API Endpoint for updating product quantity
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

            int oldQuantity = productToUpdate.Quantity; // Store old quantity for audit log
            productToUpdate.Quantity = request.Quantity;

            try
            {
                await _context.SaveChangesAsync();

                // --- NEW: Log prize quantity update ---
                string employeeName = User.Identity?.Name ?? "Unknown";
                await _context.AuditLogs.AddAsync(new AuditLog
                {
                    Action = $"Updated stock for prize '{productToUpdate.Name}' from {oldQuantity} to {productToUpdate.Quantity}.",
                    EmployeeName = employeeName,
                    Timestamp = DateTime.Now
                });
                await _context.SaveChangesAsync();
                // --- END NEW ---

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
        public async Task<IActionResult> AddPrize(string prizeName, int quantity, string imageUrl, decimal ticketCost)
        {
            // Assuming you have a form for this and 'imageUrl' might be a file upload,
            // or a direct URL. For simplicity, let's assume it's a URL for now.
            // If you're handling image uploads, the logic will be more complex.

            // Example: Create a new Product (Prize) object
            var newPrize = new Product
            {
                Name = prizeName,
                Quantity = quantity,
                Price = ticketCost, // Using 'Price' for ticket cost
                CategoryId = 1, // You might want to allow selecting a category
                ImageData = null // If you're using ImageUrl, you'll need to load it here
            };

            _context.Products.Add(newPrize);
            await _context.SaveChangesAsync();

            // --- NEW: Log prize addition ---
            string employeeName = User.Identity?.Name ?? "Unknown";
            await _context.AuditLogs.AddAsync(new AuditLog
            {
                Action = $"Added new prize: '{newPrize.Name}' (Quantity: {newPrize.Quantity}, Cost: {newPrize.Price} tickets).",
                EmployeeName = employeeName,
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();
            // --- END NEW ---

            ViewBag.Message = $"Prize '{prizeName}' added successfully!";
            return View(); // Or RedirectToAction("PrizeStock") to show updated list
        }


        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> RedeemedPrizes()
        {
            // Fetch redeemed prizes, including associated product and customer data
            var redeemedPrizes = await _context.RedeemedPrizes
                                               .Include(rp => rp.Product) // Eager load Product details
                                               .Include(rp => rp.Customer) // Eager load Customer details
                                               .OrderByDescending(rp => rp.RedemptionDate)
                                               .Take(50) // Limit to the last 50 for performance
                                               .ToListAsync();

            return View(redeemedPrizes); // Pass the list of RedeemedPrize objects to the view
        }

        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> AuditLog()
        {
            // Fetch audit logs, ordered by newest first
            var auditLogs = await _context.AuditLogs
                                          .OrderByDescending(log => log.Timestamp)
                                          .Take(100) // Limit to the last 100 for performance
                                          .ToListAsync();

            return View(auditLogs); // Pass the list of AuditLog objects to the view
        }

        // --- NEW: Action to handle a prize redemption (example) ---
        // This would typically be called from a form or an API endpoint when a player redeems a prize.
        [HttpPost]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> ProcessPrizeRedemption(int productId, int customerId, int ticketsSpent)
        {
            var product = await _context.Products.FindAsync(productId);
            var customer = await _context.Customers.FindAsync(customerId);

            if (product == null)
            {
                return NotFound("Prize not found.");
            }
            if (customer == null)
            {
                return NotFound("Customer not found.");
            }
            if (product.Quantity < 1)
            {
                return BadRequest("Prize is out of stock.");
            }
            if (customer.PrizePoints < ticketsSpent) // Assuming 'PrizePoints' is customer's tickets
            {
                return BadRequest("Customer does not have enough tickets.");
            }

            // Deduct tickets from customer
            customer.PrizePoints -= ticketsSpent;
            // Decrease prize quantity
            product.Quantity--;

            // Record the redemption
            var redeemedPrize = new RedeemedPrize
            {
                ProductId = productId,
                CustomerId = customerId,
                TicketsSpent = ticketsSpent,
                RedemptionDate = DateTime.Now,
                EmployeeName = User.Identity?.Name ?? "Unknown"
            };
            _context.RedeemedPrizes.Add(redeemedPrize);

            await _context.SaveChangesAsync();

            // --- NEW: Log prize redemption ---
            string employeeName = User.Identity?.Name ?? "Unknown";
            await _context.AuditLogs.AddAsync(new AuditLog
            {
                Action = $"Prize '{product.Name}' redeemed by '{customer.Name}' for {ticketsSpent} tickets.",
                EmployeeName = employeeName,
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();
            // --- END NEW ---

            return Ok($"Prize '{product.Name}' redeemed successfully for {customer.Name}.");
            // Or redirect to a confirmation page
        }

        // --- NEW: Action to handle daily ticket collection update (example) ---
        // This could be called by a button, or a scheduled task.
        [HttpPost]
        [Authorize(Roles = "Employee,Admin")] // Only employees/admins can manually update this
        public async Task<IActionResult> UpdateDailyTickets(long collectedAmount)
        {
            var today = DateTime.Today;
            var dailyCollection = await _context.DailyTicketCollections
                                                .FirstOrDefaultAsync(dtc => dtc.CollectionDate.Date == today);

            if (dailyCollection == null)
            {
                dailyCollection = new DailyTicketCollection
                {
                    CollectionDate = today,
                    TotalTicketsCollected = collectedAmount
                };
                _context.DailyTicketCollections.Add(dailyCollection);
            }
            else
            {
                dailyCollection.TotalTicketsCollected += collectedAmount; // Add to existing count for the day
            }
            await _context.SaveChangesAsync();

            // --- NEW: Log daily ticket collection update ---
            string employeeName = User.Identity?.Name ?? "Unknown";
            await _context.AuditLogs.AddAsync(new AuditLog
            {
                Action = $"Updated daily ticket collection. Added {collectedAmount} tickets. New total for {today.ToShortDateString()}: {dailyCollection.TotalTicketsCollected}.",
                EmployeeName = employeeName,
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();
            // --- END NEW ---

            return Ok($"Daily tickets updated. Total for today: {dailyCollection.TotalTicketsCollected}");
        }


        // Optional: An Access Denied page for when a user is authenticated but lacks the required role
        // [HttpGet]
        // public IActionResult AccessDenied()
        // {
        //    return View();
        // }
    }

    // This DTO (Data Transfer Object) is used to receive data from the JavaScript fetch request.
    public class UpdateQuantityRequest
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
    }
}