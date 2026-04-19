namespace NotificationService.Interfaces;

public interface IEmailService
{
    Task SendApplicationSubmittedEmailAsync(string toEmail, string applicantName, Guid applicationId);
    Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink);
}
