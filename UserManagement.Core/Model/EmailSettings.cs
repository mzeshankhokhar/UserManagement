namespace UserManagement.Core.Model
{
    public class EmailSettings
    {
        /// <summary>
        /// Email provider type: "SMTP", "GmailApi"
        /// </summary>
        public string Provider { get; set; } = "SMTP";

        // SMTP Settings
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string UserName { get; set; }
        public string Password { get; set; }

        // Gmail API Settings (for secure Google accounts)
        /// <summary>
        /// Path to service account JSON key file (for Google Workspace)
        /// </summary>
        public string ServiceAccountKeyPath { get; set; }

        /// <summary>
        /// OAuth2 refresh token (for personal accounts)
        /// </summary>
        public string OAuth2RefreshToken { get; set; }

        /// <summary>
        /// OAuth2 Client ID (for generating refresh token)
        /// </summary>
        public string OAuth2ClientId { get; set; }

        /// <summary>
        /// OAuth2 Client Secret (for generating refresh token)
        /// </summary>
        public string OAuth2ClientSecret { get; set; }

        // Common Settings
        public string FromEmail { get; set; }
        public string FromName { get; set; } = "UserManagement";
    }
}
