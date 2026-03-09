using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Core.DTOs;
using UserManagement.Core.Services;
using UserManagement.Core.Model;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace UserManagement.API.Controllers
{
    /// <summary>
    /// Handles user account operations: registration, login, email verification, etc.
    /// </summary>
    public class AccountController : CustomBaseController
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IVerificationCodeService _verificationCodeService;
        private readonly IMapper _mapper;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            SignInManager<User> signInManager,
            ITokenService tokenService,
            IEmailService emailService,
            IVerificationCodeService verificationCodeService,
            IMapper mapper,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _verificationCodeService = verificationCodeService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Get user by username (admin use)
        /// </summary>
        [Authorize]
        [HttpGet("[action]/{userName}")]
        public async Task<IActionResult> GetSingleUserByUserNameAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(404, "User not found"));
            }

            var userDto = _mapper.Map<UserDto>(user);
            return CreateActionResult(CustomResponseDto<UserDto>.Success(200, userDto));
        }

        /// <summary>
        /// Get current authenticated user profile (includes impersonation status)
        /// </summary>
        [Authorize]
        [HttpGet("[action]")]
        public async Task<IActionResult> GetUserAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(404, "User not found"));
            }

            var userDto = _mapper.Map<UserDto>(user);

            // Add impersonation info
            var (isImpersonating, originalUserId) = _tokenService.GetImpersonationInfo(User);

            return CreateActionResult(CustomResponseDto<object>.Success(200, new
            {
                User = userDto,
                IsImpersonating = isImpersonating,
                OriginalUserId = originalUserId
            }));
        }

        /// <summary>
        /// Get current user's profile
        /// </summary>
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfileAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(404, "User not found"));
            }

            var profile = new UserProfileResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsPhoneNumberConfirmed = user.IsPhoneNumberConfirmed,
                IsEmailConfirmed = user.IsEmailConfirmed,
                DateOfBirth = user.DateOfBirth,
                CreatedDate = user.CreatedDate,
                UpdatedDate = user.UpdatedDate
            };

            return CreateActionResult(CustomResponseDto<UserProfileResponseDto>.Success(200, profile));
        }

        /// <summary>
        /// Update current user's profile (name, phone, date of birth)
        /// </summary>
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfileAsync([FromBody] UpdateProfileRequestDto request)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(404, "User not found"));
            }

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(request.FirstName))
            {
                user.FirstName = request.FirstName;
            }

            if (!string.IsNullOrWhiteSpace(request.LastName))
            {
                user.LastName = request.LastName;
            }

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                user.PhoneNumber = request.PhoneNumber;
                user.IsPhoneNumberConfirmed = false; // Reset phone confirmation
            }

            if (request.DateOfBirth.HasValue)
            {
                user.DateOfBirth = request.DateOfBirth;
            }

            user.UpdatedDate = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return CreateActionResult(CustomResponseDto<object>.Fail(400, 
                    result.Errors.Select(e => e.Description).ToList()));
            }

            _logger.LogInformation("Profile updated for user {Email}", email);

            var profile = new UserProfileResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsPhoneNumberConfirmed = user.IsPhoneNumberConfirmed,
                IsEmailConfirmed = user.IsEmailConfirmed,
                DateOfBirth = user.DateOfBirth,
                CreatedDate = user.CreatedDate,
                UpdatedDate = user.UpdatedDate
            };

            return CreateActionResult(CustomResponseDto<UserProfileResponseDto>.Success(200, profile));
        }

        /// <summary>
        /// Request email change - sends verification code to new email
        /// </summary>
        [Authorize]
        [HttpPost("change-email")]
        public async Task<IActionResult> RequestEmailChangeAsync([FromBody] UpdateEmailRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.NewEmail))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "New email is required."));
            }

            var currentEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _userManager.FindByEmailAsync(currentEmail);

            if (user == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(404, "User not found"));
            }

            // Verify current password
            if (!string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                var passwordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
                if (!passwordValid)
                {
                    return CreateActionResult(CustomResponseDto<string>.Fail(400, "Current password is incorrect."));
                }
            }

            // Check if new email is already taken
            var existingUser = await _userManager.FindByEmailAsync(request.NewEmail);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(409, "This email is already in use."));
            }

            // Check rate limiting
            var (canRequest, waitSeconds) = await _verificationCodeService.CanRequestNewCodeAsync(user.Id, "EmailChange");
            if (!canRequest)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(429, 
                    $"Please wait {waitSeconds} seconds before requesting a new code."));
            }

            // Generate verification code and send to NEW email
            var code = await _verificationCodeService.GenerateCodeAsync(user.Id, request.NewEmail, "EmailChange");

            try
            {
                await _emailService.SendAsync(
                    request.NewEmail,
                    "Verify Your New Email Address",
                    GetVerificationCodeEmailTemplate(user.FirstName ?? user.UserName, code, "EmailChange")
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email change verification to {NewEmail}", request.NewEmail);
                return CreateActionResult(CustomResponseDto<string>.Fail(500, "Failed to send verification code."));
            }

            return CreateActionResult(CustomResponseDto<VerificationCodeResponseDto>.Success(200, new VerificationCodeResponseDto
            {
                Success = true,
                Message = $"Verification code sent to {request.NewEmail}",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            }));
        }

        /// <summary>
        /// Confirm email change with verification code
        /// </summary>
        [Authorize]
        [HttpPost("confirm-email-change")]
        public async Task<IActionResult> ConfirmEmailChangeAsync([FromBody] ConfirmEmailChangeRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.NewEmail) || string.IsNullOrWhiteSpace(request?.VerificationCode))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "New email and verification code are required."));
            }

            var currentEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _userManager.FindByEmailAsync(currentEmail);

            if (user == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(404, "User not found"));
            }

            // Verify the code
            var (isValid, message) = await _verificationCodeService.VerifyCodeAsync(
                request.NewEmail, request.VerificationCode, "EmailChange");

            if (!isValid)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, message));
            }

            // Check if new email is still available
            var existingUser = await _userManager.FindByEmailAsync(request.NewEmail);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(409, "This email is already in use."));
            }

            // Update email
            var oldEmail = user.Email;
            user.Email = request.NewEmail;
            user.UserName = request.NewEmail; // Keep username in sync with email
            user.IsEmailConfirmed = true; // Already verified via code
            user.UpdatedDate = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return CreateActionResult(CustomResponseDto<object>.Fail(400, 
                    result.Errors.Select(e => e.Description).ToList()));
            }

            // Revoke all tokens (user needs to login again with new email)
            await _tokenService.RevokeAllUserTokensAsync(user.Id);

            _logger.LogInformation("Email changed from {OldEmail} to {NewEmail} for user {UserId}", 
                oldEmail, request.NewEmail, user.Id);

            return CreateActionResult(CustomResponseDto<object>.Success(200, new
            {
                Success = true,
                Message = "Email changed successfully. Please login with your new email.",
                NewEmail = request.NewEmail
            }));
        }

        /// <summary>
        /// Delete current user's account
        /// </summary>
        [Authorize]
        [HttpDelete("profile")]
        public async Task<IActionResult> DeleteAccountAsync([FromBody] DeleteAccountRequestDto request)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(404, "User not found"));
            }

            // Verify password
            if (string.IsNullOrWhiteSpace(request?.CurrentPassword))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Password is required to delete account."));
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!passwordValid)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Password is incorrect."));
            }

            // Revoke all tokens first
            await _tokenService.RevokeAllUserTokensAsync(user.Id);

            // Delete user
            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                return CreateActionResult(CustomResponseDto<object>.Fail(400, 
                    result.Errors.Select(e => e.Description).ToList()));
            }

            _logger.LogInformation("Account deleted for user {Email}", email);

            return CreateActionResult(CustomResponseDto<string>.Success(200, "Account deleted successfully."));
        }

        /// <summary>
        /// Register a new user account
        /// </summary>
        [HttpPost("[action]")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterDto registerDto)
        {
            if (string.IsNullOrWhiteSpace(registerDto.Email) || string.IsNullOrWhiteSpace(registerDto.Password))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Email and Password are required."));
            }

            // Check for existing user
            var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
            if (existingUser != null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(409, "An account with this email already exists."));
            }

            // Create user
            var user = new User
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                PhoneNumber = registerDto.Phone,
                IsEmailConfirmed = false,
                Password = registerDto.Password, // Store plaintext password
                SecurityStamp = Guid.NewGuid().ToString(),
                CreatedDate = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, registerDto.Password);
            if (!createResult.Succeeded)
            {
                return CreateActionResult(CustomResponseDto<object>.Fail(400, 
                    createResult.Errors.Select(e => e.Description).ToList()));
            }

            // Assign default role
            await AssignDefaultRoleAsync(user);

            // Send verification code via email
            var codeSent = await SendVerificationCodeEmailAsync(user);

            return CreateActionResult(CustomResponseDto<object>.Success(201, new
            {
                status = "PENDING_VERIFICATION",
                userId = user.Id,
                email = user.Email,
                emailVerified = false,
                verificationCodeSent = codeSent,
                message = "User created successfully. A 6-digit verification code has been sent to your email."
            }));
        }

        /// <summary>
        /// Login with email/username and password
        /// </summary>
        [HttpPost("[action]")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginDto loginDto)
        {
            var user = await _userManager.FindByNameAsync(loginDto.UserName)
                       ?? await _userManager.FindByEmailAsync(loginDto.UserName);

            if (user == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(404, "Username or Password is incorrect."));
            }

            // Verify password
            var passwordOk = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!passwordOk)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Username or Password is incorrect."));
            }

            // Check email verification
            var emailVerified = user.IsEmailConfirmed ?? false;
            if (!emailVerified)
            {
                return CreateActionResult(CustomResponseDto<object>.Success(200, new
                {
                    status = "PENDING_VERIFICATION",
                    emailVerified,
                    userId = user.Id,
                    email = user.Email
                }));
            }

            // Sign in
            var result = await _signInManager.PasswordSignInAsync(user, loginDto.Password, loginDto.RememberMe, false);

            if (result.IsLockedOut)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(423, "Account is locked."));
            }

            if (result.IsNotAllowed)
            {
                return CreateActionResult(CustomResponseDto<object>.Success(200, new
                {
                    status = "PENDING_VERIFICATION",
                    emailVerified = user.IsEmailConfirmed,
                    userId = user.Id,
                    email = user.Email
                }));
            }

            if (!result.Succeeded)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Username or Password is incorrect."));
            }

            // Generate tokens with Remember Me support
            var accessToken = await _tokenService.GenerateAccessTokenAsync(user.UserName, loginDto.RememberMe);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync();
            var (accessExpiry, refreshExpiry) = _tokenService.GetTokenExpiry(loginDto.RememberMe);

            await _tokenService.StoreTokenAsync(new Token
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                IsImpersonate = false,
                RememberMe = loginDto.RememberMe,
                ExpiresAt = accessExpiry,
                RefreshTokenExpiresAt = refreshExpiry,
                UserId = user.Id
            });

            return CreateActionResult(CustomResponseDto<object>.Success(200, new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = accessExpiry,
                RefreshTokenExpiresAt = refreshExpiry,
                RememberMe = loginDto.RememberMe
            }));
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.RefreshToken))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Refresh token is required."));
            }

            var (accessToken, refreshToken, expiresAt) = await _tokenService.RefreshAccessTokenAsync(request.RefreshToken);

            if (string.IsNullOrEmpty(accessToken))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(401, "Invalid or expired refresh token."));
            }

            return CreateActionResult(CustomResponseDto<object>.Success(200, new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt
            }));
        }

        /// <summary>
        /// Revoke refresh token (logout from current device)
        /// </summary>
        [Authorize]
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeTokenAsync([FromBody] RefreshTokenRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.RefreshToken))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Refresh token is required."));
            }

            var revoked = await _tokenService.RevokeTokenAsync(request.RefreshToken);

            if (!revoked)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(404, "Token not found."));
            }

            return CreateActionResult(CustomResponseDto<string>.Success(200, "Token revoked successfully."));
        }

        /// <summary>
        /// Revoke all tokens (logout from all devices)
        /// </summary>
        [Authorize]
        [HttpPost("revoke-all-tokens")]
        public async Task<IActionResult> RevokeAllTokensAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                              ?? User.FindFirst("sid")?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(401, "Invalid user."));
            }

            await _tokenService.RevokeAllUserTokensAsync(userId);

            return CreateActionResult(CustomResponseDto<string>.Success(200, "All tokens revoked. You have been logged out from all devices."));
        }

        /// <summary>
        /// Send a 6-digit verification code to email
        /// </summary>
        [HttpPost("send-verification-code")]
        public async Task<IActionResult> SendVerificationCodeAsync([FromBody] SendVerificationCodeRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.EmailOrPhone))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Email is required."));
            }

            var user = await _userManager.FindByEmailAsync(request.EmailOrPhone);
            if (user == null)
            {
                // Return success to prevent email enumeration
                return CreateActionResult(CustomResponseDto<VerificationCodeResponseDto>.Success(200, new VerificationCodeResponseDto
                {
                    Success = true,
                    Message = "If an account exists with this email, a verification code has been sent.",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15)
                }));
            }

            // Check if already verified (for email type)
            if (request.Type == "Email" && user.IsEmailConfirmed == true)
            {
                return CreateActionResult(CustomResponseDto<string>.Success(200, "Email is already verified."));
            }

            // Check rate limiting
            var (canRequest, waitSeconds) = await _verificationCodeService.CanRequestNewCodeAsync(user.Id, request.Type);
            if (!canRequest)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(429, 
                    $"Please wait {waitSeconds} seconds before requesting a new code."));
            }

            // Generate and send code
            var code = await _verificationCodeService.GenerateCodeAsync(user.Id, user.Email, request.Type);
            var expiresAt = DateTime.UtcNow.AddMinutes(15);

            try
            {
                await _emailService.SendAsync(
                    user.Email,
                    "Your Verification Code",
                    GetVerificationCodeEmailTemplate(user.FirstName ?? user.UserName, code, request.Type)
                );
            }
            catch
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(500, "Failed to send verification code. Please try again."));
            }

            return CreateActionResult(CustomResponseDto<VerificationCodeResponseDto>.Success(200, new VerificationCodeResponseDto
            {
                Success = true,
                Message = "Verification code sent successfully.",
                ExpiresAt = expiresAt
            }));
        }

        /// <summary>
        /// Verify email/phone using 6-digit code
        /// </summary>
        [HttpPost("verify-code")]
        public async Task<IActionResult> VerifyCodeAsync([FromBody] VerifyCodeRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.EmailOrPhone) || string.IsNullOrWhiteSpace(request?.Code))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Email and code are required."));
            }

            var user = await _userManager.FindByEmailAsync(request.EmailOrPhone);
            if (user == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Invalid verification request."));
            }

            // Verify the code
            var (isValid, message) = await _verificationCodeService.VerifyCodeAsync(
                request.EmailOrPhone, request.Code, request.Type);

            if (!isValid)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, message));
            }

            // Mark email as confirmed
            if (request.Type == "Email")
            {
                user.IsEmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }

            return CreateActionResult(CustomResponseDto<object>.Success(200, new
            {
                Success = true,
                Message = "Verification successful.",
                EmailVerified = user.IsEmailConfirmed
            }));
        }

        /// <summary>
        /// Validate current JWT token
        /// </summary>
        [Authorize]
        [HttpPost("[action]")]
        public IActionResult ValidateAuthAsync()
        {
            return CreateActionResult(CustomResponseDto<string>.Success(200, "Token is valid"));
        }

        /// <summary>
        /// Test email sending (for debugging)
        /// </summary>
        [HttpPost("test-email")]
        public async Task<IActionResult> TestEmailAsync([FromBody] TestEmailRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.Email))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Email is required."));
            }

            try
            {
                _logger.LogInformation("Testing email to {Email}", request.Email);

                await _emailService.SendAsync(
                    request.Email,
                    "Test Email from UserManagement",
                    "<h1>Test Email</h1><p>This is a test email to verify SMTP configuration is working.</p>"
                );

                return CreateActionResult(CustomResponseDto<string>.Success(200, 
                    $"Test email sent successfully to {request.Email}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test email to {Email}", request.Email);
                return CreateActionResult(CustomResponseDto<string>.Fail(500, 
                    $"Failed to send email: {ex.Message}"));
            }
        }

        /// <summary>
        /// Request password reset - sends a 6-digit verification code to email
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.Email))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Email is required."));
            }

            var user = await _userManager.FindByEmailAsync(request.Email);

            // Always return success to prevent email enumeration attacks
            if (user == null)
            {
                return CreateActionResult(CustomResponseDto<VerificationCodeResponseDto>.Success(200, new VerificationCodeResponseDto
                {
                    Success = true,
                    Message = "If an account with that email exists, a verification code has been sent.",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15)
                }));
            }

            // Check rate limiting
            var (canRequest, waitSeconds) = await _verificationCodeService.CanRequestNewCodeAsync(user.Id, "PasswordReset");
            if (!canRequest)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(429, 
                    $"Please wait {waitSeconds} seconds before requesting a new code."));
            }

            // Generate verification code
            var code = await _verificationCodeService.GenerateCodeAsync(user.Id, user.Email, "PasswordReset");

            try
            {
                await _emailService.SendAsync(
                    user.Email,
                    "Password Reset Code",
                    GetVerificationCodeEmailTemplate(user.FirstName ?? user.UserName, code, "PasswordReset")
                );
            }
            catch (Exception)
            {
                // Log error but don't expose it to prevent enumeration
            }

            return CreateActionResult(CustomResponseDto<VerificationCodeResponseDto>.Success(200, new VerificationCodeResponseDto
            {
                Success = true,
                Message = "If an account with that email exists, a verification code has been sent.",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            }));
        }

        /// <summary>
        /// Verify password reset code (optional step - can go directly to reset-password)
        /// </summary>
        [HttpPost("verify-reset-code")]
        public async Task<IActionResult> VerifyResetCodeAsync([FromBody] VerifyCodeRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.EmailOrPhone) || string.IsNullOrWhiteSpace(request?.Code))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Email and code are required."));
            }

            var user = await _userManager.FindByEmailAsync(request.EmailOrPhone);
            if (user == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Invalid verification request."));
            }

            // Just validate the code without marking it as used
            var verificationCode = await GetValidVerificationCodeAsync(request.EmailOrPhone, request.Code, "PasswordReset");

            if (verificationCode == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Invalid or expired verification code."));
            }

            return CreateActionResult(CustomResponseDto<object>.Success(200, new
            {
                Success = true,
                Message = "Code verified. You can now reset your password.",
                Email = request.EmailOrPhone
            }));
        }

        /// <summary>
        /// Reset password using 6-digit verification code
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request?.NewPassword))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Email and new password are required."));
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Passwords do not match."));
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Invalid request."));
            }

            // Try verification code first (new method)
            if (!string.IsNullOrWhiteSpace(request.VerificationCode))
            {
                var (isValid, message) = await _verificationCodeService.VerifyCodeAsync(
                    request.Email, request.VerificationCode, "PasswordReset");

                if (!isValid)
                {
                    return CreateActionResult(CustomResponseDto<string>.Fail(400, message));
                }

                // Generate a password reset token and use it immediately
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

                if (!result.Succeeded)
                {
                    return CreateActionResult(CustomResponseDto<object>.Fail(400, 
                        result.Errors.Select(e => e.Description).ToList()));
                }
            }
            else
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, 
                    "Verification code is required."));
            }

            // Update the stored plaintext password
            user.Password = request.NewPassword;
            await _userManager.UpdateAsync(user);

            // Revoke all existing tokens for security
            await _tokenService.RevokeAllUserTokensAsync(user.Id);

            return CreateActionResult(CustomResponseDto<string>.Success(200, 
                "Password has been reset successfully. Please login with your new password."));
        }

        /// <summary>
        /// Change password (user must be logged in)
        /// Optionally requires verification code for extra security
        /// </summary>
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.CurrentPassword) || 
                string.IsNullOrWhiteSpace(request?.NewPassword))
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Current password and new password are required."));
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, "Passwords do not match."));
            }

            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(404, "User not found."));
            }

            // If verification code is provided, validate it first
            if (!string.IsNullOrWhiteSpace(request.VerificationCode))
            {
                var (isValid, message) = await _verificationCodeService.VerifyCodeAsync(
                    email, request.VerificationCode, "PasswordChange");

                if (!isValid)
                {
                    return CreateActionResult(CustomResponseDto<string>.Fail(400, message));
                }
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

            if (!result.Succeeded)
            {
                return CreateActionResult(CustomResponseDto<object>.Fail(400, 
                    result.Errors.Select(e => e.Description).ToList()));
            }

            // Update the stored plaintext password
            user.Password = request.NewPassword;
            await _userManager.UpdateAsync(user);

            return CreateActionResult(CustomResponseDto<string>.Success(200, "Password changed successfully."));
        }

        /// <summary>
        /// Send verification code for password change (extra security)
        /// </summary>
        [Authorize]
        [HttpPost("send-change-password-code")]
        public async Task<IActionResult> SendChangePasswordCodeAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(404, "User not found."));
            }

            // Check rate limiting
            var (canRequest, waitSeconds) = await _verificationCodeService.CanRequestNewCodeAsync(user.Id, "PasswordChange");
            if (!canRequest)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(429, 
                    $"Please wait {waitSeconds} seconds before requesting a new code."));
            }

            // Generate verification code
            var code = await _verificationCodeService.GenerateCodeAsync(user.Id, user.Email, "PasswordChange");

            try
            {
                await _emailService.SendAsync(
                    user.Email,
                    "Password Change Verification Code",
                    GetVerificationCodeEmailTemplate(user.FirstName ?? user.UserName, code, "PasswordChange")
                );
            }
            catch
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(500, "Failed to send verification code."));
            }

            return CreateActionResult(CustomResponseDto<VerificationCodeResponseDto>.Success(200, new VerificationCodeResponseDto
            {
                Success = true,
                Message = "Verification code sent to your email.",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            }));
        }

        /// <summary>
        /// Logout current user
        /// </summary>
        [Authorize]
        [HttpPost("[action]")]
        public async Task<IActionResult> LogoutAsync()
        {
            await _signInManager.SignOutAsync();
            return CreateActionResult(CustomResponseDto<string>.Success(200, "Logged out successfully"));
        }

        #region Private Helpers

        private async Task<VerificationCode> GetValidVerificationCodeAsync(string sentTo, string code, string type)
        {
            return await _verificationCodeService.GetValidCodeAsync(sentTo, code, type);
        }

        private async Task AssignDefaultRoleAsync(User user)
        {
            const string roleName = "NORMALUSER";

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new Role
                {
                    Name = roleName,
                    Description = "Normal User",
                    CreatedDate = DateTime.UtcNow
                });
            }

            await _userManager.AddToRoleAsync(user, roleName);
        }

        private async Task<bool> SendVerificationCodeEmailAsync(User user)
        {
            try
            {
                _logger.LogInformation("Generating verification code for user {Email}", user.Email);
                var code = await _verificationCodeService.GenerateCodeAsync(user.Id, user.Email, "Email");
                _logger.LogInformation("Generated code for user {Email}, sending email...", user.Email);

                await _emailService.SendAsync(
                    user.Email,
                    "Your Verification Code - UserManagement",
                    GetVerificationCodeEmailTemplate(user.FirstName ?? user.UserName, code, "Email")
                );
                _logger.LogInformation("Verification code email sent successfully to {Email}", user.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification code email to {Email}", user.Email);
                return false;
            }
        }

        private static string GetVerificationCodeEmailTemplate(string userName, string code, string type)
        {
            var title = type switch
            {
                "PasswordReset" => "Password Reset Code",
                "PasswordChange" => "Password Change Verification",
                "EmailChange" => "Email Change Verification",
                "Phone" => "Phone Verification Code",
                _ => "Email Verification Code"
            };

            var headerColor = type switch
            {
                "PasswordReset" => "#f44336",
                "PasswordChange" => "#ff9800",
                "EmailChange" => "#2196F3",
                _ => "#4CAF50"
            };

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: {headerColor}; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .code-box {{ background-color: #333; color: #fff; padding: 20px; font-size: 32px; font-family: monospace; letter-spacing: 8px; text-align: center; margin: 30px 0; border-radius: 8px; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .warning {{ color: #f44336; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{title}</h1>
        </div>
        <div class='content'>
            <h2>Hello {userName}!</h2>
            <p>Your verification code is:</p>
            <div class='code-box'>{code}</div>
            <p>This code will expire in <strong>15 minutes</strong>.</p>
            <p>Enter this code in the app to complete your verification.</p>
            <p class='warning'>If you didn't request this code, please ignore this email.</p>
        </div>
        <div class='footer'>
            <p>&copy; {DateTime.UtcNow.Year} UserManagement. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        #endregion
    }
}