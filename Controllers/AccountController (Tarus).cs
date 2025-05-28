using Microsoft.AspNetCore.Mvc;
using GameCraft.Models; // Adjust the namespace according to your project
using System.Threading.Tasks;
using GameCraft.Data;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        // Implement your login logic here (e.g., check against the database)
        // If successful, redirect to the desired page
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(string username, string email, string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            ModelState.AddModelError("", "Passwords do not match.");
            return View();
        }

        // Implement your registration logic here (e.g., save to the database)
        // If successful, redirect to the login page
        return RedirectToAction("Login");
    }
    
    [HttpGet]
    public IActionResult MyAccount()
    {
        // Retrieve the customer data from the database or session
        var customer = new Customer(); // Replace with actual retrieval logic
        return View(customer);
    }
    [HttpPost]
    public IActionResult UpdateAccount(Customer customer, bool isEmployee)
    {
        // Set RoleId based on employee status
        customer.RoleId = isEmployee ? 2 : 1; // 2 for Employee, 1 for Customer
        if (ModelState.IsValid)
        {
            // Save the customer data to the database
            // Your save logic here
            return RedirectToAction("Index", "Home"); // Redirect after successful update
        }
        return View("MyAccount", customer); // Return to the view with validation errors
    }
}
