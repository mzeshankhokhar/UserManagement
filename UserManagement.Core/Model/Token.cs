using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagement.Core.Model
{
    public class Token : BaseEntity
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        /// <summary>
        /// The user who owns this token (or the impersonated user if IsImpersonate is true)
        /// </summary>
        [ForeignKey("User")]
        public int UserId { get; set; }

        /// <summary>
        /// Indicates if this is an impersonation session
        /// </summary>
        public bool IsImpersonate { get; set; }

        /// <summary>
        /// The original admin user ID who initiated impersonation (null if not impersonating)
        /// </summary>
        public int? OriginalUserId { get; set; }

        /// <summary>
        /// When the access token expires
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// When the refresh token expires
        /// </summary>
        public DateTime? RefreshTokenExpiresAt { get; set; }

        /// <summary>
        /// Indicates if user selected "Remember Me" during login
        /// </summary>
        public bool RememberMe { get; set; }

        /// <summary>
        /// Indicates if the refresh token has been revoked
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// Device/browser info for security tracking
        /// </summary>
        public string DeviceInfo { get; set; }
    }
}
