# Authentication Flows - Frontend Implementation

## Overview
Complete authentication system with Login, Signup, Forgot Password, and Reset Password flows.

## Features Implemented

### 1. Login Flow
- **Route**: `/login`
- **API Endpoint**: `POST /auth/login`
- **Fields**: Email, Password
- **Success**: Stores token and user data in localStorage, redirects to dashboard or admin
- **Error**: Displays error message

### 2. Signup Flow
- **Route**: `/login` (toggle to signup mode)
- **API Endpoint**: `POST /auth/signup`
- **Fields**: Name, Email, Phone, Password
- **Success**: Shows success message, switches to login mode
- **Error**: Displays error message

### 3. Forgot Password Flow
- **Route**: `/login` (toggle to forgot mode)
- **API Endpoint**: `POST /auth/forgot-password`
- **Fields**: Email
- **Success**: Shows confirmation message
- **Error**: Displays error message

### 4. Reset Password Flow
- **Route**: `/reset-password?email=xxx&token=xxx`
- **API Endpoint**: `POST /auth/reset-password`
- **Fields**: New Password, Confirm Password
- **Query Params**: email, token (from reset link)
- **Success**: Shows success message, redirects to login after 2 seconds
- **Error**: Displays error message

## UI Features

### Mode Toggle
- Clean toggle between Sign In and Sign Up modes
- Forgot Password accessible from Sign In mode
- Back to Sign In button in Forgot Password mode

### Form Validation
- Required field validation
- Password match validation (Reset Password)
- Minimum password length (6 characters)
- Email format validation (HTML5)

### User Feedback
- Loading states on all buttons
- Success messages (green)
- Error messages (red)
- Disabled states when processing

### Icons
- Mail icon for email fields
- Lock icon for password fields
- User icon for name field
- Phone icon for phone field

## API Integration

### Base URL
```javascript
http://localhost:7000
```

### Endpoints Used
1. `POST /auth/login` - User login
2. `POST /auth/signup` - User registration
3. `POST /auth/forgot-password` - Request password reset
4. `POST /auth/reset-password` - Reset password with token

### Request/Response Format

#### Login
```json
// Request
{
  "email": "user@example.com",
  "password": "password123"
}

// Response
{
  "token": "jwt-token",
  "name": "John Doe",
  "email": "user@example.com",
  "role": "USER" // or "ADMIN"
}
```

#### Signup
```json
// Request
{
  "name": "John Doe",
  "email": "user@example.com",
  "password": "password123",
  "phone": "+1234567890"
}

// Response
{
  "message": "User registered successfully"
}
```

#### Forgot Password
```json
// Request
{
  "email": "user@example.com"
}

// Response
{
  "message": "Password reset email sent"
}
```

#### Reset Password
```json
// Request
{
  "email": "user@example.com",
  "token": "reset-token",
  "newPassword": "newpassword123"
}

// Response
{
  "message": "Password reset successfully"
}
```

## Testing

### Test Login
1. Navigate to `http://localhost:5174/login`
2. Enter email and password
3. Click "Sign In"
4. Should redirect to dashboard

### Test Signup
1. Navigate to `http://localhost:5174/login`
2. Click "Sign Up" toggle
3. Fill in all fields (Name, Email, Phone, Password)
4. Click "Create Account"
5. Should show success message and switch to login mode

### Test Forgot Password
1. Navigate to `http://localhost:5174/login`
2. Click "Forgot your password?"
3. Enter email
4. Click "Send Reset Link"
5. Should show confirmation message

### Test Reset Password
1. Get reset link from email (or backend console logs)
2. Navigate to reset link: `http://localhost:5174/reset-password?email=xxx&token=xxx`
3. Enter new password and confirm
4. Click "Reset Password"
5. Should show success and redirect to login

## Files Modified/Created

### Created
- `src/pages/ResetPassword.jsx` - Reset password page

### Modified
- `src/pages/Login.jsx` - Enhanced with signup and forgot password
- `src/App.jsx` - Added reset password route

### Unchanged
- `src/api/axios.js` - Already configured correctly
- Backend APIs - No changes needed

## Notes

- All forms include proper loading states
- Error handling for all API calls
- Success messages with appropriate styling
- Responsive design for mobile devices
- Clean UI with Tailwind CSS
- Icons from lucide-react
