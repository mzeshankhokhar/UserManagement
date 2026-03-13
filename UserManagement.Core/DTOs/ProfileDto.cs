namespace UserManagement.Core.DTOs;

/// <summary>
/// Request to update user profile
/// </summary>
public class UpdateProfileRequestDto
{
    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; }

    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; set; }

    /// <summary>
    /// Phone number
    /// </summary>
    public string PhoneNumber { get; set; }

    /// <summary>
    /// Date of birth
    /// </summary>
    public DateTime? DateOfBirth { get; set; }
}

/// <summary>
/// Request to update email address (requires verification)
/// </summary>
public class UpdateEmailRequestDto
{
    /// <summary>
    /// New email address
    /// </summary>
    public string NewEmail { get; set; }

    /// <summary>
    /// Current password for security
    /// </summary>
    public string CurrentPassword { get; set; }
}

/// <summary>
/// Request to confirm email change with verification code
/// </summary>
public class ConfirmEmailChangeRequestDto
{
    /// <summary>
    /// New email address
    /// </summary>
    public string NewEmail { get; set; }

    /// <summary>
    /// Verification code sent to new email
    /// </summary>
    public string VerificationCode { get; set; }
}

/// <summary>
/// Request to delete account
/// </summary>
public class DeleteAccountRequestDto
{
    /// <summary>
    /// Current password for verification
    /// </summary>
    public string CurrentPassword { get; set; }
}

/// <summary>
/// User profile response
/// </summary>
public class UserProfileResponseDto
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string UserName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string PhoneNumber { get; set; }
    public bool? IsPhoneNumberConfirmed { get; set; }
    public bool? IsEmailConfirmed { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public List<string> Roles { get; set; } = new List<string>();
}
