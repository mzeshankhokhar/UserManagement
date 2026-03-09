using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using UserManagement.Core.Model;
using UserManagement.Core.Services;

namespace UserManagement.Service.Services
{
    /// <summary>
    /// Gmail API Email Service using OAuth 2.0 / Service Account
    /// More secure than SMTP for accounts with strict security policies
    /// </summary>
    public class GmailApiEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly GmailService _gmailService;

        public GmailApiEmailService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
            _gmailService = CreateGmailService();
        }

        private GmailService CreateGmailService()
        {
            GoogleCredential credential;

            // Option 1: Service Account (for Google Workspace)
            if (!string.IsNullOrEmpty(_settings.ServiceAccountKeyPath))
            {
                credential = GoogleCredential
                    .FromFile(_settings.ServiceAccountKeyPath)
                    .CreateScoped(GmailService.Scope.GmailSend)
                    .CreateWithUser(_settings.FromEmail); // Impersonate the sending user
            }
            // Option 2: OAuth2 with refresh token
            else if (!string.IsNullOrEmpty(_settings.OAuth2RefreshToken))
            {
                credential = GoogleCredential.FromAccessToken(_settings.OAuth2RefreshToken);
            }
            // Option 3: Application Default Credentials
            else
            {
                try
                {
                    credential = GoogleCredential
                        .GetApplicationDefault()
                        .CreateScoped(GmailService.Scope.GmailSend);
                }
                catch
                {
                    // Fall back to null - will fail gracefully
                    return null;
                }
            }

            return new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "UserManagement"
            });
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            if (_gmailService == null)
            {
                throw new InvalidOperationException(
                    "Gmail API is not configured. Please set ServiceAccountKeyPath or OAuth2RefreshToken in EmailSettings.");
            }

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            mimeMessage.To.Add(new MailboxAddress("", toEmail));
            mimeMessage.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            // Convert to base64url encoded string
            using var stream = new MemoryStream();
            await mimeMessage.WriteToAsync(stream);
            var rawMessage = Convert.ToBase64String(stream.ToArray())
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");

            var message = new Message { Raw = rawMessage };

            await _gmailService.Users.Messages.Send(message, "me").ExecuteAsync();
        }
    }
}
