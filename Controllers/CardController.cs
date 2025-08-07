using GameCraft.Data;
using GameCraft.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace GameCraft.Controllers
{
    public class CardController : Controller
    {
        private readonly GameCraftDbContext _context;

        public CardController(GameCraftDbContext context)
        {
            _context = context;
        }

        // GET: /Card/Catalog
        // This action now returns a list of cards to the view
        public async Task<IActionResult> Catalog()
        {
            var cards = await _context.Cards.ToListAsync(); // Fetch all cards
            return View(cards); // Pass the list of cards to the view
        }

        // GET: /Card/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var card = await _context.Cards.FirstOrDefaultAsync(c => c.CardId == id);
            if (card == null)
            {
                return NotFound();
            }

            return View(card); // Pass the single card to the view
        }
    }
}
