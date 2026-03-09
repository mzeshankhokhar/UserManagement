using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using UserManagement.Core.Model;
using UserManagement.Core.Services;

namespace UserManagement.Service.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<EmailSettings> options, ILogger<SmtpEmailService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            _logger.LogInformation("Attempting to send email to {ToEmail} with subject: {Subject}", toEmail, subject);
            _logger.LogDebug("SMTP Settings - Host: {Host}, Port: {Port}, From: {From}, UserName: {UserName}", 
                _settings.Host, _settings.Port, _settings.FromEmail, _settings.UserName);

            try
            {
                var from = new MailAddress(_settings.FromEmail, _settings.FromName);
                var to = new MailAddress(toEmail);

                using var message = new MailMessage(from, to)
                {
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                using var client = new SmtpClient(_settings.Host, _settings.Port)
                {
                    EnableSsl = _settings.UseSsl,
                    Credentials = new NetworkCredential(_settings.UserName, _settings.Password),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 30000 // 30 seconds
                };

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP Error sending email to {ToEmail}. StatusCode: {StatusCode}", 
                    toEmail, smtpEx.StatusCode);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {ToEmail}", toEmail);
                throw;
            }
        }
    }
}
