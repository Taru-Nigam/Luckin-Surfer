using GameCraft.Data;
using GameCraft.Helpers;
using GameCraft.Models;
using GameCraft.Services; // Add this using directive
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GameCraft.Controllers
{
    public class AccountController : Controller
    {
        private readonly GameCraftDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService; // Inject IEmailService

        public AccountController(GameCraftDbContext context, IWebHostEnvironment env, IEmailService emailService)
        {
            _context = context;
            _env = env;
            _emailService = emailService;
        }

        // Helper to get default avatar image data
        private async Task<byte[]> GetDefaultAvatarImageData()
        {
            var defaultAvatarPath = Path.Combine(_env.WebRootPath, "images", "default-avatar.png");
            if (System.IO.File.Exists(defaultAvatarPath))
            {
                return await System.IO.File.ReadAllBytesAsync(defaultAvatarPath);
            }
            return null;
        }

        // Action to serve avatar image data from the database
        [HttpGet]
        public async Task<IActionResult> GetAvatarImage(int customerId)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer?.AvatarImageData != null && customer.AvatarImageData.Length > 0)
            {
                string contentType = "image/png";
                if (customer.AvatarImageData.Length > 4)
                {
                    if (customer.AvatarImageData[0] == 0xFF && customer.AvatarImageData[1] == 0xD8)
                        contentType = "image/jpeg";
                    else if (customer.AvatarImageData[0] == 0x89 && customer.AvatarImageData[1] == 0x50 && customer.AvatarImageData[2] == 0x4E && customer.AvatarImageData[3] == 0x47)
                        contentType = "image/png";
                }
                return File(customer.AvatarImageData, contentType);
            }
            var defaultAvatarData = await GetDefaultAvatarImageData();
            if (defaultAvatarData != null)
            {
                return File(defaultAvatarData, "image/png");
            }
            return NotFound();
        }

        // Action to serve the default avatar image directly
        [HttpGet]
        public async Task<IActionResult> GetDefaultAvatar()
        {
            var defaultAvatarData = await GetDefaultAvatarImageData();
            if (defaultAvatarData != null)
            {
                return File(defaultAvatarData, "image/png");
            }
            return NotFound();
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel()); // Pass a new view model
        }

        // POST: /Account/Register - Handles initial registration and OTP sending
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == model.Email);
                if (existingCustomer != null)
                {
                    // If user exists but not verified, allow them to resend OTP or verify
                    if (!existingCustomer.IsEmailVerified)
                    {
                        ModelState.AddModelError("Email", "This email is already registered but not verified. Please verify your email or resend OTP.");
                        // Store email in TempData to pre-fill OTP verification form
                        TempData["EmailForOtp"] = model.Email;
                        return View("Register", model); // Stay on register page, prompt for OTP
                    }
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(model);
                }

                string passwordHash, salt;
                (passwordHash, salt) = PasswordHelper.HashPassword(model.Password);

                byte[] defaultAvatarData = await GetDefaultAvatarImageData();

                // Generate OTP
                string otp = GenerateOtp();
                DateTime otpGeneratedTime = DateTime.UtcNow;

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
                    UserType = 1,
                    PrizePoints = 0, // Default prize points for regular registration
                    AvatarImageData = defaultAvatarData,
                    Otp = otp, // Store OTP
                    OtpGeneratedTime = otpGeneratedTime, // Store OTP generation time
                    IsEmailVerified = false // Mark as unverified
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                // Send OTP email
                var subject = "Your GameCraft Registration OTP";
                var message = $"Your One-Time Password (OTP) for GameCraft registration is: <b>{otp}</b>. This OTP is valid for 5 minutes.";
                try
                {
                    await _emailService.SendEmailAsync(customer.Email, subject, message);
                    TempData["SuccessMessage"] = "Registration successful! An OTP has been sent to your email. Please verify your account.";
                    TempData["EmailForOtp"] = customer.Email; // Pass email to the OTP verification view
                    return RedirectToAction("VerifyOtp", new { email = customer.Email });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to send OTP email. Please try again later.");
                    // Log the exception
                    Console.WriteLine($"Email sending error: {ex.Message}");
                    // Optionally, remove the customer if email sending is critical for registration
                    _context.Customers.Remove(customer);
                    await _context.SaveChangesAsync();
                    return View(model);
                }
            }

            return View(model);
        }

        // GET: /Account/SpecialRegister - Now in AccountController
        [HttpGet]
        public IActionResult SpecialRegister()
        {
            return View("~/Views/Account/SpecialRegister.cshtml", new RegisterViewModel());
        }

        // POST: /Account/SpecialRegister - Handles registration with 500 prize points
        [HttpPost]
        public async Task<IActionResult> SpecialRegister(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == model.Email);
                if (existingCustomer != null)
                {
                    if (!existingCustomer.IsEmailVerified)
                    {
                        ModelState.AddModelError("Email", "This email is already registered but not verified. Please verify your email or resend OTP.");
                        TempData["EmailForOtp"] = model.Email;
                        return View("~/Views/Account/SpecialRegister.cshtml", model);
                    }
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View("~/Views/Account/SpecialRegister.cshtml", model);
                }

                string passwordHash, salt;
                (passwordHash, salt) = PasswordHelper.HashPassword(model.Password);

                byte[] defaultAvatarData = await GetDefaultAvatarImageData();

                string otp = GenerateOtp();
                DateTime otpGeneratedTime = DateTime.UtcNow;

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
                    UserType = 1,
                    PrizePoints = 500, // Add 500 prize points for special registration
                    AvatarImageData = defaultAvatarData,
                    Otp = otp,
                    OtpGeneratedTime = otpGeneratedTime,
                    IsEmailVerified = false
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                var subject = "Your GameCraft Special Registration OTP";
                var message = $"Welcome! Your One-Time Password (OTP) for GameCraft special registration is: <b>{otp}</b>. This OTP is valid for 5 minutes. You will receive 500 prize points upon verification!";
                try
                {
                    await _emailService.SendEmailAsync(customer.Email, subject, message);
                    TempData["SuccessMessage"] = "Special registration successful! An OTP has been sent to your email. Please verify your account to claim your 500 prize points.";
                    TempData["EmailForOtp"] = customer.Email;
                    return RedirectToAction("VerifyOtp", new { email = customer.Email });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to send OTP email for special registration. Please try again later.");
                    Console.WriteLine($"Email sending error: {ex.Message}");
                    _context.Customers.Remove(customer);
                    await _context.SaveChangesAsync();
                    return View("~/Views/Account/SpecialRegister.cshtml", model);
                }
            }

            return View("~/Views/Account/SpecialRegister.cshtml", model);
        }

        // GET: /Account/VerifyOtp
        [HttpGet]
        public IActionResult VerifyOtp(string email)
        {
            if (string.IsNullOrEmpty(email) && TempData["EmailForOtp"] != null)
            {
                email = TempData["EmailForOtp"].ToString();
            }
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Register"); // Redirect if no email is provided
            }
            return View(new VerifyOtpViewModel { Email = email });
        }

        // POST: /Account/VerifyOtp - Handles OTP verification
        [HttpPost]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (ModelState.IsValid)
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == model.Email);

                if (customer == null)
                {
                    ModelState.AddModelError("", "User not found.");
                    return View(model);
                }

                if (customer.IsEmailVerified)
                {
                    ModelState.AddModelError("", "Your email is already verified. Please log in.");
                    return View("Login"); // Redirect to login if already verified
                }

                // Check OTP and expiry
                if (customer.Otp == model.Otp && customer.OtpGeneratedTime.HasValue &&
                    DateTime.UtcNow <= customer.OtpGeneratedTime.Value.AddMinutes(5)) // OTP valid for 5 minutes
                {
                    customer.IsEmailVerified = true;
                    customer.Otp = null; // Clear OTP after successful verification
                    customer.OtpGeneratedTime = null;
                    _context.Customers.Update(customer);
                    await _context.SaveChangesAsync();

                    // Automatically log in the user after successful verification
                    HttpContext.Session.SetString("UserName", customer.Name);
                    HttpContext.Session.SetString("Email", customer.Email);
                    HttpContext.Session.SetString("PrizePoints", customer.PrizePoints.ToString());
                    HttpContext.Session.SetString("AvatarUrl", Url.Action("GetAvatarImage", "Account", new { customerId = customer.CustomerId }));

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
                        Action = "Registered & Verified",
                        Details = "User registered and email verified successfully.",
                        Timestamp = DateTime.UtcNow,
                        UserRole = "Customer"
                    };
                    _context.AuditLogs.Add(auditLog);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Email verified successfully! You are now logged in.";
                    return RedirectToAction("Index", "Home");
                }
                else if (customer.OtpGeneratedTime.HasValue && DateTime.UtcNow > customer.OtpGeneratedTime.Value.AddMinutes(5))
                {
                    ModelState.AddModelError("Otp", "OTP has expired. Please request a new one.");
                }
                else
                {
                    ModelState.AddModelError("Otp", "Invalid OTP.");
                }
            }
            return View(model);
        }

        // POST: /Account/ResendOtp
        [HttpPost]
        public async Task<IActionResult> ResendOtp(string email)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);

            if (customer == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            if (customer.IsEmailVerified)
            {
                return Json(new { success = false, message = "Email is already verified." });
            }

            // Generate new OTP
            string newOtp = GenerateOtp();
            DateTime newOtpGeneratedTime = DateTime.UtcNow;

            customer.Otp = newOtp;
            customer.OtpGeneratedTime = newOtpGeneratedTime;
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            // Send new OTP email
            var subject = "Your New GameCraft Registration OTP";
            var message = $"Your new One-Time Password (OTP) for GameCraft registration is: <b>{newOtp}</b>. This OTP is valid for 5 minutes.";
            try
            {
                await _emailService.SendEmailAsync(customer.Email, subject, message);
                return Json(new { success = true, message = "A new OTP has been sent to your email." });
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Email sending error on resend: {ex.Message}");
                return Json(new { success = false, message = "Failed to resend OTP. Please try again later." });
            }
        }

        private string GenerateOtp()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString(); // 6-digit OTP
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
            if (login.Contains("@") && login.Contains("."))
            {
                customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == login);
            }
            else
            {
                customer = await _context.Customers.FirstOrDefaultAsync(c => c.Name == login);
            }

            if (customer == null)
            {
                ModelState.AddModelError("", "User does not exist.");
                return View();
            }

            // Check if email is verified
            if (!customer.IsEmailVerified)
            {
                ModelState.AddModelError("", "Your email is not verified. Please check your email for the OTP or register again.");
                TempData["EmailForOtp"] = customer.Email; // Pre-fill for OTP verification
                return RedirectToAction("VerifyOtp", new { email = customer.Email });
            }

            if (!PasswordHelper.VerifyPassword(password, customer.PasswordHash, customer.Salt))
            {
                ModelState.AddModelError("", "Invalid login or password.");
                return View();
            }

            HttpContext.Session.SetString("UserName", customer.Name);
            HttpContext.Session.SetString("Email", customer.Email);
            HttpContext.Session.SetString("PrizePoints", customer.PrizePoints.ToString());
            HttpContext.Session.SetString("AvatarUrl", Url.Action("GetAvatarImage", "Account", new { customerId = customer.CustomerId }));

            CookieOptions options = new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(7),
                IsEssential = true
            };
            Response.Cookies.Append("UserToken", customer.CustomerId.ToString(), options);

            var auditLog = new AuditLog
            {
                UserId = customer.CustomerId.ToString(),
                UserName = customer.Name,
                Action = "Logged In",
                Details = "User logged in successfully.",
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
            HttpContext.Session.Clear();
            Response.Cookies.Delete("UserToken");

            var userName = HttpContext.Session.GetString("UserName");
            var userId = HttpContext.Session.GetString("Email");
            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = "Logged Out",
                Details = "User logged out successfully.",
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
            var userName = HttpContext.Session.GetString("UserName");
            var email = HttpContext.Session.GetString("Email");
            var isAdmin = HttpContext.Session.GetString("IsAdmin") == "true"; // Check if user is admin

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var customer = await _context.Customers
                .Include(c => c.Order) // Include orders
                .ThenInclude(o => o.OrderDetails) // Include order details
                .ThenInclude(od => od.Product) // Include product details
                .Include(c => c.Order) // Include orders again for cards
                .ThenInclude(o => o.CardOrderDetails) // Include order details again
                .ThenInclude(cod => cod.Card) // Include card details
                .FirstOrDefaultAsync(c => c.Email == email);
            if (customer == null)
            {
                return RedirectToAction("Login");
            }

            var prizePointsString = HttpContext.Session.GetString("PrizePoints");
            if (prizePointsString != null && int.TryParse(prizePointsString, out int prizePoints))
            {
                customer.PrizePoints = prizePoints;
            }
            return View(customer);
        }

        // POST: /Account/ConnectAccount
        [HttpPost]
        public async Task<IActionResult> ConnectAccount(string cardNumber, string username)
        {
            var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Name == username);
            if (existingCustomer != null)
            {
                existingCustomer.GameCraftCardNumber = cardNumber;
                await _context.SaveChangesAsync();

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
                    redirectUrl = Url.Action("Index", "Home")
                });
            }

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
                AvatarImageData = defaultAvatarData
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

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
                redirectUrl = Url.Action("Index", "Home")
            });
        }

        // POST: /Account/UpdateAccount
        [HttpPost]
        public async Task<IActionResult> UpdateAccount(Customer customer, IFormFile avatarFile)
        {
            var existingCustomer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
            if (existingCustomer == null)
            {
                return NotFound();
            }

            customer.PasswordHash = existingCustomer.PasswordHash;
            customer.Salt = existingCustomer.Salt;
            customer.UserType = existingCustomer.UserType;
            customer.PrizePoints = existingCustomer.PrizePoints;
            customer.GameCraftCardNumber = existingCustomer.GameCraftCardNumber;
            customer.AdminKey = existingCustomer.AdminKey;
            customer.Otp = existingCustomer.Otp; // Preserve OTP related fields
            customer.OtpGeneratedTime = existingCustomer.OtpGeneratedTime;
            customer.IsEmailVerified = existingCustomer.IsEmailVerified;


            if (avatarFile != null && avatarFile.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await avatarFile.CopyToAsync(memoryStream);
                    customer.AvatarImageData = memoryStream.ToArray();
                }
            }
            else
            {
                customer.AvatarImageData = existingCustomer.AvatarImageData;
            }

            if (!ModelState.IsValid)
            {
                customer.AvatarImageData = existingCustomer.AvatarImageData;
                return View("MyAccount", customer);
            }

            var existingEmailUser = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == customer.Email && c.CustomerId != customer.CustomerId);
            if (existingEmailUser != null)
            {
                ModelState.AddModelError("Email", "This email is already in use by another account.");
                return View("MyAccount", customer);
            }

            var existingNameUser = await _context.Customers
                .FirstOrDefaultAsync(c => c.Name == customer.Name && c.CustomerId != customer.CustomerId);
            if (existingNameUser != null)
            {
                ModelState.AddModelError("Name", "This username is already taken by another account.");
                return View("MyAccount", customer);
            }

            _context.Entry(customer).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UserName", customer.Name);
            HttpContext.Session.SetString("Email", customer.Email);
            HttpContext.Session.SetString("AvatarUrl", Url.Action("GetAvatarImage", "Account", new { customerId = customer.CustomerId }));

            var auditLog = new AuditLog
            {
                UserId = customer.CustomerId.ToString(),
                UserName = customer.Name,
                Action = "Update Account",
                Details = $"User updated account details. New: [Email: {customer.Email}, Username: {customer.Name}].",
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
                return Json(new { success = false, message = "User not found." });
            }

            byte[]? newAvatarData = null;

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
                var fullPath = Path.Combine(_env.WebRootPath, selectedAvatarPath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    newAvatarData = await System.IO.File.ReadAllBytesAsync(fullPath);
                }
            }
            else
            {
                return Json(new { success = false, message = "No new avatar selected or uploaded." });
            }

            customer.AvatarImageData = newAvatarData;
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("AvatarUrl", Url.Action("GetAvatarImage", "Account", new { customerId = customer.CustomerId }));

            var auditLog = new AuditLog
            {
                UserId = customer.CustomerId.ToString(),
                UserName = customer.Name,
                Action = "Change Avatar",
                Details = "User changed their avatar.",
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
                return Json(new { success = false, message = "User not found." });
            }

            string passwordHash, salt;
            (passwordHash, salt) = PasswordHelper.HashPassword(newPassword);

            customer.PasswordHash = passwordHash;
            customer.Salt = salt;

            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            var auditLog = new AuditLog
            {
                UserId = customer.CustomerId.ToString(),
                UserName = customer.Name,
                Action = "Change Password",
                Details = "User changed their password.",
                Timestamp = DateTime.UtcNow,
                UserRole = "Customer"
            };
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Password changed successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(Order order)
        {
            var userId = HttpContext.Session.GetString("Email");
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == userId);

            if (customer == null)
            {
                return NotFound();
            }

            order.CustomerId = customer.CustomerId;
            order.OrderDate = DateTime.UtcNow;

            // Calculate total amount and populate order details
            order.TotalAmount = order.OrderDetails.Sum(od => od.Quantity * od.UnitPrice);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return RedirectToAction("MyAccount");
        }

    }
}
