using UserManagement.Core.Model;

namespace UserManagement.Core.Services
{
    public interface IVerificationCodeService
    {
        /// <summary>
        /// Generate and store a new verification code
        /// </summary>
        Task<string> GenerateCodeAsync(int userId, string sentTo, string type, int expiryMinutes = 15);

        /// <summary>
        /// Verify a code and mark it as used
        /// </summary>
        Task<(bool IsValid, string Message)> VerifyCodeAsync(string sentTo, string code, string type);

        /// <summary>
        /// Validate a code without marking it as used (for preview/confirmation)
        /// </summary>
        Task<VerificationCode> GetValidCodeAsync(string sentTo, string code, string type);

        /// <summary>
        /// Invalidate all existing codes for a user and type
        /// </summary>
        Task InvalidateExistingCodesAsync(int userId, string type);

        /// <summary>
        /// Check if user can request a new code (rate limiting)
        /// </summary>
        Task<(bool CanRequest, int WaitSeconds)> CanRequestNewCodeAsync(int userId, string type);
    }
}
