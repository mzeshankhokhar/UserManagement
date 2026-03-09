using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Core.DTOs;
using UserManagement.Core.Model;
using UserManagement.Core.Services;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace UserManagement.API.Controllers
{
    /// <summary>
    /// Admin-only operations including user impersonation
    /// </summary>
    public class AdminController : CustomBaseController
    {
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;

        public AdminController(
            UserManager<User> userManager,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Start impersonating another user (Admin only)
        /// </summary>
        [HttpPost("impersonate")]
        [Authorize(Roles = "ADMIN,SITEADMIN")]
        public async Task<IActionResult> ImpersonateUserAsync([FromBody] ImpersonateRequestDto request)
        {
            // Get current admin user
            var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(401, "Admin user not found"));
            }

            // Check if already impersonating
            var (isImpersonating, _) = _tokenService.GetImpersonationInfo(User);
            if (isImpersonating)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, 
                    "Already impersonating a user. Stop current impersonation first."));
            }

            // Find target user - try ID first (if numeric), then email, then username
            User targetUser = null;

            if (int.TryParse(request.TargetUserIdOrEmail, out var userId))
            {
                targetUser = await _userManager.FindByIdAsync(userId.ToString());
            }

            targetUser ??= await _userManager.FindByEmailAsync(request.TargetUserIdOrEmail);
            targetUser ??= await _userManager.FindByNameAsync(request.TargetUserIdOrEmail);

            if (targetUser == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(404, "Target user not found"));
            }

            // Prevent self-impersonation
            if (targetUser.Id == adminUser.Id)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Cannot impersonate yourself"));
            }

            // Generate impersonation tokens
            var accessToken = await _tokenService.GenerateImpersonationTokenAsync(targetUser, adminUser);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync();

            // Store impersonation token
            var tokenModel = new Token
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                IsImpersonate = true,
                OriginalUserId = adminUser.Id,
                UserId = targetUser.Id,
                CreatedDate = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(1) // Impersonation expires in 1 hour
            };
            await _tokenService.StoreTokenAsync(tokenModel);

            var response = new ImpersonateResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                IsImpersonating = true,
                OriginalUserId = adminUser.Id,
                OriginalUserEmail = adminUser.Email,
                ImpersonatedUserId = targetUser.Id,
                ImpersonatedUserEmail = targetUser.Email
            };

            return CreateActionResult(CustomResponseDto<ImpersonateResponseDto>.Success(200, response));
        }

        /// <summary>
        /// Stop impersonating and return to admin session.
        /// Accessible by any authenticated user who is currently impersonating.
        /// </summary>
        [HttpPost("stop-impersonation")]
        [Authorize]
        public async Task<IActionResult> StopImpersonationAsync()
        {
            var (isImpersonating, originalUserId) = _tokenService.GetImpersonationInfo(User);

            if (!isImpersonating || !originalUserId.HasValue)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Not currently impersonating"));
            }

            // Get original admin user
            var adminUser = await _userManager.FindByIdAsync(originalUserId.Value.ToString());
            if (adminUser == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(404, "Original admin user not found"));
            }

            // Generate new tokens for the admin
            var accessToken = await _tokenService.GenerateAccessTokenAsync(adminUser.UserName);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync();
            var (accessExpiry, refreshExpiry) = _tokenService.GetTokenExpiry(false);

            var tokenModel = new Token
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                IsImpersonate = false,
                OriginalUserId = null,
                UserId = adminUser.Id,
                ExpiresAt = accessExpiry,
                RefreshTokenExpiresAt = refreshExpiry,
                CreatedDate = DateTime.UtcNow
            };
            await _tokenService.StoreTokenAsync(tokenModel);

            return CreateActionResult(CustomResponseDto<object>.Success(200, new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = accessExpiry,
                Message = "Impersonation ended. You are now logged in as yourself."
            }));
        }

        /// <summary>
        /// Get current impersonation status.
        /// Accessible by any authenticated user.
        /// </summary>
        [HttpGet("impersonation-status")]
        [Authorize]
        public IActionResult GetImpersonationStatus()
        {
            var (isImpersonating, originalUserId) = _tokenService.GetImpersonationInfo(User);
            var currentUserId = User.FindFirst(JwtRegisteredClaimNames.Sid)?.Value 
                                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return CreateActionResult(CustomResponseDto<object>.Success(200, new
            {
                IsImpersonating = isImpersonating,
                OriginalUserId = originalUserId,
                CurrentUserId = currentUserId,
                CurrentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value
            }));
        }

        /// <summary>
        /// Get all users (Admin only)
        /// </summary>
        [HttpGet("users")]
        [Authorize(Roles = "ADMIN,SITEADMIN")]
        public IActionResult GetAllUsers()
        {
            var users = _userManager.Users.Select(u => new
            {
                u.Id,
                u.Email,
                u.UserName,
                u.FirstName,
                u.LastName,
                u.IsEmailConfirmed,
                u.CreatedDate
            }).ToList();

            return CreateActionResult(CustomResponseDto<object>.Success(200, users));
        }
    }
}
