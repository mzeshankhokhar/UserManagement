namespace UserManagement.Core.DTOs;

/// <summary>
/// Request to initiate password reset (forgot password) - sends verification code
/// </summary>
public class ForgotPasswordRequestDto
{
    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; }
}

/// <summary>
/// Request to reset password using verification code
/// </summary>
public class ResetPasswordRequestDto
{
    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// 6-digit verification code received via email
    /// </summary>
    public string VerificationCode { get; set; }

    /// <summary>
    /// New password
    /// </summary>
    public string NewPassword { get; set; }

    /// <summary>
    /// Confirm new password
    /// </summary>
    public string ConfirmPassword { get; set; }
}

/// <summary>
/// Request to change password (when user is logged in)
/// </summary>
public class ChangePasswordRequestDto
{
    /// <summary>
    /// Current password
    /// </summary>
    public string CurrentPassword { get; set; }

    /// <summary>
    /// New password
    /// </summary>
    public string NewPassword { get; set; }

    /// <summary>
    /// Confirm new password
    /// </summary>
    public string ConfirmPassword { get; set; }

    /// <summary>
    /// Optional: 6-digit verification code for extra security
    /// </summary>
    public string VerificationCode { get; set; }
}
