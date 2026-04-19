#!/bin/bash

# Test Email Configuration Script
# This script helps verify your email settings are correct

echo "========================================="
echo "CapFinLoan - Email Configuration Test"
echo "========================================="
echo ""

# Check if appsettings.json exists
if [ ! -f "CapFinLoan.Auth.API/appsettings.json" ]; then
    echo "❌ Error: appsettings.json not found"
    exit 1
fi

echo "✅ appsettings.json found"
echo ""

# Check if EmailSettings section exists
if grep -q "EmailSettings" CapFinLoan.Auth.API/appsettings.json; then
    echo "✅ EmailSettings section found"
else
    echo "❌ Error: EmailSettings section not found in appsettings.json"
    exit 1
fi

echo ""
echo "Current Email Configuration:"
echo "----------------------------"

# Extract email settings (basic parsing)
SMTP_SERVER=$(grep -A 10 "EmailSettings" CapFinLoan.Auth.API/appsettings.json | grep "SmtpServer" | cut -d'"' -f4)
PORT=$(grep -A 10 "EmailSettings" CapFinLoan.Auth.API/appsettings.json | grep "Port" | grep -o '[0-9]*')
SENDER_EMAIL=$(grep -A 10 "EmailSettings" CapFinLoan.Auth.API/appsettings.json | grep "SenderEmail" | cut -d'"' -f4)
USERNAME=$(grep -A 10 "EmailSettings" CapFinLoan.Auth.API/appsettings.json | grep "Username" | cut -d'"' -f4)

echo "SMTP Server: $SMTP_SERVER"
echo "Port: $PORT"
echo "Sender Email: $SENDER_EMAIL"
echo "Username: $USERNAME"

echo ""
echo "Configuration Checklist:"
echo "------------------------"

# Check if placeholder values are still present
if [[ "$SENDER_EMAIL" == *"your_email"* ]]; then
    echo "❌ SenderEmail still has placeholder value"
    echo "   Update with your actual email address"
else
    echo "✅ SenderEmail configured"
fi

if grep -q "your_app_password" CapFinLoan.Auth.API/appsettings.json; then
    echo "❌ Password still has placeholder value"
    echo "   Update with your Gmail App Password"
else
    echo "✅ Password configured"
fi

if [[ "$SMTP_SERVER" == "smtp.gmail.com" ]]; then
    echo "✅ Using Gmail SMTP"
    echo ""
    echo "Gmail Setup Reminder:"
    echo "1. Enable 2-Factor Authentication"
    echo "2. Generate App Password: https://myaccount.google.com/apppasswords"
    echo "3. Use the 16-character app password (not your Gmail password)"
fi

echo ""
echo "========================================="
echo "Next Steps:"
echo "========================================="
echo "1. Update email settings in appsettings.json"
echo "2. Start Auth service: cd CapFinLoan.Auth.API && dotnet run"
echo "3. Test forgot password from frontend: http://localhost:5174/login"
echo "4. Check console logs for email sending status"
echo ""
echo "For detailed setup instructions, see:"
echo "  - EMAIL_SETUP_GUIDE.md"
echo "  - PASSWORD_RESET_COMPLETE_GUIDE.md"
echo ""
