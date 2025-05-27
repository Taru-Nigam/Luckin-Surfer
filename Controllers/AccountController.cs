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
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public IActionResult Register(string email, string name, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Email and password are required.");
                return View();
            }

            if (_context.Customers.Any(c => c.Email == email))
            {
                ModelState.AddModelError("", "This email is already registered.");
                return View();
            }

            var (hashed, salt) = PasswordHelper.HashPassword(password);

            var customer = new Customer
            {
                Email = email,
                Name = name,
                HashedPassword = hashed,
                Salt = salt
              
            };

            _context.Customers.Add(customer);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var customer = _context.Customers.FirstOrDefault(c => c.Email == email);
            if (customer == null || !PasswordHelper.VerifyPassword(password, customer.HashedPassword, customer.Salt))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View();
            }

            
            return RedirectToAction("Index", "Home");
        }
    }
}