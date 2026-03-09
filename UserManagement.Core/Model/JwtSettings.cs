namespace UserManagement.Core.Model
{
    public class JwtSettings
    {
        public string Secret { get; set; }
        public string GOOGLE_CLIENT_SECRET { get; set; }
        public string GOOGLE_CLIENT_ID { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }

        /// <summary>
        /// Normal access token expiry (default: 15 minutes)
        /// </summary>
        public int AccessTokenExpiryMinutes { get; set; } = 15;

        /// <summary>
        /// Refresh token expiry (default: 7 days)
        /// </summary>
        public int RefreshTokenExpiryDays { get; set; } = 7;

        /// <summary>
        /// Access token expiry when "Remember Me" is checked (default: 7 days = 10080 minutes)
        /// </summary>
        public int RememberMeExpiryMinutes { get; set; } = 10080;

        /// <summary>
        /// Refresh token expiry when "Remember Me" is checked (default: 30 days)
        /// </summary>
        public int RememberMeRefreshTokenExpiryDays { get; set; } = 30;
    }
}
