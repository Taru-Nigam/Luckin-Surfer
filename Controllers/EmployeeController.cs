using Microsoft.AspNetCore.Mvc;

namespace GameCraft.Controllers
{
    public class EmployeeController : Controller
    {
        private const string ValidAdminKey = "YourSecureAdminKey123"; // Set your actual admin key here

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // Check for employee login
            if (username == "employee" && password == "password")
            {
                return RedirectToAction("Dashboard", "Employee");
            }
            ModelState.AddModelError("", "Invalid username or password.");
            return View();
        }

        [HttpPost]
        public IActionResult AdminLogin(string adminKey)
        {
            if (string.IsNullOrWhiteSpace(adminKey))
            {
                ModelState.AddModelError("", "Admin key is required.");
                return View("Login"); // Return to the login view
            }
            // Log the admin key for debugging
            Console.WriteLine($"Admin Key Entered: {adminKey}");
            if (adminKey == ValidAdminKey)
            {
                // Set session variables for admin
                HttpContext.Session.SetString("IsAdmin", "true");
                HttpContext.Session.SetString("User Name", "Admin");
                // Redirect to the admin index page
                return RedirectToAction("Index", "Admin");
            }
            else
            {
                ModelState.AddModelError("", "Invalid admin key.");
                return View("Login"); // Return to the login view
            }
        }

        public IActionResult Dashboard()
        {
            // Check if the user is an admin
            var isAdmin = HttpContext.Session.GetString("IsAdmin");
            if (isAdmin != null)
            {
                ViewBag.IsAdmin = true; // Indicate that the user is an admin
            }
            return View();
        }

        
        public IActionResult PrizeStock()
        {
           
            return View();
        }

       
        public IActionResult AddPrize()
        {
            return View();
        }

        
        public IActionResult RedeemedPrizes()
        {
           
            return View();
        }

        
        public IActionResult AuditLog()
        {
            
            return View();
        }
        
        [HttpPost]
        public IActionResult AddPrize(string prizeName, int quantity, string imageUrl, decimal ticketCost)
        {
            
            ViewBag.Message = "Prize '" + prizeName + "' added successfully!";
            
            return View();
        }
    }
}
