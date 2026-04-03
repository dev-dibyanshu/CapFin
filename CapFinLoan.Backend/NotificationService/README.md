# NotificationService - Email Setup Guide

## Overview
NotificationService consumes RabbitMQ events and sends real emails using MailKit with Gmail SMTP.

## Configuration

### Option 1: Using appsettings.json (Development)
Edit `appsettings.json`:
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "CapFinLoan Notifications",
    "SenderEmail": "your-email@gmail.com",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

### Option 2: Using Environment Variables (Production)
Set these environment variables:
```bash
export EmailSettings__SmtpServer="smtp.gmail.com"
export EmailSettings__Port="587"
export EmailSettings__SenderName="CapFinLoan Notifications"
export EmailSettings__SenderEmail="your-email@gmail.com"
export EmailSettings__Username="your-email@gmail.com"
export EmailSettings__Password="your-app-password"
```

### Option 3: Using dotnet user-secrets (Recommended for Development)
```bash
cd CapFinLoan.Backend/NotificationService
dotnet user-secrets init
dotnet user-secrets set "EmailSettings:SmtpServer" "smtp.gmail.com"
dotnet user-secrets set "EmailSettings:Port" "587"
dotnet user-secrets set "EmailSettings:SenderName" "CapFinLoan Notifications"
dotnet user-secrets set "EmailSettings:SenderEmail" "your-email@gmail.com"
dotnet user-secrets set "EmailSettings:Username" "your-email@gmail.com"
dotnet user-secrets set "EmailSettings:Password" "your-app-password"
```

## Gmail Setup

### Generate App Password
1. Go to Google Account settings: https://myaccount.google.com/
2. Navigate to Security
3. Enable 2-Step Verification (if not already enabled)
4. Go to "App passwords"
5. Generate a new app password for "Mail"
6. Copy the 16-character password
7. Use this password in the configuration (NOT your regular Gmail password)

## Features

### Email Sending
- Uses MailKit with Gmail SMTP
- HTML email templates
- Secure connection (StartTls)

### Retry Logic
- 3 retry attempts on failure
- 2-second delay between retries
- Logs each attempt

### RabbitMQ Acknowledgment
- Manual acknowledgment mode
- BasicAck on success
- BasicNack with requeue on failure

### Error Handling
- Try-catch around email sending
- Detailed logging
- Graceful failure handling

## Running the Service

```bash
cd CapFinLoan.Backend/NotificationService
dotnet run
```

## Testing

1. Start RabbitMQ
2. Start NotificationService
3. Submit a loan application via ApplicationService
4. Check console output for email sending logs
5. Check recipient inbox for email

## Troubleshooting

### "Authentication failed" error
- Verify you're using an App Password, not your regular Gmail password
- Ensure 2-Step Verification is enabled on your Google account

### "Connection refused" error
- Check if RabbitMQ is running on localhost:5672
- Verify firewall settings

### Email not received
- Check spam folder
- Verify recipient email address
- Check Gmail sending limits (500 emails/day for free accounts)
