using CapFinLoan.Auth.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Logging;

namespace CapFinLoan.Auth.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(EmailSettings emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("[EMAIL] Starting email send process");
        Console.WriteLine($"[EMAIL] Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"[EMAIL] To: {toEmail}");
        Console.WriteLine($"[EMAIL] User Name: {userName}");
        Console.WriteLine($"[EMAIL] Reset Link: {resetLink}");
        Console.WriteLine("----------------------------------------");
        Console.WriteLine("[EMAIL] Configuration:");
        Console.WriteLine($"[EMAIL] SMTP Server: {_emailSettings.SmtpServer}");
        Console.WriteLine($"[EMAIL] Port: {_emailSettings.Port}");
        Console.WriteLine($"[EMAIL] Sender Name: {_emailSettings.SenderName}");
        Console.WriteLine($"[EMAIL] Sender Email: {_emailSettings.SenderEmail}");
        Console.WriteLine($"[EMAIL] Username: {_emailSettings.Username}");
        Console.WriteLine($"[EMAIL] Password: {(string.IsNullOrEmpty(_emailSettings.Password) ? "NOT SET" : $"SET (length: {_emailSettings.Password.Length})")}");
        Console.WriteLine($"[EMAIL] Enable SSL: {_emailSettings.EnableSsl}");
        Console.WriteLine("----------------------------------------");

        try
        {
            _logger.LogInformation("Preparing to send password reset email to {Email}", toEmail);

            Console.WriteLine("[EMAIL] Creating email message...");
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(new MailboxAddress(userName, toEmail));
            message.Subject = "Reset Your CapFinLoan Password";
            Console.WriteLine("[EMAIL] Message created successfully");

            Console.WriteLine("[EMAIL] Building email body...");
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = GetPasswordResetEmailTemplate(userName, resetLink)
            };
            message.Body = bodyBuilder.ToMessageBody();
            Console.WriteLine("[EMAIL] Email body built successfully");

            Console.WriteLine("[EMAIL] Creating SMTP client...");
            using var client = new SmtpClient();
            
            Console.WriteLine($"[EMAIL] Connecting to SMTP server: {_emailSettings.SmtpServer}:{_emailSettings.Port}");
            _logger.LogInformation("Connecting to SMTP server {Server}:{Port}", _emailSettings.SmtpServer, _emailSettings.Port);
            
            await client.ConnectAsync(
                _emailSettings.SmtpServer, 
                _emailSettings.Port, 
                _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            Console.WriteLine("[EMAIL] ✅ Connected to SMTP server successfully");
            Console.WriteLine($"[EMAIL] Server capabilities: {client.Capabilities}");

            Console.WriteLine($"[EMAIL] Authenticating with username: {_emailSettings.Username}");
            _logger.LogInformation("Authenticating with SMTP server as {Username}", _emailSettings.Username);
            
            await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password, cancellationToken);

            Console.WriteLine("[EMAIL] ✅ Authenticated successfully");

            Console.WriteLine($"[EMAIL] Sending email to {toEmail}...");
            _logger.LogInformation("Sending email to {Email}", toEmail);
            
            await client.SendAsync(message, cancellationToken);
            
            Console.WriteLine("[EMAIL] ✅ Email sent successfully");
            
            Console.WriteLine("[EMAIL] Disconnecting from SMTP server...");
            await client.DisconnectAsync(true, cancellationToken);
            Console.WriteLine("[EMAIL] ✅ Disconnected successfully");

            Console.WriteLine("========================================");
            Console.WriteLine("[EMAIL] ✅✅✅ EMAIL SENT SUCCESSFULLY ✅✅✅");
            Console.WriteLine("========================================");
            
            _logger.LogInformation("Password reset email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("[EMAIL ERROR] ❌❌❌ EMAIL SENDING FAILED ❌❌❌");
            Console.WriteLine($"[EMAIL ERROR] Exception Type: {ex.GetType().Name}");
            Console.WriteLine($"[EMAIL ERROR] Message: {ex.Message}");
            Console.WriteLine($"[EMAIL ERROR] Stack Trace:");
            Console.WriteLine(ex.ToString());
            
            if (ex.InnerException != null)
            {
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("[EMAIL ERROR] Inner Exception:");
                Console.WriteLine($"[EMAIL ERROR] Type: {ex.InnerException.GetType().Name}");
                Console.WriteLine($"[EMAIL ERROR] Message: {ex.InnerException.Message}");
                Console.WriteLine(ex.InnerException.ToString());
            }
            
            Console.WriteLine("========================================");
            Console.WriteLine("[RESET LINK FALLBACK]:");
            Console.WriteLine(resetLink);
            Console.WriteLine("========================================");
            
            _logger.LogError(ex, "Failed to send password reset email to {Email}. Error: {Message}", toEmail, ex.Message);
            
            throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
        }
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
