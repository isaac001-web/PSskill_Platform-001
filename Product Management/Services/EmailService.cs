using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Product_Management.Services
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string toName,
            string subject, string htmlBody);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(
            string toEmail,
            string toName,
            string subject,
            string htmlBody)
        {
            var email = new MimeMessage();

            // FROM
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"] ?? "PSskill",
                _config["EmailSettings:SenderEmail"] ?? ""
            ));

            // TO
            email.To.Add(new MailboxAddress(toName, toEmail));

            email.Subject = subject;

            // BODY — fixed BodyBuilder usage
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };

            email.Body = bodyBuilder.ToMessageBody();  // ← FIXED

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                _config["EmailSettings:SmtpHost"] ?? "smtp.gmail.com",
                int.Parse(_config["EmailSettings:SmtpPort"] ?? "587"),
                SecureSocketOptions.StartTls
            );

            await smtp.AuthenticateAsync(
                _config["EmailSettings:SmtpUser"] ?? "",
                _config["EmailSettings:SmtpPass"] ?? ""
            );

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}