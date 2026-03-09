namespace UserManagement.Core.DTOs;

public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; }
}

public class TokenResponseDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public bool RememberMe { get; set; }
}
