using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.Threading.Tasks;

namespace GameCraft.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var emailSettings = _configuration.GetSection("SmtpSettings");
            var host = emailSettings["Host"];
            var port = int.Parse(emailSettings["Port"]);
            var username = emailSettings["Username"];
            var password = emailSettings["Password"];
            var enableSsl = bool.Parse(emailSettings["EnableSsl"]);
            var senderName = emailSettings["SenderName"];
            var senderEmail = emailSettings["SenderEmail"];

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart("html") { Text = message };

            using (var smtp = new SmtpClient())
            {
                try
                {
                    await smtp.ConnectAsync(host, port, enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
                    await smtp.AuthenticateAsync(username, password);
                    await smtp.SendAsync(email);
                    await smtp.DisconnectAsync(true);
                }
                catch (System.Exception ex)
                {
                    // Log the exception (e.g., using ILogger)
                    System.Console.WriteLine($"Error sending email: {ex.Message}");
                    throw; // Re-throw to indicate failure
                }
            }
        }
    }
}
