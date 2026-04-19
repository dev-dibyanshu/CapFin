#!/bin/bash

# Password Reset Flow Test Script
# This script tests the complete password reset flow

echo "=========================================="
echo "Password Reset Flow Test"
echo "=========================================="
echo ""

# Check if email is provided
if [ -z "$1" ]; then
    echo "Usage: ./test-reset-flow.sh <email>"
    echo "Example: ./test-reset-flow.sh user@example.com"
    exit 1
fi

EMAIL=$1
API_URL="http://localhost:7001/api/auth"

echo "Testing with email: $EMAIL"
echo ""

# Step 1: Request password reset
echo "Step 1: Requesting password reset..."
echo "----------------------------------------"

RESPONSE=$(curl -s -X POST "$API_URL/forgot-password" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\"}")

echo "Response: $RESPONSE"
echo ""

# Check if request was successful
if echo "$RESPONSE" | grep -q "reset link has been sent"; then
    echo "✅ Password reset request successful"
    echo ""
    echo "Next steps:"
    echo "1. Check backend console logs for:"
    echo "   - [FORGOT PASSWORD] Token URL-encoded successfully"
    echo "   - [RESET LINK]: http://localhost:5174/reset-password?email=...&token=..."
    echo "   - [EMAIL] ✅✅✅ EMAIL SENT SUCCESSFULLY ✅✅✅"
    echo ""
    echo "2. Check your email inbox for reset link"
    echo ""
    echo "3. Click the reset link or copy the token from console logs"
    echo ""
    echo "4. To test reset with token, run:"
    echo "   curl -X POST $API_URL/reset-password \\"
    echo "     -H \"Content-Type: application/json\" \\"
    echo "     -d '{\"email\":\"$EMAIL\",\"token\":\"PASTE_TOKEN_HERE\",\"newPassword\":\"NewPassword123!\"}'"
    echo ""
else
    echo "❌ Password reset request failed"
    echo "Response: $RESPONSE"
    exit 1
fi

echo "=========================================="
echo "Check backend console for detailed logs:"
echo "- Token generation"
echo "- Token encoding"
echo "- Email sending"
echo "=========================================="
