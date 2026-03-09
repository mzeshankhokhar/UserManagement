namespace UserManagement.Core.DTOs;

public class ImpersonateRequestDto
{
    /// <summary>
    /// The user ID or email to impersonate
    /// </summary>
    public string TargetUserIdOrEmail { get; set; }
}

public class ImpersonateResponseDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public bool IsImpersonating { get; set; }
    public int OriginalUserId { get; set; }
    public string OriginalUserEmail { get; set; }
    public int ImpersonatedUserId { get; set; }
    public string ImpersonatedUserEmail { get; set; }
}
