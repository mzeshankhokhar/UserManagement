using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagement.Core.Model
{
    /// <summary>
    /// Stores verification codes for email/phone verification
    /// </summary>
    public class VerificationCode : BaseEntity
    {
        /// <summary>
        /// User ID this code belongs to
        /// </summary>
        [ForeignKey("User")]
        public int UserId { get; set; }

        /// <summary>
        /// The 6-digit verification code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Type of verification: "Email", "Phone", "PasswordReset"
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// When the code expires
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Whether the code has been used
        /// </summary>
        public bool IsUsed { get; set; }

        /// <summary>
        /// Number of verification attempts
        /// </summary>
        public int Attempts { get; set; }

        /// <summary>
        /// Maximum allowed attempts (default 5)
        /// </summary>
        public int MaxAttempts { get; set; } = 5;

        /// <summary>
        /// Email or phone number the code was sent to
        /// </summary>
        public string SentTo { get; set; }
    }
}
