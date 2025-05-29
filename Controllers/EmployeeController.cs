using Microsoft.AspNetCore.Mvc;

namespace GameCraft.Controllers
{
    public class EmployeeController : Controller
    {
        
        public IActionResult Login()
        {
            return View();
        }

       
        public IActionResult Dashboard()
        {
           
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

        // POST: /Employee/Login (Placeholder for login submission)
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            
            if (username == "employee" && password == "password") 
            {
                
                return RedirectToAction("Dashboard", "Employee");
            }
            ModelState.AddModelError("", "Invalid username or password.");
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
