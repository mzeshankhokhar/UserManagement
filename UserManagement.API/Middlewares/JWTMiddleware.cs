using UserManagement.Core.Services;

namespace UserManagement.API.Middlewares
{
    public class JwtRefreshMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtRefreshMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
        {
            var accessToken = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (!string.IsNullOrEmpty(accessToken))
            {
                // Check if the access token is expired
                var isTokenExpired = await tokenService.IsTokenExpired(accessToken);
                if (isTokenExpired)
                {
                    // Attempt to get the refresh token from the headers
                    var refreshToken = context.Request.Headers["RefreshToken"].ToString();

                    if (!string.IsNullOrEmpty(refreshToken))
                    {
                        try
                        {
                            // Attempt to refresh tokens using the refresh token
                            var (newAccessToken, newRefreshToken, expiresAt) = 
                                await tokenService.RefreshAccessTokenAsync(refreshToken);

                            if (!string.IsNullOrEmpty(newRefreshToken) && !string.IsNullOrEmpty(newAccessToken))
                            {
                                // If new tokens are successfully generated, update response headers with new tokens
                                context.Response.Headers["AccessToken"] = newAccessToken;
                                context.Response.Headers["RefreshToken"] = newRefreshToken;
                                context.Response.Headers["TokenExpiresAt"] = expiresAt.ToString("o");

                                // Update the request authorization header so the request can proceed
                                context.Request.Headers["Authorization"] = $"Bearer {newAccessToken}";
                            }
                            else
                            {
                                // Refresh failed, set status code to 401 Unauthorized
                                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                await context.Response.WriteAsJsonAsync(new { message = "Token refresh failed. Please login again." });
                                return;
                            }
                        }
                        catch (Exception)
                        {
                            // Log exception if needed, then set status code to 401 Unauthorized
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsJsonAsync(new { message = "Token validation failed." });
                            return;
                        }
                    }
                    else
                    {
                        // Missing refresh token, set status code to 401 Unauthorized
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { message = "Access token expired. Please provide refresh token." });
                        return;
                    }
                }
            }

            // Proceed to the next middleware in the pipeline
            await _next(context);
        }
    }
}
