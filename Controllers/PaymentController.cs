using Microsoft.AspNetCore.Mvc;
using GameCraft.Data;
using GameCraft.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http; // For HttpContext.Session

namespace GameCraft.Controllers
{
    public class PaymentController : Controller
    {
        private readonly GameCraftDbContext _context;

        public PaymentController(GameCraftDbContext context)
        {
            _context = context;
        }

        // GET: /Payment/CardPayment
        // This action displays the payment method selection page.
        // It receives the promotionId from the GetCard page.
        public async Task<IActionResult> CardPayment(int? promotionId) // Renamed from Index to CardPayment
        {
            if (!promotionId.HasValue || promotionId.Value <= 0)
            {
                TempData["ErrorMessage"] = "No item selected for purchase.";
                return RedirectToAction("Index", "Home");
            }

            // Fetch the promotion details to display on the payment page
            var promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.PromotionId == promotionId.Value);

            // IMPORTANT: Check if the fetched promotion is indeed the GameCraft Card
            if (promotion == null || promotion.Title != "Purchase Your GameCraft Card!")
            {
                TempData["ErrorMessage"] = "GameCraft Card promotion not found or invalid.";
                return RedirectToAction("Index", "Home");
            }

            // Store the promotionId in TempData so it can be picked up by Checkout/Index
            TempData["PurchasePromotionId"] = promotion.PromotionId;
            // Convert decimal to string before storing in TempData
            TempData["PurchasePromotionPrice"] = promotion.Price.ToString();

            // Pass the promotion object to the view
            return View(promotion); // This will look for Views/Payment/CardPayment.cshtml
        }
    }
}
