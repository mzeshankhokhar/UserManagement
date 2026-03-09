namespace UserManagement.Core.DTOs;

/// <summary>
/// Request to send verification code
/// </summary>
public class SendVerificationCodeRequestDto
{
    /// <summary>
    /// Email or phone number to send code to
    /// </summary>
    public string EmailOrPhone { get; set; }

    /// <summary>
    /// Type of verification: "Email", "Phone", "PasswordReset"
    /// </summary>
    public string Type { get; set; } = "Email";
}

/// <summary>
/// Request to verify code
/// </summary>
public class VerifyCodeRequestDto
{
    /// <summary>
    /// Email or phone number the code was sent to
    /// </summary>
    public string EmailOrPhone { get; set; }

    /// <summary>
    /// The 6-digit verification code
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Type of verification: "Email", "Phone", "PasswordReset"
    /// </summary>
    public string Type { get; set; } = "Email";
}

/// <summary>
/// Response after sending verification code
/// </summary>
public class VerificationCodeResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? RemainingAttempts { get; set; }
}

/// <summary>
/// Request to test email sending
/// </summary>
public class TestEmailRequestDto
{
    public string Email { get; set; }
}
