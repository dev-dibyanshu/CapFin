#!/bin/bash

echo "=== NotificationService Email Configuration Setup ==="
echo ""
echo "This script will configure email settings using dotnet user-secrets."
echo "You'll need a Gmail account with an App Password."
echo ""
echo "To generate a Gmail App Password:"
echo "1. Go to https://myaccount.google.com/"
echo "2. Navigate to Security > 2-Step Verification"
echo "3. Scroll down to 'App passwords'"
echo "4. Generate a new app password for 'Mail'"
echo ""
read -p "Press Enter to continue or Ctrl+C to cancel..."
echo ""

# Initialize user-secrets
echo "Initializing user-secrets..."
dotnet user-secrets init

# Prompt for email settings
read -p "Enter your Gmail address: " email
read -p "Enter your Gmail App Password (16 characters): " password
read -p "Enter sender name (default: CapFinLoan Notifications): " sender_name
sender_name=${sender_name:-"CapFinLoan Notifications"}

# Set secrets
echo ""
echo "Configuring secrets..."
dotnet user-secrets set "EmailSettings:SmtpServer" "smtp.gmail.com"
dotnet user-secrets set "EmailSettings:Port" "587"
dotnet user-secrets set "EmailSettings:SenderName" "$sender_name"
dotnet user-secrets set "EmailSettings:SenderEmail" "$email"
dotnet user-secrets set "EmailSettings:Username" "$email"
dotnet user-secrets set "EmailSettings:Password" "$password"

echo ""
echo "✅ Email configuration completed!"
echo ""
echo "To view your secrets, run:"
echo "  dotnet user-secrets list"
echo ""
echo "To start the service, run:"
echo "  dotnet run"
echo ""
