# Password Reset - Quick Debug Checklist

## 🚀 Quick Start

```bash
# 1. Start Auth Service
cd CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API
dotnet run

# 2. Start Frontend
cd CapFinLoan.Frontend
npm run dev

# 3. Test Flow
./test-reset-flow.sh user@example.com
```

## ✅ Success Indicators

### Backend Console
```
✅ [FORGOT PASSWORD] Token URL-encoded successfully (length: 344)
✅ [EMAIL] ✅✅✅ EMAIL SENT SUCCESSFULLY ✅✅✅
✅ [RESET PASSWORD] ✅ Token decoded successfully
✅ [USER REPOSITORY] ✅✅✅ RESET PASSWORD SUCCEEDED ✅✅✅
```

### Browser Console
```
✅ [FRONTEND] Token length: 344
✅ [FRONTEND] ✅ Password reset successful!
```

## 🔍 Token Verification

### Step 1: Check Token Generation
```
[FORGOT PASSWORD] Raw token (first 50 chars): CfDJ8...
[FORGOT PASSWORD] Encoded token (first 50 chars): Q2ZESjg...
```
- Raw token: ~256 chars
- Encoded token: ~344 chars
- Encoded uses only: A-Z, a-z, 0-9, -, _

### Step 2: Check Token in URL
```
[FRONTEND] Token (first 50 chars): Q2ZESjg...
```
- Should match encoded token from backend
- No %20, %2B, %2F, %3D in token

### Step 3: Check Token Decoding
```
[RESET PASSWORD] Decoded token (first 50 chars): CfDJ8...
```
- Should match raw token from backend

## ❌ Common Errors

### Error: "InvalidToken"
**Cause:** Token expired, used, or corrupted
**Fix:** Request new password reset

### Error: "PasswordTooShort"
**Cause:** Password < 6 characters
**Fix:** Use longer password

### Error: "PasswordRequiresNonAlphanumeric"
**Cause:** Missing special character
**Fix:** Add !, @, #, $, etc.

### Error: "PasswordRequiresDigit"
**Cause:** Missing number
**Fix:** Add 0-9

### Error: "PasswordRequiresUpper"
**Cause:** Missing uppercase letter
**Fix:** Add A-Z

### Error: "PasswordRequiresLower"
**Cause:** Missing lowercase letter
**Fix:** Add a-z

## 🔧 Quick Fixes

### Token Mismatch
```bash
# Compare these in logs:
[FORGOT PASSWORD] Encoded token: Q2ZESjg...
[FRONTEND] Token: Q2ZESjg...
[RESET PASSWORD] Received token: Q2ZESjg...

# Should all match exactly!
```

### Email Not Sending
```bash
# Test email
./test-email-endpoint.sh your@gmail.com

# Check Gmail setup
./setup-gmail.sh
```

### Token Decoding Failed
```bash
# Check encoding method
grep "WebEncoders.Base64UrlEncode" AuthService.cs
grep "WebEncoders.Base64UrlDecode" AuthService.cs

# Should use WebEncoders, NOT Convert.ToBase64String
```

## 📋 Password Requirements

Default ASP.NET Identity requirements:
- ✅ Minimum 6 characters
- ✅ At least 1 uppercase (A-Z)
- ✅ At least 1 lowercase (a-z)
- ✅ At least 1 digit (0-9)
- ✅ At least 1 special character (!@#$%^&*)

**Example valid password:** `Password123!`

## 🧪 Test Commands

### Test Email
```bash
curl "http://localhost:7001/api/auth/test-email?email=your@gmail.com"
```

### Request Reset
```bash
curl -X POST http://localhost:7001/api/auth/forgot-password \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com"}'
```

### Reset Password
```bash
curl -X POST http://localhost:7001/api/auth/reset-password \
  -H "Content-Type: application/json" \
  -d '{
    "email":"user@example.com",
    "token":"PASTE_TOKEN_FROM_LOGS",
    "newPassword":"NewPassword123!"
  }'
```

## 📊 Log Locations

### Backend Logs
- Terminal running `dotnet run`
- Look for: `[FORGOT PASSWORD]`, `[RESET PASSWORD]`, `[USER REPOSITORY]`, `[RESET ERROR]`

### Frontend Logs
- Browser Console (F12)
- Look for: `[FRONTEND]`, `[RESET PASSWORD FRONTEND]`

## 🎯 Debugging Steps

1. **Request password reset**
   - Check: `[FORGOT PASSWORD]` logs
   - Verify: Token generated and encoded
   - Confirm: Email sent

2. **Click reset link**
   - Check: Browser console
   - Verify: Token extracted from URL
   - Confirm: Token matches backend

3. **Submit new password**
   - Check: `[RESET PASSWORD]` logs
   - Verify: Token decoded successfully
   - Check: `[USER REPOSITORY]` logs
   - Verify: No Identity errors

4. **If failed**
   - Look for: `[RESET ERROR]` logs
   - Note: Error code and description
   - Apply: Appropriate fix from table above

## 📚 Documentation

- `PASSWORD_RESET_DEBUG_ENHANCED.md` - Full debugging guide
- `ENHANCED_LOGGING_SUMMARY.md` - Implementation details
- `TOKEN_ENCODING_FIX.md` - Token encoding explanation
- `PASSWORD_RESET_FINAL_STATUS.md` - Complete status

## 🆘 Still Not Working?

1. **Check all services running**
   ```bash
   # Auth service on 7001
   curl http://localhost:7001/api/auth/health
   
   # Frontend on 5174
   curl http://localhost:5174
   ```

2. **Restart everything**
   ```bash
   # Stop all services (Ctrl+C)
   # Clear any cached data
   # Start Auth service
   # Start Frontend
   # Try again
   ```

3. **Check configuration**
   ```bash
   # Verify Gmail setup
   cat CapFinLoan.Auth.API/appsettings.Development.json
   
   # Should have EmailSettings with valid credentials
   ```

4. **Review complete logs**
   - Copy entire backend console output
   - Copy entire browser console output
   - Compare token values at each step
   - Identify exact mismatch point

## 💡 Pro Tips

- Token expires in 15 minutes - complete reset quickly
- Tokens are single-use - can't reuse old links
- Always check browser console for frontend issues
- Compare token first 50 chars to verify match
- Identity errors are now exposed in API response
- Use test scripts for quick verification

## ✨ Success Flow

```
1. Request reset → ✅ Token generated
2. Email sent → ✅ Link received
3. Click link → ✅ Token extracted
4. Submit password → ✅ Token decoded
5. Validate → ✅ Password reset
6. Login → ✅ Success!
```

---

**Last Updated:** 2026-04-15
**Status:** Enhanced logging implemented
**Next:** Test complete flow and verify logs
