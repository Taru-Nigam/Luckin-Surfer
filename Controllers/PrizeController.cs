using Microsoft.AspNetCore.Mvc;
using GameCraft.Data;
using GameCraft.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace GameCraft.Controllers
{
    public class PrizesController : Controller
    {
        private readonly GameCraftDbContext _dbContext;

        public PrizesController(GameCraftDbContext dbContext) 
        {
            _dbContext = dbContext;
        }

        // This will handle requests to /Prizes or /Prizes/Index
        public async Task<IActionResult> Prizes() 
        {
            var prizes = await _dbContext.Prizes.ToListAsync();
            return View("~/Views/Home/Prizes.cshtml", prizes);
        }

        
    }
}