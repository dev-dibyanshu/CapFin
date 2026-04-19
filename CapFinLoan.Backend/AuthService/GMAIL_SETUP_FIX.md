# Gmail SMTP Authentication Fix - Step by Step

## Problem
```
[EMAIL ERROR] Exception Type: AuthenticationException
[EMAIL ERROR] Message: 535-5.7.8 Username and Password not accepted
```

## Root Cause
- ❌ Using normal Gmail password (doesn't work with SMTP)
- ❌ Using invalid or expired App Password
- ❌ 2FA not enabled
- ❌ Spaces in App Password
- ❌ Username doesn't match sender email

## Solution (Follow Exactly)

### Step 1: Enable 2-Factor Authentication

1. **Go to Google Account Security**
   ```
   https://myaccount.google.com/security
   ```

2. **Find "2-Step Verification"**
   - Click on "2-Step Verification"
   - Follow the setup wizard
   - Verify your phone number
   - Complete setup

3. **Verify 2FA is Active**
   - You should see "2-Step Verification: On"

### Step 2: Generate App Password

1. **Go to App Passwords**
   ```
   https://myaccount.google.com/apppasswords
   ```
   
   **Note:** If you don't see this option:
   - Ensure 2FA is enabled (Step 1)
   - Wait 5 minutes after enabling 2FA
   - Refresh the page

2. **Select App**
   - Click "Select app" dropdown
   - Choose "Mail"

3. **Select Device**
   - Click "Select device" dropdown
   - Choose "Other (Custom name)"
   - Enter: `CapFinLoan Auth Service`

4. **Generate**
   - Click "Generate" button
   - You'll see a 16-character password like:
   ```
   abcd efgh ijkl mnop
   ```

5. **Copy Password CORRECTLY**
   - **REMOVE ALL SPACES**
   - Correct: `abcdefghijklmnop`
   - Wrong: `abcd efgh ijkl mnop`
   - Wrong: `abcd-efgh-ijkl-mnop`

### Step 3: Create Configuration File

**Create:** `CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API/appsettings.Development.json`

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "CapFinLoan",
    "SenderEmail": "YOUR_ACTUAL_EMAIL@gmail.com",
    "Username": "YOUR_ACTUAL_EMAIL@gmail.com",
    "Password": "abcdefghijklmnop",
    "EnableSsl": true
  }
}
```

**Replace:**
- `YOUR_ACTUAL_EMAIL@gmail.com` → Your Gmail address
- `abcdefghijklmnop` → Your 16-character App Password (no spaces)

**CRITICAL:**
- `SenderEmail` MUST equal `Username`
- Both must be the same Gmail account
- Password must be App Password (not regular password)
- No spaces in password

### Step 4: Verify Configuration

**Check your file looks like this:**

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "CapFinLoan",
    "SenderEmail": "john.doe@gmail.com",
    "Username": "john.doe@gmail.com",
    "Password": "abcdefghijklmnop",
    "EnableSsl": true
  }
}
```

**Common Mistakes to Avoid:**
- ❌ `"Password": "abcd efgh ijkl mnop"` (has spaces)
- ❌ `"Password": "MyGmailPassword123"` (regular password)
- ❌ `"SenderEmail": "sender@gmail.com"` but `"Username": "different@gmail.com"` (mismatch)
- ❌ Forgetting to remove placeholder values

### Step 5: Restart Auth Service

**Stop current service:**
```bash
# Press Ctrl+C in the terminal running Auth service
```

**Start service:**
```bash
cd CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API
dotnet run
```

**Check startup logs:**
```
========================================
[CONFIG] Email Settings Validation
========================================
[CONFIG] SMTP Server: smtp.gmail.com
[CONFIG] Port: 587
[CONFIG] Sender Email: john.doe@gmail.com
[CONFIG] Username: john.doe@gmail.com
[CONFIG] Password: ✅ SET (length: 16)
[CONFIG] Enable SSL: True
========================================
```

**Good signs:**
- ✅ Password shows "✅ SET (length: 16)"
- ✅ No placeholder warnings
- ✅ SenderEmail matches Username

**Bad signs:**
- ❌ "⚠️ WARNING: Email settings contain placeholder values"
- ❌ "Password: ❌ NOT SET"
- ❌ Password length is not 16

### Step 6: Test Email Sending

**Method 1: Use Test Endpoint**

```bash
curl "http://localhost:7001/api/auth/test-email?email=YOUR_EMAIL@gmail.com"
```

**Or use script:**
```bash
cd CapFinLoan.Backend/AuthService
./test-email-endpoint.sh YOUR_EMAIL@gmail.com
```

**Expected Console Output:**
```
========================================
[EMAIL] Starting email send process
[EMAIL] Configuration:
[EMAIL] SMTP Server: smtp.gmail.com
[EMAIL] Port: 587
[EMAIL] Username: john.doe@gmail.com
[EMAIL] Password: SET (length: 16)
----------------------------------------
[EMAIL] Connecting to SMTP server: smtp.gmail.com:587
[EMAIL] ✅ Connected to SMTP server successfully
[EMAIL] Authenticating with username: john.doe@gmail.com
[EMAIL] ✅ Authenticated successfully
[EMAIL] Sending email to YOUR_EMAIL@gmail.com...
[EMAIL] ✅ Email sent successfully
========================================
[EMAIL] ✅✅✅ EMAIL SENT SUCCESSFULLY ✅✅✅
========================================
```

**Expected API Response:**
```json
{
  "success": true,
  "message": "Test email sent successfully to YOUR_EMAIL@gmail.com",
  "timestamp": "2026-04-15T10:00:00Z"
}
```

**Method 2: Test via Frontend**

1. Go to: `http://localhost:5174/login`
2. Click "Forgot your password?"
3. Enter your email
4. Click "Send Reset Link"
5. Check console logs
6. Check email inbox

### Step 7: Verify Email Received

1. **Check Inbox**
   - Look for email from "CapFinLoan"
   - Subject: "Reset Your CapFinLoan Password"

2. **Check Spam Folder**
   - Gmail may filter first email to spam
   - Mark as "Not Spam" if found there

3. **Check Promotions Tab**
   - Gmail may categorize it as promotional

4. **Wait 1-2 Minutes**
   - Email delivery may take a moment

## Troubleshooting

### Issue 1: Still Getting "Username and Password not accepted"

**Solutions:**

1. **Generate NEW App Password**
   - Go to: https://myaccount.google.com/apppasswords
   - Delete old "CapFinLoan Auth Service" password
   - Generate new one
   - Copy without spaces
   - Update appsettings.Development.json
   - Restart service

2. **Verify 2FA is Enabled**
   - Go to: https://myaccount.google.com/security
   - Ensure "2-Step Verification: On"

3. **Check Username Matches Email**
   ```json
   "SenderEmail": "john@gmail.com",
   "Username": "john@gmail.com"  // Must match!
   ```

4. **Verify No Spaces in Password**
   ```json
   // Wrong
   "Password": "abcd efgh ijkl mnop"
   
   // Correct
   "Password": "abcdefghijklmnop"
   ```

### Issue 2: "Connection refused" or "Timeout"

**Solutions:**

1. **Check Firewall**
   ```bash
   # Test if port 587 is reachable
   telnet smtp.gmail.com 587
   ```

2. **Try Alternative Port**
   ```json
   "Port": 465  // Instead of 587
   ```

3. **Check Internet Connection**
   - Verify you can access Gmail in browser

### Issue 3: No Email Received

**Solutions:**

1. **Check Spam Folder**
   - First email often goes to spam

2. **Check Promotions Tab**
   - Gmail may categorize it

3. **Verify Email Address**
   - Ensure you're checking the correct inbox

4. **Use Fallback Link**
   - Check console logs for:
   ```
   [RESET LINK FALLBACK]:
   http://localhost:5174/reset-password?email=...&token=...
   ```
   - Copy and paste in browser

### Issue 4: "Sender address rejected"

**Solution:**
```json
// Ensure these match
"SenderEmail": "john@gmail.com",
"Username": "john@gmail.com"
```

## Verification Checklist

Before testing, verify:

- [ ] 2FA enabled on Gmail account
- [ ] App Password generated (not regular password)
- [ ] App Password copied without spaces
- [ ] appsettings.Development.json created
- [ ] SenderEmail matches Username
- [ ] Both are the same Gmail account
- [ ] Password is 16 characters
- [ ] Auth service restarted
- [ ] Startup logs show "✅ SET (length: 16)"
- [ ] No placeholder warnings

## Success Indicators

When everything is correct:

1. ✅ Startup shows password configured
2. ✅ Test endpoint returns `"success": true`
3. ✅ Console shows "✅✅✅ EMAIL SENT SUCCESSFULLY ✅✅✅"
4. ✅ Email received in inbox (or spam)
5. ✅ Reset link works
6. ✅ Password reset successful
7. ✅ Can login with new password

## Example Working Configuration

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "CapFinLoan",
    "SenderEmail": "capfinloan.demo@gmail.com",
    "Username": "capfinloan.demo@gmail.com",
    "Password": "abcdefghijklmnop",
    "EnableSsl": true
  }
}
```

**Key Points:**
- Real Gmail address (not placeholder)
- 16-character App Password (no spaces)
- SenderEmail = Username
- Port 587 with EnableSsl true

## Quick Commands

```bash
# Check configuration
cd CapFinLoan.Backend/AuthService
./test-email-config.sh

# Test email sending
./test-email-endpoint.sh YOUR_EMAIL@gmail.com

# Restart Auth service
cd CapFinLoan.Auth.API
dotnet run

# Test via API
curl "http://localhost:7001/api/auth/test-email?email=YOUR_EMAIL@gmail.com"
```

## Still Not Working?

1. **Copy ALL console logs**
2. **Check exact error message**
3. **Verify App Password is fresh** (generate new one)
4. **Try different Gmail account** (to rule out account issues)
5. **Check Gmail security settings** (ensure SMTP is not blocked)

## Alternative: Use Different Email Provider

If Gmail continues to fail, try:

### Outlook/Hotmail
```json
{
  "SmtpServer": "smtp-mail.outlook.com",
  "Port": 587,
  "Username": "your@outlook.com",
  "Password": "your_outlook_password",
  "EnableSsl": true
}
```

### SendGrid (Free tier: 100 emails/day)
```json
{
  "SmtpServer": "smtp.sendgrid.net",
  "Port": 587,
  "Username": "apikey",
  "Password": "your_sendgrid_api_key",
  "EnableSsl": true
}
```

## Final Notes

- App Passwords are safer than regular passwords
- They can be revoked without changing account password
- Generate new one if compromised
- Each app should have its own App Password
- Never commit credentials to git (use appsettings.Development.json)

## Success!

Once you see:
```
[EMAIL] ✅✅✅ EMAIL SENT SUCCESSFULLY ✅✅✅
```

Your forgot password flow is fully working! 🎉
