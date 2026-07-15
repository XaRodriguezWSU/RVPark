using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace RVSite.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            var smtp = new SmtpClient
            {
                Host = _config["Email:SmtpHost"],
                Port = int.Parse(_config["Email:SmtpPort"]),
                EnableSsl = true,
                Credentials = new NetworkCredential(
                    _config["Email:Username"],
                    _config["Email:Password"])
            };

            var message = new MailMessage(
                _config["Email:From"],
                to,
                subject,
                body)
            {
                IsBodyHtml = true
            };

            await smtp.SendMailAsync(message);
        }
    }
}
