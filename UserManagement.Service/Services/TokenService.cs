using AutoMapper;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UserManagement.Core.Model;
using UserManagement.Core.Repositories;
using UserManagement.Core.Services;
using UserManagement.Core.UnitOfWorks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Claim = System.Security.Claims.Claim;

namespace UserManagement.Service.Services
{
    public class TokenService : Service<Token>, ITokenService
    {
        private readonly ITokenRepository _tokenRepository;
        private readonly IMapper _mapper;
        private readonly SymmetricSecurityKey _signingKey;
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtSettings _jwtSettings;

        // Custom claim types
        private const string ImpersonationClaimType = "impersonation";
        private const string OriginalUserIdClaimType = "original_user_id";
        private const string OriginalUserEmailClaimType = "original_user_email";
        private const string RememberMeClaimType = "remember_me";

        public TokenService(
            IGenericRepository<Token> repository,
            IUnitOfWork unitOfWork,
            ITokenRepository tokenRepository,
            IOptions<JwtSettings> jwtOptions,
            IMapper mapper) : base(repository, unitOfWork)
        {
            _tokenRepository = tokenRepository;
            _mapper = mapper;
            _jwtSettings = jwtOptions.Value;
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            _unitOfWork = unitOfWork;
        }

        #region Token Generation

        public async Task<string> GenerateAccessTokenAsync(string userName, bool rememberMe = false)
        {
            var user = await _tokenRepository.GetUserByUserNameAsync(userName);

            if (user == null)
            {
                throw new InvalidOperationException($"User '{userName}' not found.");
            }

            var claims = BuildUserClaims(user, isImpersonation: false, originalAdmin: null, rememberMe: rememberMe);
            var expiryMinutes = rememberMe ? _jwtSettings.RememberMeExpiryMinutes : _jwtSettings.AccessTokenExpiryMinutes;

            return GenerateJwtToken(claims, expiryMinutes);
        }

        public Task<string> GenerateImpersonationTokenAsync(User targetUser, User originalAdmin)
        {
            if (targetUser == null)
                throw new ArgumentNullException(nameof(targetUser));
            if (originalAdmin == null)
                throw new ArgumentNullException(nameof(originalAdmin));

            var claims = BuildUserClaims(targetUser, isImpersonation: true, originalAdmin: originalAdmin, rememberMe: false);
            // Impersonation tokens have shorter expiry (1 hour)
            return Task.FromResult(GenerateJwtToken(claims, 60));
        }

        public Task<string> GenerateRefreshTokenAsync()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Task.FromResult(Convert.ToBase64String(randomNumber));
        }

        private List<Claim> BuildUserClaims(User user, bool isImpersonation, User originalAdmin, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sid, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(RememberMeClaimType, rememberMe.ToString().ToLower())
            };

            // Add impersonation claims
            if (isImpersonation && originalAdmin != null)
            {
                claims.Add(new Claim(ImpersonationClaimType, "true"));
                claims.Add(new Claim(OriginalUserIdClaimType, originalAdmin.Id.ToString()));
                claims.Add(new Claim(OriginalUserEmailClaimType, originalAdmin.Email ?? string.Empty));
            }

            // Add role claims
            if (user.UserRoles != null)
            {
                foreach (var userRole in user.UserRoles)
                {
                    if (userRole.Role != null)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));

                        if (userRole.Role.RoleClaims != null)
                        {
                            foreach (var roleClaim in userRole.Role.RoleClaims)
                            {
                                if (roleClaim.Claim != null)
                                {
                                    claims.Add(new Claim(roleClaim.Claim.Type, roleClaim.Claim.Value));
                                }
                            }
                        }
                    }
                }
            }

            return claims;
        }

        private string GenerateJwtToken(List<Claim> claims, int expiryMinutes)
        {
            var creds = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        #endregion

        #region Token Validation

        public Task<bool> IsTokenExpired(string token)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            if (!jwtTokenHandler.CanReadToken(token))
                return Task.FromResult(true);

            var jwtToken = jwtTokenHandler.ReadJwtToken(token);
            return Task.FromResult(jwtToken.ValidTo < DateTime.UtcNow);
        }

        public async Task<ClaimsPrincipal> ValidateAccessTokenAsync(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _signingKey,
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                if (validatedToken is JwtSecurityToken jwtToken && 
                    jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return await Task.FromResult(principal);
                }
            }
            catch
            {
                // Invalid token
            }

            return null;
        }

        #endregion

        #region Refresh Token

        public async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> RefreshAccessTokenAsync(string refreshToken)
        {
            // Find the token in database
            var storedToken = await _tokenRepository.GetByRefreshTokenAsync(refreshToken);

            if (storedToken == null)
            {
                return (null, null, DateTime.MinValue);
            }

            // Check if refresh token is expired
            if (storedToken.RefreshTokenExpiresAt.HasValue && storedToken.RefreshTokenExpiresAt < DateTime.UtcNow)
            {
                return (null, null, DateTime.MinValue);
            }

            // Check if token is revoked
            if (storedToken.IsRevoked)
            {
                return (null, null, DateTime.MinValue);
            }

            // Get user
            var user = await _tokenRepository.GetUserByUserNameAsync(
                (await _tokenRepository.GetUserByIdAsync(storedToken.UserId))?.UserName);

            if (user == null)
            {
                return (null, null, DateTime.MinValue);
            }

            // Determine if this was a "remember me" session
            var rememberMe = storedToken.RememberMe;

            // Generate new tokens
            var newAccessToken = await GenerateAccessTokenAsync(user.UserName, rememberMe);
            var newRefreshToken = await GenerateRefreshTokenAsync();
            var (accessExpiry, refreshExpiry) = GetTokenExpiry(rememberMe);

            // Update stored token (rotate refresh token)
            storedToken.AccessToken = newAccessToken;
            storedToken.RefreshToken = newRefreshToken;
            storedToken.ExpiresAt = accessExpiry;
            storedToken.RefreshTokenExpiresAt = refreshExpiry;
            storedToken.UpdatedDate = DateTime.UtcNow;

            _tokenRepository.Update(storedToken);
            await _unitOfWork.CommitAsync();

            return (newAccessToken, newRefreshToken, accessExpiry);
        }

        #endregion

        #region Token Storage & Revocation

        public async Task StoreTokenAsync(Token token)
        {
            var existingToken = await _tokenRepository.GetTokenAsync(token.UserId);

            if (existingToken != null)
            {
                // Update existing token
                existingToken.AccessToken = token.AccessToken;
                existingToken.RefreshToken = token.RefreshToken;
                existingToken.ExpiresAt = token.ExpiresAt;
                existingToken.RefreshTokenExpiresAt = token.RefreshTokenExpiresAt;
                existingToken.RememberMe = token.RememberMe;
                existingToken.IsImpersonate = token.IsImpersonate;
                existingToken.OriginalUserId = token.OriginalUserId;
                existingToken.IsRevoked = false;
                existingToken.DeviceInfo = token.DeviceInfo;
                existingToken.UpdatedDate = DateTime.UtcNow;

                _tokenRepository.Update(existingToken);
            }
            else
            {
                token.CreatedDate = DateTime.UtcNow;
                await _tokenRepository.AddAsync(token);
            }

            await _unitOfWork.CommitAsync();
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            var token = await _tokenRepository.GetByRefreshTokenAsync(refreshToken);

            if (token == null)
                return false;

            token.IsRevoked = true;
            token.UpdatedDate = DateTime.UtcNow;

            _tokenRepository.Update(token);
            await _unitOfWork.CommitAsync();

            return true;
        }

        public async Task RevokeAllUserTokensAsync(int userId)
        {
            var tokens = await _tokenRepository.GetAllUserTokensAsync(userId);

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.UpdatedDate = DateTime.UtcNow;
                _tokenRepository.Update(token);
            }

            await _unitOfWork.CommitAsync();
        }

        #endregion

        #region Helper Methods

        public (bool IsImpersonating, int? OriginalUserId) GetImpersonationInfo(ClaimsPrincipal principal)
        {
            if (principal == null)
                return (false, null);

            var impersonationClaim = principal.FindFirst(ImpersonationClaimType);
            var originalUserIdClaim = principal.FindFirst(OriginalUserIdClaimType);

            var isImpersonating = impersonationClaim?.Value == "true";
            int? originalUserId = null;

            if (isImpersonating && originalUserIdClaim != null && int.TryParse(originalUserIdClaim.Value, out var id))
            {
                originalUserId = id;
            }

            return (isImpersonating, originalUserId);
        }

        public (DateTime AccessTokenExpiry, DateTime RefreshTokenExpiry) GetTokenExpiry(bool rememberMe)
        {
            if (rememberMe)
            {
                return (
                    DateTime.UtcNow.AddMinutes(_jwtSettings.RememberMeExpiryMinutes),
                    DateTime.UtcNow.AddDays(_jwtSettings.RememberMeRefreshTokenExpiryDays)
                );
            }

            return (
                DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
                DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
            );
        }

        #endregion
    }
}
