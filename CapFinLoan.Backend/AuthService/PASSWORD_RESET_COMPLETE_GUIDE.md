# Complete Password Reset Implementation Guide

## What Was Implemented

### Backend Changes

1. **Email Service** (`CapFinLoan.Auth.Infrastructure/Email/EmailService.cs`)
   - Uses MailKit for SMTP email sending
   - Professional HTML email template
   - Comprehensive error logging
   - Fallback to console logging if email fails

2. **Email Settings** (`CapFinLoan.Auth.Infrastructure/Email/EmailSettings.cs`)
   - Configuration model for SMTP settings
   - Supports Gmail, Outlook, SendGrid, etc.

3. **Enhanced AuthService** (`CapFinLoan.Auth.Application/Services/AuthService.cs`)
   - Integrated email service
   - Detailed logging for debugging
   - Proper error handling
   - Security best practices (no user enumeration)

4. **Configuration** (`appsettings.json`)
   - Added EmailSettings section
   - Enhanced logging for Auth namespace

5. **Dependency Injection** (`Program.cs`)
   - Registered EmailService
   - Configured EmailSettings from appsettings

### Frontend (Already Complete)

1. **Login Page** - Toggle between Login/Signup/Forgot Password
2. **Reset Password Page** - Dedicated page for password reset
3. **Routing** - `/reset-password` route configured
4. **API Integration** - All endpoints connected

## Setup Instructions

### Step 1: Configure Email (Required)

Choose one of these options:

#### Option A: Gmail (Easiest for Development)

1. Enable 2FA on your Gmail account
2. Generate App Password: https://myaccount.google.com/apppasswords
3. Update `appsettings.json`:

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

#### Option B: Use appsettings.Development.json (Recommended)

Create `appsettings.Development.json` (won't be committed to git):

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "CapFinLoan",
    "SenderEmail": "your_email@gmail.com",
    "Username": "your_email@gmail.com",
    "Password": "your_app_password",
    "EnableSsl": true
  }
}
```

### Step 2: Restart Auth Service

```bash
cd CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API
dotnet run
```

## Testing the Complete Flow

### Test 1: Forgot Password (Email Sending)

1. **Open Frontend**
   ```
   http://localhost:5174/login
   ```

2. **Click "Forgot your password?"**

3. **Enter email address**
   - Use a real email you have access to
   - Or use the email configured in appsettings

4. **Click "Send Reset Link"**

5. **Check Backend Logs**
   You should see:
   ```
   Password reset requested for email: user@example.com
   Generating password reset token for user: {guid}
   Reset link generated for user@example.com: http://localhost:5174/reset-password?...
   Connecting to SMTP server smtp.gmail.com:587
   Authenticating with SMTP server as your_email@gmail.com
   Sending email to user@example.com
   Password reset email sent successfully to user@example.com
   ```

6. **Check Email Inbox**
   - Look for email from CapFinLoan
   - Subject: "Reset Your CapFinLoan Password"
   - Should have blue branding and reset button

### Test 2: Reset Password

1. **Open Reset Email**

2. **Click "Reset Password" button**
   - Should open: `http://localhost:5174/reset-password?email=...&token=...`

3. **Enter New Password**
   - Must be at least 6 characters
   - Enter same password in both fields

4. **Click "Reset Password"**

5. **Check Backend Logs**
   ```
   Password reset attempt for email: user@example.com
   Token decoded successfully for user@example.com
   Attempting to reset password for user: {guid}
   Password reset successful for: user@example.com
   ```

6. **Should see success message**
   - "Password reset successfully! Redirecting to login..."
   - Auto-redirects after 2 seconds

### Test 3: Login with New Password

1. **Enter email and new password**

2. **Click "Sign In"**

3. **Should redirect to dashboard**

## Troubleshooting

### Issue: "Failed to send email"

**Check logs for specific error:**

#### Error: "Authentication failed"
```
Solution:
1. Verify app password is correct (no spaces)
2. Ensure 2FA is enabled on Gmail
3. Generate new app password
4. Update appsettings.json
```

#### Error: "Connection refused" or "Connection timeout"
```
Solution:
1. Check firewall settings
2. Verify port 587 is not blocked
3. Try alternative port 465
4. Check internet connection
```

#### Error: "Sender address rejected"
```
Solution:
1. Ensure SenderEmail matches Username
2. Verify Gmail account is active
3. Check if account has sending limits
```

### Issue: Email not received

**Check these:**

1. **Spam/Junk folder**
   - Gmail may filter automated emails

2. **Email address typo**
   - Verify email is correct in database

3. **Backend logs**
   - Look for "Password reset email sent successfully"
   - If not present, check error logs

4. **Fallback link**
   - If email fails, link is logged to console
   - Look for: "FALLBACK: Password reset link for..."

### Issue: "Invalid or expired reset token"

**Possible causes:**

1. **Token expired**
   - Tokens expire in 15 minutes
   - Request new reset link

2. **Token already used**
   - Tokens are single-use
   - Request new reset link

3. **URL encoding issue**
   - Ensure full URL is copied
   - Don't manually edit the URL

## API Endpoints Reference

### 1. Forgot Password
```http
POST http://localhost:7000/api/auth/forgot-password
Content-Type: application/json

{
  "email": "user@example.com"
}
```

**Response:**
```json
{
  "message": "If the email exists, a reset link has been sent."
}
```

**Note:** Always returns 200 OK to prevent user enumeration

### 2. Reset Password
```http
POST http://localhost:7000/api/auth/reset-password
Content-Type: application/json

{
  "email": "user@example.com",
  "token": "base64_encoded_token_from_email",
  "newPassword": "NewPassword123!"
}
```

**Success Response (200 OK):**
```json
{
  "message": "Password reset successful."
}
```

**Error Response (400 Bad Request):**
```json
{
  "message": "Invalid or expired reset token."
}
```

## Email Template Preview

The password reset email includes:

- **Header**: Blue CapFinLoan branding
- **Greeting**: Personalized with user name
- **Message**: Clear explanation
- **Button**: Large "Reset Password" button
- **Link**: Fallback clickable link
- **Security Warning**: 
  - 15-minute expiry notice
  - Don't share link warning
  - Ignore if not requested
- **Footer**: Copyright and automated email notice

## Security Features

1. **No User Enumeration**
   - Always returns same message regardless of email existence
   - Prevents attackers from discovering valid emails

2. **Token Expiry**
   - Tokens expire in 15 minutes
   - Reduces window for attacks

3. **Single-Use Tokens**
   - Token invalidated after use
   - Prevents replay attacks

4. **Secure Token Generation**
   - Uses ASP.NET Identity's built-in token generator
   - Cryptographically secure

5. **HTTPS in Production**
   - Reset links should use HTTPS in production
   - Update frontend URL in AuthService

## Production Considerations

### 1. Update Reset Link URL

In `AuthService.cs`, change:
```csharp
var resetLink = $"https://yourdomain.com/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(encodedToken)}";
```

### 2. Use Environment Variables

Instead of appsettings.json:
```bash
export EmailSettings__SmtpServer="smtp.gmail.com"
export EmailSettings__Port="587"
export EmailSettings__Username="your_email@gmail.com"
export EmailSettings__Password="your_app_password"
```

### 3. Use Professional Email Service

Consider:
- SendGrid (99,000 free emails/month)
- Mailgun (5,000 free emails/month)
- AWS SES (62,000 free emails/month)

### 4. Add Rate Limiting

Prevent abuse:
- Limit reset requests per email (e.g., 3 per hour)
- Limit requests per IP address
- Use middleware or API gateway

### 5. Monitor Email Delivery

- Track email delivery success/failure
- Set up alerts for high failure rates
- Log email metrics

## Files Modified/Created

### Created
- `CapFinLoan.Auth.Infrastructure/Email/EmailSettings.cs`
- `CapFinLoan.Auth.Infrastructure/Email/EmailService.cs`
- `CapFinLoan.Auth.Application/Interfaces/IEmailService.cs`
- `EMAIL_SETUP_GUIDE.md`
- `PASSWORD_RESET_COMPLETE_GUIDE.md`

### Modified
- `CapFinLoan.Auth.Infrastructure/CapFinLoan.Auth.Infrastructure.csproj` (added MailKit)
- `CapFinLoan.Auth.Application/Services/AuthService.cs` (integrated email service)
- `CapFinLoan.Auth.API/Program.cs` (registered email service)
- `CapFinLoan.Auth.API/appsettings.json` (added email settings)

### Frontend (Already Complete)
- `src/pages/Login.jsx` (forgot password flow)
- `src/pages/ResetPassword.jsx` (reset password page)
- `src/App.jsx` (reset password route)

## Success Criteria

✅ User can request password reset from login page
✅ Email is sent with reset link
✅ Email has professional template
✅ Reset link opens reset password page
✅ User can set new password
✅ User can login with new password
✅ Comprehensive logging for debugging
✅ Proper error handling
✅ Security best practices implemented

## Next Steps (Optional Enhancements)

1. **Email Templates**
   - Create reusable email template system
   - Add more email types (welcome, verification, etc.)

2. **Email Queue**
   - Use background job for email sending
   - Retry failed emails
   - Use Hangfire or similar

3. **Email Verification**
   - Require email verification on signup
   - Send verification email with token

4. **Rate Limiting**
   - Implement request throttling
   - Prevent brute force attacks

5. **Audit Logging**
   - Log all password reset attempts
   - Track successful/failed resets
   - Store in database for analysis

6. **Multi-language Support**
   - Translate email templates
   - Support user's preferred language

7. **SMS Backup**
   - Send SMS if email fails
   - Two-factor authentication option
