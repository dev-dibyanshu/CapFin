# Email Debugging Guide - Step by Step

## Current Status

✅ Enhanced debugging is now active
✅ Configuration validation at startup
✅ Detailed logging at every step
✅ Test email endpoint available
⚠️  Email settings contain placeholder values (needs configuration)

## Step 1: Check Startup Logs

When Auth service starts, you should see:

```
========================================
[CONFIG] Email Settings Validation
========================================
[CONFIG] SMTP Server: smtp.gmail.com
[CONFIG] Port: 587
[CONFIG] Sender Name: CapFinLoan
[CONFIG] Sender Email: your_email@gmail.com
[CONFIG] Username: your_email@gmail.com
[CONFIG] Password: ✅ SET (length: 22)
[CONFIG] Enable SSL: True
========================================
```

### What to Check:
- ✅ Password shows "✅ SET" (not "❌ NOT SET")
- ⚠️  If you see "WARNING: Email settings contain placeholder values" → Update credentials

## Step 2: Configure Email Credentials

### Option A: Update appsettings.json

Edit: `CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API/appsettings.json`

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "CapFinLoan",
    "SenderEmail": "YOUR_ACTUAL_EMAIL@gmail.com",
    "Username": "YOUR_ACTUAL_EMAIL@gmail.com",
    "Password": "YOUR_16_CHAR_APP_PASSWORD",
    "EnableSsl": true
  }
}
```

### Option B: Create appsettings.Development.json (Recommended)

Create: `CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API/appsettings.Development.json`

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "CapFinLoan",
    "SenderEmail": "YOUR_ACTUAL_EMAIL@gmail.com",
    "Username": "YOUR_ACTUAL_EMAIL@gmail.com",
    "Password": "YOUR_16_CHAR_APP_PASSWORD",
    "EnableSsl": true
  }
}
```

### Get Gmail App Password:
1. Go to: https://myaccount.google.com/apppasswords
2. Enable 2FA if not already enabled
3. Generate new app password
4. Copy the 16-character password (remove spaces)
5. Paste into configuration

## Step 3: Test Email Sending

### Method 1: Use Test Endpoint (Easiest)

```bash
curl "http://localhost:7001/api/auth/test-email?email=YOUR_EMAIL@gmail.com"
```

Or open in browser:
```
http://localhost:7001/api/auth/test-email?email=YOUR_EMAIL@gmail.com
```

### Expected Output in Console:

```
========================================
[TEST EMAIL] Endpoint called
[TEST EMAIL] Target email: YOUR_EMAIL@gmail.com
========================================
[TEST EMAIL] Calling email service...
========================================
[EMAIL] Starting email send process
[EMAIL] Timestamp: 2026-04-15 10:00:00
[EMAIL] To: YOUR_EMAIL@gmail.com
[EMAIL] User Name: Test User
[EMAIL] Reset Link: http://localhost:5174/reset-password?...
----------------------------------------
[EMAIL] Configuration:
[EMAIL] SMTP Server: smtp.gmail.com
[EMAIL] Port: 587
[EMAIL] Sender Name: CapFinLoan
[EMAIL] Sender Email: YOUR_EMAIL@gmail.com
[EMAIL] Username: YOUR_EMAIL@gmail.com
[EMAIL] Password: SET (length: 16)
[EMAIL] Enable SSL: True
----------------------------------------
[EMAIL] Creating email message...
[EMAIL] Message created successfully
[EMAIL] Building email body...
[EMAIL] Email body built successfully
[EMAIL] Creating SMTP client...
[EMAIL] Connecting to SMTP server: smtp.gmail.com:587
[EMAIL] ✅ Connected to SMTP server successfully
[EMAIL] Server capabilities: ...
[EMAIL] Authenticating with username: YOUR_EMAIL@gmail.com
[EMAIL] ✅ Authenticated successfully
[EMAIL] Sending email to YOUR_EMAIL@gmail.com...
[EMAIL] ✅ Email sent successfully
[EMAIL] Disconnecting from SMTP server...
[EMAIL] ✅ Disconnected successfully
========================================
[EMAIL] ✅✅✅ EMAIL SENT SUCCESSFULLY ✅✅✅
========================================
[TEST EMAIL] ✅ Email sent successfully
```

### Method 2: Test via Frontend

1. Go to: http://localhost:5174/login
2. Click "Forgot your password?"
3. Enter email address
4. Click "Send Reset Link"
5. Watch console logs

## Step 4: Identify Failure Point

### Scenario A: Config Not Loading

**Symptoms:**
```
[CONFIG] SMTP Server: 
[CONFIG] Password: ❌ NOT SET
```

**Solution:**
- Check appsettings.json exists
- Check EmailSettings section exists
- Restart Auth service

### Scenario B: Connection Failed

**Symptoms:**
```
[EMAIL] Connecting to SMTP server: smtp.gmail.com:587
[EMAIL ERROR] ❌❌❌ EMAIL SENDING FAILED ❌❌❌
[EMAIL ERROR] Exception Type: SocketException
[EMAIL ERROR] Message: Connection refused
```

**Solutions:**
- Check firewall blocking port 587
- Try port 465 instead
- Check internet connection
- Verify SMTP server address

### Scenario C: Authentication Failed

**Symptoms:**
```
[EMAIL] ✅ Connected to SMTP server successfully
[EMAIL] Authenticating with username: your@email.com
[EMAIL ERROR] ❌❌❌ EMAIL SENDING FAILED ❌❌❌
[EMAIL ERROR] Exception Type: AuthenticationException
[EMAIL ERROR] Message: 535-5.7.8 Username and Password not accepted
```

**Solutions:**
- ❌ Using regular Gmail password → Use App Password
- ❌ 2FA not enabled → Enable 2FA first
- ❌ App password has spaces → Remove all spaces
- ❌ Wrong app password → Generate new one
- ✅ Verify username matches sender email

### Scenario D: Email Service Not Called

**Symptoms:**
```
[FORGOT PASSWORD] Email requested: user@example.com
[FORGOT PASSWORD] User exists: True
[FORGOT PASSWORD] Generating password reset token...
[FORGOT PASSWORD] Token generated successfully
[FORGOT PASSWORD] Reset link generated:
[RESET LINK]: http://localhost:5174/reset-password?...
[FORGOT PASSWORD] Calling EmailService.SendPasswordResetEmailAsync...
```

**But NO [EMAIL] logs appear**

**Solution:**
- Email service not registered in DI
- Check Program.cs has: `builder.Services.AddScoped<IEmailService, EmailService>();`
- Restart Auth service

### Scenario E: Silent Failure

**Symptoms:**
- No error logs
- No success logs
- Just returns success message

**Solution:**
- Check if exception is being swallowed
- Look for try-catch without logging
- This should NOT happen with current implementation

## Step 5: Verify Email Received

### Check Inbox
1. Check primary inbox
2. Check spam/junk folder
3. Check promotions tab (Gmail)
4. Wait 1-2 minutes for delivery

### Email Should Contain:
- Subject: "Reset Your CapFinLoan Password"
- From: CapFinLoan
- Blue header with CapFinLoan branding
- "Reset Password" button
- Clickable link as fallback
- Security warnings

## Step 6: Test Complete Flow

1. **Request Reset**
   ```bash
   curl -X POST http://localhost:7000/api/auth/forgot-password \
     -H "Content-Type: application/json" \
     -d '{"email":"YOUR_EMAIL@gmail.com"}'
   ```

2. **Check Console Logs**
   - Should see all [FORGOT PASSWORD] logs
   - Should see all [EMAIL] logs
   - Should see "✅✅✅ EMAIL SENT SUCCESSFULLY ✅✅✅"

3. **Check Email**
   - Open email
   - Click "Reset Password" button

4. **Reset Password**
   - Enter new password
   - Confirm password
   - Click "Reset Password"

5. **Login**
   - Use new password
   - Should login successfully

## Common Errors and Solutions

### Error: "Connection refused"
```bash
# Check if port is blocked
telnet smtp.gmail.com 587

# Try alternative port
# Update appsettings.json: "Port": 465
```

### Error: "Username and Password not accepted"
```bash
# Verify 2FA is enabled
# Generate new app password
# Remove all spaces from password
# Ensure username = sender email
```

### Error: "Sender address rejected"
```bash
# Ensure SenderEmail matches Username
# Verify Gmail account is active
# Check if account has sending limits
```

### Error: "Timeout"
```bash
# Check internet connection
# Check firewall settings
# Try different network
# Verify SMTP server is reachable
```

## Debugging Checklist

- [ ] Auth service started successfully
- [ ] Email configuration validated at startup
- [ ] No placeholder values in configuration
- [ ] Password shows "✅ SET" in logs
- [ ] Test endpoint returns success
- [ ] Console shows "✅✅✅ EMAIL SENT SUCCESSFULLY ✅✅✅"
- [ ] Email received in inbox (or spam)
- [ ] Reset link works
- [ ] Password reset successful
- [ ] Can login with new password

## Log Interpretation

### Success Pattern:
```
[FORGOT PASSWORD] Email requested: user@example.com
[FORGOT PASSWORD] User exists: True
[FORGOT PASSWORD] Generating password reset token...
[FORGOT PASSWORD] Token generated successfully
[FORGOT PASSWORD] Calling EmailService.SendPasswordResetEmailAsync...
[EMAIL] Starting email send process
[EMAIL] Connecting to SMTP server: smtp.gmail.com:587
[EMAIL] ✅ Connected to SMTP server successfully
[EMAIL] ✅ Authenticated successfully
[EMAIL] ✅ Email sent successfully
[EMAIL] ✅✅✅ EMAIL SENT SUCCESSFULLY ✅✅✅
[FORGOT PASSWORD] EmailService call completed successfully
```

### Failure Pattern:
```
[FORGOT PASSWORD] Email requested: user@example.com
[FORGOT PASSWORD] User exists: True
[FORGOT PASSWORD] Generating password reset token...
[FORGOT PASSWORD] Token generated successfully
[FORGOT PASSWORD] Calling EmailService.SendPasswordResetEmailAsync...
[EMAIL] Starting email send process
[EMAIL] Connecting to SMTP server: smtp.gmail.com:587
[EMAIL ERROR] ❌❌❌ EMAIL SENDING FAILED ❌❌❌
[EMAIL ERROR] Exception Type: AuthenticationException
[EMAIL ERROR] Message: 535-5.7.8 Username and Password not accepted
[RESET LINK FALLBACK]: http://localhost:5174/reset-password?...
[FORGOT PASSWORD ERROR] Exception caught:
```

## Fallback Testing

If email sending fails, you can still test the reset flow:

1. Look for this in console logs:
   ```
   [RESET LINK FALLBACK]:
   http://localhost:5174/reset-password?email=...&token=...
   ```

2. Copy the entire URL

3. Paste in browser

4. Complete password reset

5. Verify login works

## Next Steps After Success

1. ✅ Email sending works
2. ✅ Reset flow complete
3. 🔒 Secure credentials (use environment variables)
4. 📊 Monitor email delivery rates
5. 🚀 Consider professional email service for production

## Support

If still having issues:
1. Copy ALL console logs
2. Check exact error message
3. Verify Gmail App Password setup
4. Try test endpoint first
5. Check firewall/network settings
