using System.Diagnostics;
using GameCraft.Models;
using Microsoft.AspNetCore.Mvc;

namespace GameCraft.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View(); // This corresponds to Index.cshtml
        }

        public IActionResult ConnectAccount() 
        {
            return View();
        }

        public IActionResult About()
        {
            return View(); // This corresponds to About.cshtml
        }

        public IActionResult Prizes()
        {
            return View(); // You'll create Prizes.cshtml
        }

        public IActionResult MyAccount()
        {
            return View(); // You'll create MyAccount.cshtml
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
