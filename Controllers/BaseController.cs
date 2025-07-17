// FileName: /Controllers/BaseController.cs
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using GameCraft.Data;
using System.Linq;
using Microsoft.AspNetCore.Http; // For HttpContext.Session
using System.Security.Claims; // For ClaimTypes

public class BaseController : Controller
{
    protected readonly GameCraftDbContext _context;

    public BaseController(GameCraftDbContext context)
    {
        _context = context;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Check if UserName is already in session
        if (HttpContext.Session.GetString("UserName") == null)
        {
            // Try to retrieve user from cookie
            var userToken = HttpContext.Request.Cookies["UserToken"];
            if (!string.IsNullOrEmpty(userToken))
            {
                if (int.TryParse(userToken, out var customerId))
                {
                    var customer = _context.Customers.FirstOrDefault(c => c.CustomerId == customerId);
                    if (customer != null)
                    {
                        // Set session variables
                        HttpContext.Session.SetString("UserName", customer.Name);
                        HttpContext.Session.SetString("Email", customer.Email);
                        HttpContext.Session.SetString("PrizePoints", customer.PrizePoints.ToString());
                        // Store a URL to the avatar image, not the raw data
                        // This assumes GetAvatarImage action is in AccountController
                        HttpContext.Session.SetString("AvatarUrl", Url.Action("GetAvatarImage", "Account", new { customerId = customer.CustomerId }));
                    }
                }
            }
        }

        base.OnActionExecuting(context);
    }

    // Helper to get current customer ID from claims (if using cookie authentication)
    protected int? GetCurrentCustomerId()
    {
        if (User.Identity.IsAuthenticated)
        {
            var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(customerIdClaim, out int customerId))
            {
                return customerId;
            }
        }
        return null;
    }
}
