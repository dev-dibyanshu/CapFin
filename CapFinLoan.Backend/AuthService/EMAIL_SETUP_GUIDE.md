# Email Setup Guide for Password Reset

## Overview
The Auth service now sends real password reset emails using SMTP (Gmail).

## Gmail Setup (Recommended for Development)

### Step 1: Enable 2-Factor Authentication
1. Go to your Google Account: https://myaccount.google.com/
2. Navigate to **Security**
3. Enable **2-Step Verification**

### Step 2: Generate App Password
1. Go to: https://myaccount.google.com/apppasswords
2. Select **Mail** as the app
3. Select **Other (Custom name)** as the device
4. Enter name: "CapFinLoan Auth Service"
5. Click **Generate**
6. Copy the 16-character password (remove spaces)

### Step 3: Update appsettings.json
Edit `CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API/appsettings.json`:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "CapFinLoan",
    "SenderEmail": "your_email@gmail.com",
    "Username": "your_email@gmail.com",
    "Password": "your_16_char_app_password",
    "EnableSsl": true
  }
}
```

Replace:
- `your_email@gmail.com` with your Gmail address
- `your_16_char_app_password` with the app password from Step 2

## Alternative: Use appsettings.Development.json (Recommended)

Create `appsettings.Development.json` (not tracked in git):

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "CapFinLoan",
    "SenderEmail": "your_email@gmail.com",
    "Username": "your_email@gmail.com",
    "Password": "your_16_char_app_password",
    "EnableSsl": true
  }
}
```

## Testing the Email Flow

### 1. Start the Auth Service
```bash
cd CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API
dotnet run
```

### 2. Request Password Reset
From the frontend:
1. Go to http://localhost:5174/login
2. Click "Forgot your password?"
3. Enter your email
4. Click "Send Reset Link"

### 3. Check Logs
The console will show:
```
Password reset requested for email: user@example.com
Generating password reset token for user: {guid}
Reset link generated for user@example.com: http://localhost:5174/reset-password?...
Connecting to SMTP server smtp.gmail.com:587
Authenticating with SMTP server as your_email@gmail.com
Sending email to user@example.com
Password reset email sent successfully to user@example.com
```

### 4. Check Email
- Check your inbox for the reset email
- Click the "Reset Password" button
- Or copy the link and paste in browser

### 5. Reset Password
1. Enter new password
2. Confirm password
3. Click "Reset Password"
4. Should redirect to login

### 6. Test Login
Login with the new password

## Troubleshooting

### Error: "Failed to send email"
**Check logs for specific error:**

#### "Authentication failed"
- Verify app password is correct (no spaces)
- Ensure 2FA is enabled on Gmail
- Try generating a new app password

#### "Connection refused"
- Check firewall settings
- Verify port 587 is not blocked
- Try port 465 with SSL

#### "Sender address rejected"
- Ensure SenderEmail matches Username
- Verify Gmail account is active

### Fallback: Console Logging
If email fails, the reset link is logged to console:
```
FALLBACK: Password reset link for user@example.com: http://localhost:5174/reset-password?...
```

Copy this link and use it manually.

## Other SMTP Providers

### Outlook/Hotmail
```json
{
  "SmtpServer": "smtp-mail.outlook.com",
  "Port": 587,
  "EnableSsl": true
}
```

### SendGrid
```json
{
  "SmtpServer": "smtp.sendgrid.net",
  "Port": 587,
  "Username": "apikey",
  "Password": "your_sendgrid_api_key",
  "EnableSsl": true
}
```

### Mailgun
```json
{
  "SmtpServer": "smtp.mailgun.org",
  "Port": 587,
  "Username": "postmaster@your-domain.mailgun.org",
  "Password": "your_mailgun_password",
  "EnableSsl": true
}
```

## Security Notes

1. **Never commit credentials to git**
   - Use `appsettings.Development.json` (add to .gitignore)
   - Or use environment variables
   - Or use Azure Key Vault in production

2. **App Passwords are safer than account passwords**
   - Can be revoked without changing account password
   - Limited to specific app access

3. **Token Expiry**
   - Reset tokens expire in 15 minutes (ASP.NET Identity default)
   - Tokens are single-use only

4. **Rate Limiting** (Future Enhancement)
   - Consider adding rate limiting to prevent abuse
   - Limit password reset requests per email/IP

## Email Template

The password reset email includes:
- Professional HTML template
- Blue CapFinLoan branding
- Clear "Reset Password" button
- Clickable link as fallback
- Security warnings
- 15-minute expiry notice

## API Endpoints

### Forgot Password
```
POST /api/auth/forgot-password
Content-Type: application/json

{
  "email": "user@example.com"
}

Response: 200 OK
{
  "message": "If the email exists, a reset link has been sent."
}
```

### Reset Password
```
POST /api/auth/reset-password
Content-Type: application/json

{
  "email": "user@example.com",
  "token": "base64_encoded_token",
  "newPassword": "NewPassword123!"
}

Response: 200 OK
{
  "message": "Password reset successful."
}

Response: 400 Bad Request
{
  "message": "Invalid or expired reset token."
}
```

## Logs to Monitor

Enable debug logging in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "CapFinLoan.Auth": "Debug"
    }
  }
}
```

This will show:
- Email requests
- Token generation
- SMTP connection details
- Email sending status
- Errors with stack traces
