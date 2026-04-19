#!/bin/bash

# Gmail Setup Script for CapFinLoan Auth Service

echo "========================================="
echo "CapFinLoan - Gmail SMTP Setup"
echo "========================================="
echo ""

# Check if appsettings.Development.json exists
if [ -f "CapFinLoan.Auth.API/appsettings.Development.json" ]; then
    echo "⚠️  appsettings.Development.json already exists"
    echo ""
    read -p "Do you want to overwrite it? (y/n): " -n 1 -r
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Setup cancelled"
        exit 0
    fi
fi

echo ""
echo "📧 Gmail Setup Instructions:"
echo "========================================="
echo ""
echo "Step 1: Enable 2-Factor Authentication"
echo "  → Go to: https://myaccount.google.com/security"
echo "  → Enable '2-Step Verification'"
echo ""
echo "Step 2: Generate App Password"
echo "  → Go to: https://myaccount.google.com/apppasswords"
echo "  → App: Mail"
echo "  → Device: Other (Custom) → 'CapFinLoan'"
echo "  → Click Generate"
echo "  → Copy the 16-character password (REMOVE SPACES)"
echo ""
echo "========================================="
echo ""

# Prompt for email
read -p "Enter your Gmail address: " EMAIL

# Validate email format
if [[ ! $EMAIL =~ ^[a-zA-Z0-9._%+-]+@gmail\.com$ ]]; then
    echo "❌ Invalid Gmail address format"
    exit 1
fi

echo ""
echo "⚠️  IMPORTANT: Use App Password (not regular password)"
echo "   App Password is 16 characters without spaces"
echo ""

# Prompt for password (hidden input)
read -s -p "Enter your Gmail App Password (16 chars, no spaces): " PASSWORD
echo ""

# Validate password length
if [ ${#PASSWORD} -ne 16 ]; then
    echo "❌ App Password should be exactly 16 characters"
    echo "   You entered: ${#PASSWORD} characters"
    echo ""
    echo "Common mistakes:"
    echo "   - Including spaces (remove them)"
    echo "   - Using regular Gmail password (use App Password)"
    echo "   - Copying incorrectly"
    exit 1
fi

# Create appsettings.Development.json
cat > CapFinLoan.Auth.API/appsettings.Development.json << EOF
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "CapFinLoan",
    "SenderEmail": "$EMAIL",
    "Username": "$EMAIL",
    "Password": "$PASSWORD",
    "EnableSsl": true
  }
}
EOF

echo ""
echo "========================================="
echo "✅ Configuration file created successfully!"
echo "========================================="
echo ""
echo "File: CapFinLoan.Auth.API/appsettings.Development.json"
echo ""
echo "Configuration:"
echo "  SMTP Server: smtp.gmail.com"
echo "  Port: 587"
echo "  Email: $EMAIL"
echo "  Password: ✅ SET (length: ${#PASSWORD})"
echo ""
echo "========================================="
echo "Next Steps:"
echo "========================================="
echo ""
echo "1. Restart Auth Service:"
echo "   cd CapFinLoan.Auth.API"
echo "   dotnet run"
echo ""
echo "2. Test Email Sending:"
echo "   ./test-email-endpoint.sh $EMAIL"
echo ""
echo "3. Or test via API:"
echo "   curl \"http://localhost:7001/api/auth/test-email?email=$EMAIL\""
echo ""
echo "4. Check console logs for:"
echo "   [EMAIL] ✅✅✅ EMAIL SENT SUCCESSFULLY ✅✅✅"
echo ""
echo "========================================="
echo ""
echo "⚠️  SECURITY NOTE:"
echo "   appsettings.Development.json is NOT tracked in git"
echo "   Your credentials are safe"
echo ""
