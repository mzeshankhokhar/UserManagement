using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Core.DTOs;
using UserManagement.Core.Services;
using UserManagement.Core.Model;
using AutoMapper;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.PeopleService.v1;

namespace UserManagement.API.Controllers
{
    /// <summary>
    /// Handles external authentication providers (Google, etc.)
    /// </summary>
    public class AuthController : CustomBaseController
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly JwtSettings _jwtSettings;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ITokenService tokenService,
            IOptions<JwtSettings> jwtOptions)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _jwtSettings = jwtOptions.Value;
        }

        /// <summary>
        /// Authenticate user via Google OAuth
        /// </summary>
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLoginAsync([FromBody] GoogleAuthDto model)
        {
            try
            {
                // Validate Google token
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _jwtSettings.GOOGLE_CLIENT_ID }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(model.TokenId, settings);
                if (payload == null)
                {
                    return CreateActionResult(CustomResponseDto<string>.Fail(400, "Invalid Google token."));
                }

                // Get additional profile info from Google People API
                var credential = GoogleCredential.FromAccessToken(model.AccessToken);
                var service = new PeopleServiceService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "UserManagement"
                });

                var request = service.People.Get("people/me");
                request.PersonFields = "names,emailAddresses,birthdays";
                var profile = await request.ExecuteAsync();

                var birthday = profile.Birthdays?.FirstOrDefault()?.Date;

                // Find or create user
                var user = await _userManager.FindByEmailAsync(payload.Email);
                if (user == null)
                {
                    user = new User
                    {
                        UserName = payload.Email,
                        Email = payload.Email,
                        IsEmailConfirmed = payload.EmailVerified,
                        FirstName = payload.GivenName,
                        LastName = payload.FamilyName,
                        DateOfBirth = birthday != null 
                            ? new DateTime((int)birthday.Year, (int)birthday.Month, (int)birthday.Day) 
                            : null,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        Password = "googleAuthentication",
                        CreatedDate = DateTime.UtcNow
                    };

                    var createResult = await _userManager.CreateAsync(user, "googleAuthentication");
                    if (!createResult.Succeeded)
                    {
                        return CreateActionResult(CustomResponseDto<string>.Fail(400, 
                            createResult.Errors.Select(x => x.Description).ToList()));
                    }

                    user = await _userManager.FindByEmailAsync(payload.Email);
                }

                // Sign in user
                var signInResult = await _signInManager.PasswordSignInAsync(user, "googleAuthentication", false, false);
                if (!signInResult.Succeeded)
                {
                    return CreateActionResult(CustomResponseDto<string>.Fail(400, "Error occurred while signing in with Google."));
                }

                // Generate tokens
                var accessToken = await _tokenService.GenerateAccessTokenAsync(user.UserName);
                var refreshToken = await _tokenService.GenerateRefreshTokenAsync();

                var tokenModel = new Token
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    IsImpersonate = false,
                    CreatedDate = DateTime.UtcNow,
                    UserId = user.Id
                };
                await _tokenService.StoreTokenAsync(tokenModel);

                return CreateActionResult(CustomResponseDto<object>.Success(200, new
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                }));
            }
            catch (Exception ex)
            {
                return CreateActionResult(CustomResponseDto<string>.Fail(400, $"Google authentication failed: {ex.Message}"));
            }
        }
    }
}
