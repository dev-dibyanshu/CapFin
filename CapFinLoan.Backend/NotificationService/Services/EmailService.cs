using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using NotificationService.Configuration;
using NotificationService.Interfaces;

namespace NotificationService.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailService(EmailSettings emailSettings)
    {
        _emailSettings = emailSettings;
    }

    public async Task SendApplicationSubmittedEmailAsync(string toEmail, string applicantName, Guid applicationId)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
        message.To.Add(new MailboxAddress(applicantName, toEmail));
        message.Subject = "Loan Application Submitted Successfully";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = GetApplicationSubmittedEmailTemplate(applicantName, applicationId)
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private string GetApplicationSubmittedEmailTemplate(string applicantName, Guid applicationId)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #777; }}
        .application-id {{ background-color: #e8f5e9; padding: 10px; border-left: 4px solid #4CAF50; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>CapFinLoan</h1>
        </div>
        <div class='content'>
            <h2>Application Submitted Successfully</h2>
            <p>Dear {applicantName},</p>
            <p>Thank you for submitting your loan application with CapFinLoan. We have received your application and it is now under review.</p>
            <div class='application-id'>
                <strong>Application ID:</strong> {applicationId}
            </div>
            <p>Our team will review your application and get back to you within 2-3 business days.</p>
            <p>You can track your application status by logging into your account.</p>
            <p>If you have any questions, please don't hesitate to contact us.</p>
            <p>Best regards,<br>CapFinLoan Team</p>
        </div>
        <div class='footer'>
            <p>&copy; 2026 CapFinLoan. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }
}
