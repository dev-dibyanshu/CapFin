# Password Reset - Quick Reference

## API Endpoints

### 1. Forgot Password
```http
POST /api/auth/forgot-password
Content-Type: application/json

{
  "email": "user@example.com"
}
```

**Response:**
```json
{
  "message": "If the email exists, a reset link has been sent."
}
```

**Console Output:**
```
========================================
PASSWORD RESET REQUEST
Email: user@example.com
Reset Link: http://localhost:5174/reset-password?email=user%40example.com&token=Q2ZESjhLV...
========================================
```

### 2. Reset Password
```http
POST /api/auth/reset-password
Content-Type: application/json

{
  "email": "user@example.com",
  "token": "Q2ZESjhLV...",
  "newPassword": "NewPassword123!"
}
```

**Success Response:**
```json
{
  "message": "Password reset successful."
}
```

**Error Response:**
```json
{
  "message": "Invalid or expired reset token."
}
```

## Testing with curl

### Step 1: Request Reset
```bash
curl -X POST http://localhost:7001/api/auth/forgot-password \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com"}'
```

### Step 2: Copy Token from Console

### Step 3: Reset Password
```bash
curl -X POST http://localhost:7001/api/auth/reset-password \
  -H "Content-Type: application/json" \
  -d '{
    "email":"user@example.com",
    "token":"PASTE_TOKEN_HERE",
    "newPassword":"NewPassword123!"
  }'
```

### Step 4: Login with New Password
```bash
curl -X POST http://localhost:7001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email":"user@example.com",
    "password":"NewPassword123!"
  }'
```

## Via API Gateway

Replace `http://localhost:7001` with `http://localhost:7000` in all commands above.

## Key Features

✅ Uses ASP.NET Identity's built-in token generation  
✅ Tokens expire after 24 hours  
✅ Single-use tokens (invalidated after reset)  
✅ Prevents user enumeration  
✅ Base64 URL-safe encoding  
✅ Password complexity validation  
✅ Console logging for development  

## Files Created/Modified

**Created:**
- `ForgotPasswordRequest.cs`
- `ResetPasswordRequest.cs`
- `MessageResponse.cs`

**Modified:**
- `IUserRepository.cs` + `UserRepository.cs`
- `IAuthService.cs` + `AuthService.cs`
- `AuthController.cs`

## Security Notes

- Always returns success for forgot password (prevents email enumeration)
- Tokens are cryptographically secure
- Password policy enforced by ASP.NET Identity
- Generic error messages for security
- Detailed server-side logging only

## Next Steps

1. Start AuthService: `dotnet run`
2. Test endpoints with Postman or curl
3. Integrate with frontend reset password page
4. (Future) Replace console logging with email service
