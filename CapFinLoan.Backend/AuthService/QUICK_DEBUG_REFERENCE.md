# Quick Debug Reference - Email Issues

## 🚀 Quick Test

```bash
# Test email sending immediately
curl "http://localhost:7001/api/auth/test-email?email=YOUR_EMAIL@gmail.com"

# Or use script
./test-email-endpoint.sh YOUR_EMAIL@gmail.com
```

## 📋 Checklist

- [ ] Auth service running on port 7001
- [ ] Startup shows: `[CONFIG] Password: ✅ SET`
- [ ] No placeholder warnings
- [ ] Test endpoint returns success
- [ ] Console shows: `✅✅✅ EMAIL SENT SUCCESSFULLY ✅✅✅`
- [ ] Email received (check spam folder)

## 🔍 Identify Failure Point

### 1. Config Not Loading
**See:** `[CONFIG] Password: ❌ NOT SET`
**Fix:** Update appsettings.json, restart service

### 2. Connection Failed
**See:** `[EMAIL] Connecting...` (no ✅)
**Fix:** Check firewall, try port 465

### 3. Auth Failed
**See:** `✅ Connected` but `❌ Authenticated`
**Fix:** Use Gmail App Password, enable 2FA

### 4. Sending Failed
**See:** `✅ Authenticated` but `❌ Email sent`
**Fix:** Check Gmail limits, verify account

### 5. Service Not Called
**See:** No `[EMAIL]` logs
**Fix:** Check DI registration, restart service

## ⚙️ Configuration

### Gmail Setup (2 minutes)
1. Enable 2FA: https://myaccount.google.com/security
2. Generate App Password: https://myaccount.google.com/apppasswords
3. Update appsettings.json:
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderEmail": "YOUR_EMAIL@gmail.com",
    "Username": "YOUR_EMAIL@gmail.com",
    "Password": "YOUR_16_CHAR_APP_PASSWORD",
    "EnableSsl": true
  }
}
```
4. Restart Auth service

## 📊 Log Patterns

### ✅ Success
```
[EMAIL] ✅ Connected to SMTP server successfully
[EMAIL] ✅ Authenticated successfully
[EMAIL] ✅ Email sent successfully
[EMAIL] ✅✅✅ EMAIL SENT SUCCESSFULLY ✅✅✅
```

### ❌ Failure
```
[EMAIL ERROR] ❌❌❌ EMAIL SENDING FAILED ❌❌❌
[EMAIL ERROR] Exception Type: {error type}
[EMAIL ERROR] Message: {error message}
[RESET LINK FALLBACK]: {link}
```

## 🛠️ Common Fixes

| Error | Solution |
|-------|----------|
| Connection refused | Check firewall, try port 465 |
| Auth failed | Use App Password, not regular password |
| Timeout | Check internet connection |
| Sender rejected | Ensure SenderEmail = Username |
| No email | Check spam folder |

## 📞 Support

1. Check console logs for exact error
2. See EMAIL_DEBUG_GUIDE.md for detailed steps
3. Use fallback reset link from console
4. Test with endpoint first before frontend

## 🎯 Expected Flow

```
Request → Config Valid → User Found → Token Generated → 
Email Service Called → SMTP Connected → Authenticated → 
Email Sent → Success → Email Received
```

Any break in this chain will be clearly logged!
