using UserManagement.Core.Model;
using System.Security.Claims;

namespace UserManagement.Core.Services
{
    public interface ITokenService : IService<Token>
    {
        /// <summary>
        /// Generate access token for a user
        /// </summary>
        Task<string> GenerateAccessTokenAsync(string userName, bool rememberMe = false);

        /// <summary>
        /// Generate access token for impersonation (includes original admin info)
        /// </summary>
        Task<string> GenerateImpersonationTokenAsync(User targetUser, User originalAdmin);

        /// <summary>
        /// Generate a secure refresh token
        /// </summary>
        Task<string> GenerateRefreshTokenAsync();

        /// <summary>
        /// Check if a token has expired
        /// </summary>
        Task<bool> IsTokenExpired(string token);

        /// <summary>
        /// Validate an access token and return the claims principal
        /// </summary>
        Task<ClaimsPrincipal> ValidateAccessTokenAsync(string token);

        /// <summary>
        /// Refresh an expired access token using a refresh token
        /// </summary>
        Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> RefreshAccessTokenAsync(string refreshToken);

        /// <summary>
        /// Store token in database
        /// </summary>
        Task StoreTokenAsync(Token tokenModel);

        /// <summary>
        /// Revoke a refresh token (logout)
        /// </summary>
        Task<bool> RevokeTokenAsync(string refreshToken);

        /// <summary>
        /// Revoke all tokens for a user (logout from all devices)
        /// </summary>
        Task RevokeAllUserTokensAsync(int userId);

        /// <summary>
        /// Get impersonation info from current token claims
        /// </summary>
        (bool IsImpersonating, int? OriginalUserId) GetImpersonationInfo(ClaimsPrincipal principal);

        /// <summary>
        /// Get token expiry times based on remember me setting
        /// </summary>
        (DateTime AccessTokenExpiry, DateTime RefreshTokenExpiry) GetTokenExpiry(bool rememberMe);
    }
}
