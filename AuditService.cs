// Example AuditService.cs
using GameCraft.Data;
using GameCraft.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace GameCraft.Services
{
    public class AuditService : IAuditService
    {
        private readonly GameCraftDbContext _context;
        private readonly UserManager<IdentityUser> _userManager; // Assuming IdentityUser for user management

        public AuditService(GameCraftDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task LogActivity(string userId, string userName, string action, string details, string userRole)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow,
                UserRole = userRole
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}
