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

    public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
        message.To.Add(new MailboxAddress(userName, toEmail));
        message.Subject = "Reset Your CapFinLoan Password";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = GetPasswordResetEmailTemplate(userName, resetLink)
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

    private string GetPasswordResetEmailTemplate(string userName, string resetLink)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #2563eb; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border: 1px solid #ddd; }}
        .button {{ 
            display: inline-block; 
            padding: 12px 30px; 
            background-color: #2563eb; 
            color: white; 
            text-decoration: none; 
            border-radius: 5px; 
            margin: 20px 0;
            font-weight: bold;
        }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #777; }}
        .warning {{ 
            background-color: #fff3cd; 
            border-left: 4px solid #ffc107; 
            padding: 15px; 
            margin: 20px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>CapFinLoan</h1>
        </div>
        <div class='content'>
            <h2>Password Reset Request</h2>
            <p>Hello {userName},</p>
            <p>We received a request to reset your password for your CapFinLoan account.</p>
            <p>Click the button below to reset your password:</p>
            <div style='text-align: center;'>
                <a href='{resetLink}' class='button'>Reset Password</a>
            </div>
            <p>Or copy and paste this link into your browser:</p>
            <p style='word-break: break-all; background-color: #e9ecef; padding: 10px; border-radius: 4px;'>
                {resetLink}
            </p>
            <div class='warning'>
                <strong>⚠️ Security Notice:</strong>
                <ul style='margin: 10px 0;'>
                    <li>This link will expire in 15 minutes</li>
                    <li>If you didn't request this reset, please ignore this email</li>
                    <li>Never share this link with anyone</li>
                </ul>
            </div>
            <p>If you have any questions, please contact our support team.</p>
            <p>Best regards,<br>CapFinLoan Team</p>
        </div>
        <div class='footer'>
            <p>&copy; 2026 CapFinLoan. All rights reserved.</p>
            <p>This is an automated email. Please do not reply.</p>
        </div>
    </div>
</body>
</html>";
    }
}
