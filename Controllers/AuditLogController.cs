using GameCraft.Data;
using GameCraft.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace GameCraft.Controllers
{
    public class AuditLogController : Controller
    {
        private readonly GameCraftDbContext _context;

        public AuditLogController(GameCraftDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Fetch all audit logs
            var auditLogs = _context.AuditLogs.ToList();
            return View(auditLogs);
        }
    }
}
