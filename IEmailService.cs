using System.Threading.Tasks;

namespace GameCraft.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }
}
