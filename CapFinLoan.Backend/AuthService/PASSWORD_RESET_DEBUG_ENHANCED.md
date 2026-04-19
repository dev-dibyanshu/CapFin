# Password Reset - Enhanced Debugging Guide

## Overview

This guide provides comprehensive logging and debugging for the password reset flow to identify EXACT failure points.

## Enhanced Logging Added

### 1. Backend - ForgotPasswordAsync (AuthService.cs)
```
[FORGOT PASSWORD] Email requested: {email}
[FORGOT PASSWORD] User exists: {true/false}
[FORGOT PASSWORD] Raw token generated successfully (length: X)
[FORGOT PASSWORD] Raw token (first 50 chars): ...
[FORGOT PASSWORD] Token URL-encoded successfully (length: X)
[FORGOT PASSWORD] Encoded token (first 50 chars): ...
[RESET LINK]: http://localhost:5174/reset-password?email=...&token=...
```

### 2. Backend - ResetPasswordAsync (AuthService.cs)
```
[RESET PASSWORD] Email: {email}
[RESET PASSWORD] Received token length: X
[RESET PASSWORD] Received token (first 50 chars): ...
[RESET PASSWORD] ✅ User found: {userId}
[RESET PASSWORD] Decoding URL-encoded token...
[RESET PASSWORD] ✅ Token decoded successfully
[RESET PASSWORD] Decoded token length: X
[RESET PASSWORD] Decoded token (first 50 chars): ...
```

### 3. Backend - UserRepository.ResetPasswordAsync
```
[USER REPOSITORY] Calling UserManager.ResetPasswordAsync...
[USER REPOSITORY] User ID: {userId}
[USER REPOSITORY] User Email: {email}
[USER REPOSITORY] Token length: X
[USER REPOSITORY] Token (first 50 chars): ...
[USER REPOSITORY] New password length: X
[USER REPOSITORY] Reset result: ✅ SUCCESS / ❌ FAILED
```

### 4. Backend - Identity Errors (if failed)
```
[USER REPOSITORY] ❌❌❌ RESET PASSWORD FAILED ❌❌❌
[USER REPOSITORY] Error count: X
[RESET ERROR] Code: InvalidToken
[RESET ERROR] Description: Invalid token.
```

### 5. Frontend - URL Parameter Extraction
```
[RESET PASSWORD FRONTEND] URL Parameters:
[FRONTEND] Email: user@example.com
[FRONTEND] Token length: 344
[FRONTEND] Token (first 50 chars): Q2ZESjg...
[FRONTEND] Full URL: http://localhost:5174/reset-password?email=...&token=...
```

### 6. Frontend - API Request
```
[FRONTEND] Sending reset password request...
[FRONTEND] Email: user@example.com
[FRONTEND] Token length: 344
[FRONTEND] Token (first 50 chars): Q2ZESjg...
[FRONTEND] New password length: 10
```

### 7. Frontend - Response
```
[FRONTEND] ✅ Password reset successful!
OR
[FRONTEND] ❌ Password reset failed
[FRONTEND ERROR]: Invalid or expired reset token
```

## Testing Flow

### Step 1: Request Password Reset

**Action:** Go to http://localhost:5174/login and click "Forgot your password?"

**Expected Backend Logs:**
```
========================================
[FORGOT PASSWORD] Email requested: user@example.com
[FORGOT PASSWORD] Timestamp: 2026-04-15 10:30:00
[FORGOT PASSWORD] User exists: True
[FORGOT PASSWORD] User ID: {guid}
[FORGOT PASSWORD] User Name: John Doe
[FORGOT PASSWORD] Generating password reset token...
[FORGOT PASSWORD] Raw token generated successfully (length: 256)
[FORGOT PASSWORD] Raw token (first 50 chars): CfDJ8...
[FORGOT PASSWORD] Token URL-encoded successfully (length: 344)
[FORGOT PASSWORD] Encoded token (first 50 chars): Q2ZESjg...
[FORGOT PASSWORD] Reset link generated:
[RESET LINK]: http://localhost:5174/reset-password?email=user@example.com&token=Q2ZESjg...
[EMAIL] ✅✅✅ EMAIL SENT SUCCESSFULLY ✅✅✅
========================================
```

**What to Check:**
- ✅ User exists: True
- ✅ Raw token length: ~256 characters
- ✅ Encoded token length: ~344 characters
- ✅ Encoded token uses URL-safe characters (no +, /, =)
- ✅ Email sent successfully

### Step 2: Click Reset Link

**Action:** Click the reset link in the email

**Expected Frontend Logs (Browser Console):**
```
========================================
[RESET PASSWORD FRONTEND] URL Parameters:
[FRONTEND] Email: user@example.com
[FRONTEND] Token length: 344
[FRONTEND] Token (first 50 chars): Q2ZESjg...
[FRONTEND] Full URL: http://localhost:5174/reset-password?email=user@example.com&token=Q2ZESjg...
========================================
```

**What to Check:**
- ✅ Token length matches encoded token from backend (~344)
- ✅ Token first 50 chars match encoded token from backend
- ✅ No URL encoding artifacts (no %20, %2B, etc.)

### Step 3: Submit New Password

**Action:** Enter new password and click "Reset Password"

**Expected Frontend Logs:**
```
========================================
[FRONTEND] Sending reset password request...
[FRONTEND] Email: user@example.com
[FRONTEND] Token length: 344
[FRONTEND] Token (first 50 chars): Q2ZESjg...
[FRONTEND] New password length: 10
========================================
```

**Expected Backend Logs:**
```
========================================
[RESET PASSWORD] Email: user@example.com
[RESET PASSWORD] Timestamp: 2026-04-15 10:32:00
[RESET PASSWORD] Received token length: 344
[RESET PASSWORD] Received token (first 50 chars): Q2ZESjg...
[RESET PASSWORD] New password length: 10
[RESET PASSWORD] ✅ User found: {guid}
[RESET PASSWORD] User Name: John Doe
----------------------------------------
[RESET PASSWORD] Decoding URL-encoded token...
[RESET PASSWORD] ✅ Token decoded successfully
[RESET PASSWORD] Decoded token length: 256
[RESET PASSWORD] Decoded token (first 50 chars): CfDJ8...
----------------------------------------
[RESET PASSWORD] Calling UserRepository.ResetPasswordAsync...
========================================
[USER REPOSITORY] Calling UserManager.ResetPasswordAsync...
[USER REPOSITORY] User ID: {guid}
[USER REPOSITORY] User Email: user@example.com
[USER REPOSITORY] Token length: 256
[USER REPOSITORY] Token (first 50 chars): CfDJ8...
[USER REPOSITORY] New password length: 10
----------------------------------------
[USER REPOSITORY] Reset result: ✅ SUCCESS
[USER REPOSITORY] ✅✅✅ RESET PASSWORD SUCCEEDED ✅✅✅
========================================
[RESET PASSWORD] ✅✅✅ PASSWORD RESET SUCCESSFUL ✅✅✅
========================================
```

**What to Check:**
- ✅ Received token matches sent token (compare first 50 chars)
- ✅ Token decodes successfully
- ✅ Decoded token length matches raw token (~256)
- ✅ Decoded token first 50 chars match raw token
- ✅ UserManager.ResetPasswordAsync succeeds

## Common Failure Scenarios

### Scenario 1: Token Encoding Mismatch

**Symptoms:**
```
[RESET ERROR] Code: InvalidToken
[RESET ERROR] Description: Invalid token.
```

**Diagnosis:**
Compare these values:
1. `[FORGOT PASSWORD] Raw token (first 50 chars): CfDJ8...`
2. `[RESET PASSWORD] Decoded token (first 50 chars): CfDJ8...`

**If they DON'T match:**
- Token encoding/decoding is broken
- Check WebEncoders.Base64UrlEncode/Decode usage

**If they DO match:**
- Token may be expired (>15 minutes)
- Token may have been used already
- User may have changed password since token was generated

### Scenario 2: Token Corrupted in URL

**Symptoms:**
```
[FRONTEND] Token (first 50 chars): Q2ZESjg...
[RESET PASSWORD] Received token (first 50 chars): Q2ZESjg%2B...
```

**Diagnosis:**
Token contains URL-encoded characters (%2B, %2F, %3D)

**Fix:**
- Ensure using WebEncoders.Base64UrlEncode (not Convert.ToBase64String)
- Frontend should NOT call decodeURIComponent on token

### Scenario 3: Token Decoding Fails

**Symptoms:**
```
[RESET PASSWORD] ❌ Failed to decode token: Invalid length for a Base-64 char array
```

**Diagnosis:**
Token is not valid Base64URL format

**Fix:**
- Check token extraction in frontend
- Ensure token is passed exactly as received
- No trimming, no modification

### Scenario 4: Password Requirements Not Met

**Symptoms:**
```
[RESET ERROR] Code: PasswordTooShort
[RESET ERROR] Description: Passwords must be at least 6 characters.
```

**Diagnosis:**
New password doesn't meet ASP.NET Identity requirements

**Fix:**
- Minimum 6 characters (default)
- May require uppercase, lowercase, digit, special character
- Check Identity configuration in Program.cs

### Scenario 5: Token Expired

**Symptoms:**
```
[RESET ERROR] Code: InvalidToken
[RESET ERROR] Description: Invalid token.
```

**Diagnosis:**
More than 15 minutes passed since token generation

**Fix:**
- Request new password reset
- Complete reset within 15 minutes
- Consider extending token lifetime in Identity options

### Scenario 6: Token Already Used

**Symptoms:**
```
[RESET ERROR] Code: InvalidToken
[RESET ERROR] Description: Invalid token.
```

**Diagnosis:**
Token was already used to reset password

**Fix:**
- Tokens are single-use only
- Request new password reset
- Don't reuse old reset links

## Verification Checklist

### Token Generation (Backend)
- [ ] Raw token generated (length ~256)
- [ ] Token URL-encoded (length ~344)
- [ ] Encoded token uses only: A-Z, a-z, 0-9, -, _
- [ ] Reset link contains encoded token
- [ ] Email sent successfully

### Token Transmission (Email → Frontend)
- [ ] Email received
- [ ] Reset link clickable
- [ ] URL contains email and token parameters
- [ ] Token in URL matches encoded token from backend

### Token Extraction (Frontend)
- [ ] Token extracted from URL
- [ ] Token length matches (~344)
- [ ] Token first 50 chars match
- [ ] No URL encoding artifacts

### Token Decoding (Backend)
- [ ] Token received matches sent token
- [ ] Token decodes successfully
- [ ] Decoded token length matches raw token (~256)
- [ ] Decoded token first 50 chars match raw token

### Password Reset (Backend)
- [ ] User found
- [ ] UserManager.ResetPasswordAsync called
- [ ] No Identity errors
- [ ] Password reset succeeds

### Success Confirmation (Frontend)
- [ ] Success message displayed
- [ ] Redirect to login
- [ ] Can login with new password

## Quick Debug Commands

### Check Backend Logs
```bash
# Watch Auth service logs
cd CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API
dotnet run | grep -E "\[FORGOT PASSWORD\]|\[RESET PASSWORD\]|\[USER REPOSITORY\]|\[RESET ERROR\]"
```

### Check Frontend Logs
Open browser console (F12) and filter by:
- `[FRONTEND]`
- `[RESET PASSWORD FRONTEND]`

### Test Email Sending
```bash
cd CapFinLoan.Backend/AuthService
./test-email-endpoint.sh your@gmail.com
```

### Manual Token Test
```bash
# Request reset
curl -X POST http://localhost:7001/api/auth/forgot-password \
  -H "Content-Type: application/json" \
  -d '{"email":"your@email.com"}'

# Copy token from console logs
# Then test reset
curl -X POST http://localhost:7001/api/auth/reset-password \
  -H "Content-Type: application/json" \
  -d '{
    "email":"your@email.com",
    "token":"PASTE_TOKEN_HERE",
    "newPassword":"NewPassword123!"
  }'
```

## Success Indicators

When everything works correctly:

### Backend Console
```
✅ [FORGOT PASSWORD] Token URL-encoded successfully
✅ [EMAIL] ✅✅✅ EMAIL SENT SUCCESSFULLY ✅✅✅
✅ [RESET PASSWORD] ✅ Token decoded successfully
✅ [USER REPOSITORY] ✅✅✅ RESET PASSWORD SUCCEEDED ✅✅✅
✅ [RESET PASSWORD] ✅✅✅ PASSWORD RESET SUCCESSFUL ✅✅✅
```

### Frontend Console
```
✅ [FRONTEND] Token length: 344
✅ [FRONTEND] ✅ Password reset successful!
```

### User Experience
```
✅ Email received
✅ Reset link works
✅ Password reset succeeds
✅ Login with new password works
```

## Error Message Reference

| Error Code | Description | Cause | Solution |
|------------|-------------|-------|----------|
| InvalidToken | Invalid token | Token expired, used, or corrupted | Request new reset |
| PasswordTooShort | Password too short | Less than 6 characters | Use longer password |
| PasswordRequiresNonAlphanumeric | Missing special char | No special character | Add !, @, #, etc. |
| PasswordRequiresDigit | Missing digit | No number | Add 0-9 |
| PasswordRequiresUpper | Missing uppercase | No uppercase letter | Add A-Z |
| PasswordRequiresLower | Missing lowercase | No lowercase letter | Add a-z |

## Next Steps

1. **Start Auth Service**
   ```bash
   cd CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API
   dotnet run
   ```

2. **Start Frontend**
   ```bash
   cd CapFinLoan.Frontend
   npm run dev
   ```

3. **Test Complete Flow**
   - Request password reset
   - Check all logs match expected output
   - Identify exact failure point if any
   - Fix based on error messages

4. **Report Results**
   - Copy relevant log sections
   - Note where flow breaks
   - Include error messages
   - Share for further debugging

## Summary

With this enhanced logging:
- ✅ Every step is logged
- ✅ Token values are tracked end-to-end
- ✅ Exact error messages are captured
- ✅ No more guessing
- ✅ Precise diagnosis possible

The logs will show EXACTLY where and why the reset is failing.
