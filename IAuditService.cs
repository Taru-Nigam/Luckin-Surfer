// Example IAuditService.cs
using System.Threading.Tasks;

namespace GameCraft.Services
{
    public interface IAuditService
    {
        Task LogActivity(string userId, string userName, string action, string details, string userRole);
    }
}
