
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace TryMeBitch.Models
{
    public class SmtpEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var msg = new MailMessage(
               "Email Here", email, subject, htmlMessage)
            { IsBodyHtml = true };  // Use true if your body contains HTML

            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential("Email here", "APP Password here"),
                EnableSsl = true
            };
            return client.SendMailAsync(msg);
        }
    }
}
