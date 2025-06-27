using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using GameCraft.Data;

public class BaseController : Controller
{
    protected readonly GameCraftDbContext _context;

    public BaseController(GameCraftDbContext context)
    {
        _context = context;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        
        if (HttpContext.Session.GetString("UserName") == null)
        {
            var userToken = HttpContext.Request.Cookies["UserToken"];
            if (!string.IsNullOrEmpty(userToken))
            {
                if (int.TryParse(userToken, out var customerId))
                {
                    var customer = _context.Customers.FirstOrDefault(c => c.CustomerId == customerId);
                    if (customer != null)
                    {
                        
                        HttpContext.Session.SetString("UserName", customer.Name);
                        HttpContext.Session.SetString("Email", customer.Email);
                        HttpContext.Session.SetString("PrizePoints", customer.PrizePoints.ToString());
                        HttpContext.Session.SetString("AvatarUrl", customer.AvatarUrl ?? "/images/default-avatar.png");
                    }
                }
            }
        }

        base.OnActionExecuting(context);
    }
}