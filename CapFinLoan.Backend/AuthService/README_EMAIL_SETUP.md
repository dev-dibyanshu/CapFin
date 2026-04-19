# Email Setup for Password Reset - Quick Start

## 🚀 Quick Setup (2 Minutes)

### Step 1: Run Setup Script
```bash
cd CapFinLoan.Backend/AuthService
./setup-gmail.sh
```

Follow the prompts to:
1. Enter your Gmail address
2. Enter your Gmail App Password (16 characters, no spaces)

### Step 2: Get Gmail App Password

**Before running the script, get your App Password:**

1. **Enable 2FA:** https://myaccount.google.com/security
2. **Generate App Password:** https://myaccount.google.com/apppasswords
   - App: Mail
   - Device: Other → "CapFinLoan"
   - Copy the 16-character password (remove spaces)

### Step 3: Test
```bash
./test-email-endpoint.sh YOUR_EMAIL@gmail.com
```

**Expected:** Email received in inbox ✅

## 📚 Documentation

| File | Purpose |
|------|---------|
| `GMAIL_SETUP_FIX.md` | Complete step-by-step setup guide |
| `GMAIL_AUTH_FIX_SUMMARY.md` | Technical summary |
| `EMAIL_DEBUG_GUIDE.md` | Debugging guide |
| `QUICK_DEBUG_REFERENCE.md` | Quick reference card |

## 🛠️ Scripts

| Script | Purpose |
|--------|---------|
| `setup-gmail.sh` | Interactive Gmail setup |
| `test-email-config.sh` | Validate configuration |
| `test-email-endpoint.sh` | Test email sending |

## ✅ Success Checklist

- [ ] 2FA enabled on Gmail
- [ ] App Password generated
- [ ] `./setup-gmail.sh` completed
- [ ] Auth service restarted
- [ ] Test email sent successfully
- [ ] Email received in inbox

## 🔧 Troubleshooting

**Auth Failed?**
- Use App Password (not regular password)
- Remove all spaces from password
- Ensure password is 16 characters

**No Email?**
- Check spam folder
- Check promotions tab
- Wait 1-2 minutes

**Still Issues?**
- See `GMAIL_SETUP_FIX.md` for detailed solutions
- Check console logs for exact error
- Generate new App Password

## 🎯 Test Endpoints

```bash
# Health check
curl http://localhost:7001/api/auth/health

# Test email
curl "http://localhost:7001/api/auth/test-email?email=YOUR_EMAIL@gmail.com"

# Forgot password (via frontend)
# Go to: http://localhost:5174/login
# Click "Forgot your password?"
```

## 📧 Expected Console Output

When working correctly:
```
[EMAIL] ✅ Connected to SMTP server successfully
[EMAIL] ✅ Authenticated successfully
[EMAIL] ✅ Email sent successfully
[EMAIL] ✅✅✅ EMAIL SENT SUCCESSFULLY ✅✅✅
```

## 🔒 Security

- `appsettings.Development.json` is in `.gitignore`
- Credentials are NOT tracked in git
- Safe to use real credentials locally
- Use environment variables in production

## 🚨 Common Mistakes

| ❌ Wrong | ✅ Correct |
|----------|-----------|
| Regular Gmail password | App Password |
| Password with spaces | Remove all spaces |
| 2FA not enabled | Enable 2FA first |
| Different email in Username | Must match SenderEmail |

## 📞 Need Help?

1. Check `GMAIL_SETUP_FIX.md` for detailed guide
2. Run `./test-email-config.sh` to validate config
3. Check console logs for exact error
4. See `EMAIL_DEBUG_GUIDE.md` for debugging

## ✨ That's It!

Once setup is complete, your forgot password flow will work perfectly with real email sending!
