namespace CapFinLoan.Auth.Application.Events;

public class PasswordResetRequestedEvent
{
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string ResetLink { get; set; } = string.Empty;
}
