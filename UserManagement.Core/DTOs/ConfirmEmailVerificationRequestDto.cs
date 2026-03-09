namespace UserManagement.Core.DTOs;

public class ConfirmEmailVerificationRequestDto
{
    public string EmailOrUserName { get; set; }
    public string Token { get; set; }
}
