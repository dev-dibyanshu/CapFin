using System.ComponentModel.DataAnnotations;

namespace CapFinLoan.Auth.Application.Contracts.Requests;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
}
