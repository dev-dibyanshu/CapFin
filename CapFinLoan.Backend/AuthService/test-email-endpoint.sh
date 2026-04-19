#!/bin/bash

# Test Email Endpoint Script
# This script tests the email sending functionality

echo "========================================="
echo "CapFinLoan - Email Sending Test"
echo "========================================="
echo ""

# Check if email parameter is provided
if [ -z "$1" ]; then
    echo "Usage: ./test-email-endpoint.sh YOUR_EMAIL@gmail.com"
    echo ""
    echo "Example:"
    echo "  ./test-email-endpoint.sh john@gmail.com"
    echo ""
    exit 1
fi

EMAIL=$1

echo "Testing email sending to: $EMAIL"
echo ""
echo "Calling test endpoint..."
echo "URL: http://localhost:7001/api/auth/test-email?email=$EMAIL"
echo ""

# Make the request
RESPONSE=$(curl -s -w "\n%{http_code}" "http://localhost:7001/api/auth/test-email?email=$EMAIL")

# Extract HTTP status code (last line)
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

# Extract response body (everything except last line)
BODY=$(echo "$RESPONSE" | sed '$d')

echo "========================================="
echo "Response:"
echo "========================================="
echo "$BODY" | python3 -m json.tool 2>/dev/null || echo "$BODY"
echo ""
echo "HTTP Status Code: $HTTP_CODE"
echo "========================================="

if [ "$HTTP_CODE" = "200" ]; then
    echo "✅ SUCCESS: Email sent successfully!"
    echo ""
    echo "Next steps:"
    echo "1. Check your email inbox: $EMAIL"
    echo "2. Check spam/junk folder if not in inbox"
    echo "3. Look for email from CapFinLoan"
    echo "4. Subject: 'Reset Your CapFinLoan Password'"
else
    echo "❌ FAILED: Email sending failed"
    echo ""
    echo "Troubleshooting:"
    echo "1. Check Auth service console logs for detailed error"
    echo "2. Verify email configuration in appsettings.json"
    echo "3. Ensure Gmail App Password is configured"
    echo "4. Check EMAIL_DEBUG_GUIDE.md for solutions"
fi

echo ""
echo "For detailed logs, check Auth service console output"
echo ""
