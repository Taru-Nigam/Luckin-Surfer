using GameCraft.Data;
using GameCraft.Helpers;
using GameCraft.Models;
using Microsoft.AspNetCore.Hosting; // For IWebHostEnvironment
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // For async operations
using System.IO; // For FileStream, Path
using System.Linq;
using System.Threading.Tasks;

namespace GameCraft.Controllers
{
    public class AccountController : Controller
    {
        private readonly GameCraftDbContext _context;
        private readonly IWebHostEnvironment _env; // Inject IWebHostEnvironment

        public AccountController(GameCraftDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Helper to get default avatar image data
        private async Task<byte[]> GetDefaultAvatarImageData()
        {
            var defaultAvatarPath = Path.Combine(_env.WebRootPath, "images", "default-avatar.png");
            if (System.IO.File.Exists(defaultAvatarPath))
            {
                return await System.IO.File.ReadAllBytesAsync(defaultAvatarPath);
            }
            return null; // Or throw an exception if default avatar is mandatory
        }

        // Action to serve avatar image data from the database
        [HttpGet]
        public async Task<IActionResult> GetAvatarImage(int customerId)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer?.AvatarImageData != null && customer.AvatarImageData.Length > 0)
            {
                // Attempt to determine content type (basic check)
                string contentType = "image/png"; // Default to PNG
                if (customer.AvatarImageData.Length > 4)
                {
                    if (customer.AvatarImageData[0] == 0xFF && customer.AvatarImageData[1] == 0xD8)
                        contentType = "image/jpeg";
                    else if (customer.AvatarImageData[0] == 0x89 && customer.AvatarImageData[1] == 0x50 && customer.AvatarImageData[2] == 0x4E && customer.AvatarImageData[3] == 0x47)
                        contentType = "image/png";
                }
                return File(customer.AvatarImageData, contentType);
            }
            // Serve default avatar if no specific avatar is found or data is empty
            var defaultAvatarData = await GetDefaultAvatarImageData();
            if (defaultAvatarData != null)
            {
                return File(defaultAvatarData, "image/png"); // Assuming default is PNG
            }
            return NotFound(); // Fallback if no image can be served
        }

        // Action to serve the default avatar image directly
        [HttpGet]
        public async Task<IActionResult> GetDefaultAvatar()
        {
            var defaultAvatarData = await GetDefaultAvatarImageData();
            if (defaultAvatarData != null)
            {
                return File(defaultAvatarData, "image/png"); // Assuming default is PNG
            }
            return NotFound();
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if the email is already registered
                var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == model.Email);
                if (existingCustomer != null)
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(model);  // Return with error
                }

                // Hash + Salt
                string passwordHash, salt;
                (passwordHash, salt) = PasswordHelper.HashPassword(model.Password);

                // Get default avatar image data
                byte[] defaultAvatarData = await GetDefaultAvatarImageData();

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
                    UserType = 1, // Set UserType to 1 for regular users
                    AvatarImageData = defaultAvatarData // Set default avatar data
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                // Automatically log in the user after registration using session
                HttpContext.Session.SetString("UserName", customer.Name);
                HttpContext.Session.SetString("Email", customer.Email);
                HttpContext.Session.SetString("PrizePoints", customer.PrizePoints.ToString()); // Initialize PrizePoints if needed
                // Store a URL to the avatar image, not the raw data
                HttpContext.Session.SetString("AvatarUrl", Url.Action("GetAvatarImage", "Account", new { customerId = customer.CustomerId }));

                ViewBag.RegistrationSuccess = true;

                CookieOptions options = new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddDays(7),
                    IsEssential = true
                };
                Response.Cookies.Append("UserToken", customer.CustomerId.ToString(), options);

                // Log the registration activity
                var auditLog = new AuditLog
                {
                    UserId = customer.CustomerId.ToString(),
                    UserName = customer.Name,
                    Action = "Registered",
                    Details = "User  registered successfully.",
                    Timestamp = DateTime.UtcNow,
                    UserRole = "Customer"
                };
                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                // Redirect to home page after successful registration and login
                return RedirectToAction("Index", "Home");
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
        public async Task<IActionResult> Login(string login, string password)
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
                customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == login);
            }
            else
            {
                customer = await _context.Customers.FirstOrDefaultAsync(c => c.Name == login);
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
            HttpContext.Session.SetString("UserName", customer.Name);
            HttpContext.Session.SetString("Email", customer.Email);
            HttpContext.Session.SetString("PrizePoints", customer.PrizePoints.ToString());
            // Store a URL to the avatar image, not the raw data
            HttpContext.Session.SetString("AvatarUrl", Url.Action("GetAvatarImage", "Account", new { customerId = customer.CustomerId }));

            CookieOptions options = new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(7),
                IsEssential = true
            };
            Response.Cookies.Append("UserToken", customer.CustomerId.ToString(), options);

            // Log the login activity
            var auditLog = new AuditLog
            {
                UserId = customer.CustomerId.ToString(),
                UserName = customer.Name,
                Action = "Logged In",
                Details = "User  logged in successfully.",
                Timestamp = DateTime.UtcNow,
                UserRole = "Customer"
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();
            Response.Cookies.Delete("UserToken");

            // Log the logout activity
            var userName = HttpContext.Session.GetString("UserName");
            var userId = HttpContext.Session.GetString("Email"); // Assuming Email is used as UserId
            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = "Logged Out",
                Details = "User  logged out successfully.",
                Timestamp = DateTime.UtcNow,
                UserRole = "Customer"
            };
            _context.AuditLogs.Add(auditLog);

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/MyAccount
        [HttpGet]
        public async Task<IActionResult> MyAccount()
        {
            // Retrieve the customer data from the session
            var userName = HttpContext.Session.GetString("UserName");
            var email = HttpContext.Session.GetString("Email");

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login"); // Redirect to login if not authenticated
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
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

        // POST: /Account/ConnectAccount
        [HttpPost]
        public async Task<IActionResult> ConnectAccount(string cardNumber, string username)
        {
            // Check if the username already exists
            var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Name == username);
            if (existingCustomer != null)
            {
                // Update existing user with the GameCraft card number
                existingCustomer.GameCraftCardNumber = cardNumber;
                await _context.SaveChangesAsync();

                // Automatically log in the existing user
                HttpContext.Session.SetString("UserName", existingCustomer.Name);
                HttpContext.Session.SetString("Email", existingCustomer.Email ?? "email@gamecraft.com");
                HttpContext.Session.SetString("PrizePoints", existingCustomer.PrizePoints.ToString());
                HttpContext.Session.SetString("AvatarUrl", Url.Action("GetAvatarImage", "Account", new { customerId = existingCustomer.CustomerId }));

                Response.Cookies.Append("UserToken", existingCustomer.CustomerId.ToString(), new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddDays(7),
                    IsEssential = true
                });

                return Json(new
                {
                    success = true,
                    message = "GameCraft card number added to existing account.",
                    customerId = existingCustomer.CustomerId,
                    redirectUrl = Url.Action("Index", "Home")  // Add redirect URL
                });
            }

            // Create a new user with placeholder email
            var defaultAvatarData = await GetDefaultAvatarImageData();
            var customer = new Customer
            {
                Name = username,
                Email = "email@gamecraft.com",
                PasswordHash = PasswordHelper.HashPassword("password").Item1,
                Salt = PasswordHelper.HashPassword("password").Item2,
                GameCraftCardNumber = cardNumber,
                UserType = 1,
                Phone = "",
                Address = "",
                City = "",
                PostCode = "",
                AvatarImageData = defaultAvatarData // Set default avatar data
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            // Full authentication setup (same as Register)
            HttpContext.Session.SetString("UserName", customer.Name);
            HttpContext.Session.SetString("Email", customer.Email);
            HttpContext.Session.SetString("PrizePoints", customer.PrizePoints.ToString());
            HttpContext.Session.SetString("AvatarUrl", Url.Action("GetAvatarImage", "Account", new { customerId = customer.CustomerId }));

            Response.Cookies.Append("UserToken", customer.CustomerId.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(7),
                IsEssential = true
            });

            return Json(new
            {
                success = true,
                message = "A new account has been created with the username: " + username + " and default password" + "You are logged in, Please Update your password in My Account -> Change password",
                customerId = customer.CustomerId,
                redirectUrl = Url.Action("Index", "Home")  // Add redirect URL
            });
        }

        // POST: /Account/UpdateAccount
        [HttpPost]
        public async Task<IActionResult> UpdateAccount(Customer customer, IFormFile avatarFile)
        {
            // Re-fetch the existing customer to ensure we don't overwrite PasswordHash, Salt, etc.
            var existingCustomer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
            if (existingCustomer == null)
            {
                return NotFound();
            }

            // Manually set the non-editable fields from the existing customer
            customer.PasswordHash = existingCustomer.PasswordHash;
            customer.Salt = existingCustomer.Salt;
            customer.UserType = existingCustomer.UserType;
            customer.PrizePoints = existingCustomer.PrizePoints;
            customer.GameCraftCardNumber = existingCustomer.GameCraftCardNumber;
            customer.AdminKey = existingCustomer.AdminKey;

            // Handle avatar file upload
            if (avatarFile != null && avatarFile.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await avatarFile.CopyToAsync(memoryStream);
                    customer.AvatarImageData = memoryStream.ToArray(); // Update the AvatarImageData
                }
            }
            else
            {
                // Retain existing image data if no new image is uploaded
                customer.AvatarImageData = existingCustomer.AvatarImageData;
            }

            // Validate the model after manually setting properties
            if (!ModelState.IsValid)
            {
                // If validation fails, return to the view with the model and errors
                // Ensure AvatarImageData is set back for display
                customer.AvatarImageData = existingCustomer.AvatarImageData; // Or the newly uploaded one if it was the issue
                return View("MyAccount", customer);
            }

            // Check if the email is already taken by another user
            var existingEmailUser = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == customer.Email && c.CustomerId != customer.CustomerId);
            if (existingEmailUser != null)
            {
                ModelState.AddModelError("Email", "This email is already in use by another account.");
                return View("MyAccount", customer);
            }

            // Check if the username is already taken by another user
            var existingNameUser = await _context.Customers
                .FirstOrDefaultAsync(c => c.Name == customer.Name && c.CustomerId != customer.CustomerId);
            if (existingNameUser != null)
            {
                ModelState.AddModelError("Name", "This username is already taken by another account.");
                return View("MyAccount", customer);
            }

            // Attach the updated customer entity and mark it as modified
            _context.Entry(customer).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Update Session
            HttpContext.Session.SetString("UserName", customer.Name);
            HttpContext.Session.SetString("Email", customer.Email);
            HttpContext.Session.SetString("AvatarUrl", Url.Action("GetAvatarImage", "Account", new { customerId = customer.CustomerId }));

            // Log the account update activity
            var auditLog = new AuditLog
            {
                UserId = customer.CustomerId.ToString(),
                UserName = customer.Name,
                Action = "Update Account",
                Details = $"User  updated account details. New: [Email: {customer.Email}, Username: {customer.Name}].",
                Timestamp = DateTime.UtcNow,
                UserRole = "Customer"
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Account updated successfully.";
            return RedirectToAction("MyAccount", new { activeSection = "accountDetails" });
        }

        // New action to handle avatar updates specifically from the modal
        [HttpPost]
        public async Task<IActionResult> UpdateAvatar(IFormFile? avatarFile, string? selectedAvatarPath)
        {
            var email = HttpContext.Session.GetString("Email");
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);

            if (customer == null)
            {
                return Json(new { success = false, message = "User  not found." });
            }

            byte[] newAvatarData = null;

            if (avatarFile != null && avatarFile.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await avatarFile.CopyToAsync(ms);
                    newAvatarData = ms.ToArray();
                }
            }
            else if (!string.IsNullOrEmpty(selectedAvatarPath))
            {
                // Read the pre-set avatar image from wwwroot
                var fullPath = Path.Combine(_env.WebRootPath, selectedAvatarPath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    newAvatarData = await System.IO.File.ReadAllBytesAsync(fullPath);
                }
            }
            else
            {
                // If neither file nor pre-set path is provided, keep current avatar
                return Json(new { success = false, message = "No new avatar selected or uploaded." });
            }

            customer.AvatarImageData = newAvatarData;
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            // Update session avatar URL
            HttpContext.Session.SetString("AvatarUrl", Url.Action("GetAvatarImage", "Account", new { customerId = customer.CustomerId }));

            // Log the avatar update activity
            var auditLog = new AuditLog
            {
                UserId = customer.CustomerId.ToString(),
                UserName = customer.Name,
                Action = "Change Avatar",
                Details = "User  changed their avatar.",
                Timestamp = DateTime.UtcNow,
                UserRole = "Customer"
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            return Json(new { success = true, newAvatarUrl = Url.Action("GetAvatarImage", "Account", new { customerId = customer.CustomerId }) });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                return Json(new { success = false, message = "Passwords do not match." });
            }

            var email = HttpContext.Session.GetString("Email");
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
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
            await _context.SaveChangesAsync();

            // Log the password change activity
            var auditLog = new AuditLog
            {
                UserId = customer.CustomerId.ToString(),
                UserName = customer.Name,
                Action = "Change Password",
                Details = "User  changed their password.",
                Timestamp = DateTime.UtcNow,
                UserRole = "Customer"
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Password changed successfully." });
        }
    }
}
