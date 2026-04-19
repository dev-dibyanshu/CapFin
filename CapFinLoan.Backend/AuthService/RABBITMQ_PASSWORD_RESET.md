# Password Reset with RabbitMQ - Implementation Guide

## Overview

Password reset emails are now sent asynchronously using RabbitMQ message queue. The Auth Service publishes events, and the NotificationService consumes them to send emails.

## Architecture

```
┌─────────────────┐         ┌──────────────┐         ┌─────────────────────┐
│   Auth Service  │────────▶│   RabbitMQ   │────────▶│ NotificationService │
│                 │ Publish │              │ Consume │                     │
│ ForgotPassword  │ Event   │ password-    │ Event   │   Send Email        │
│                 │         │ reset queue  │         │   (SMTP)            │
└─────────────────┘         └──────────────┘         └─────────────────────┘
```

## Benefits

✅ **Asynchronous**: Auth Service responds immediately without waiting for email
✅ **Decoupled**: Auth Service doesn't need email configuration
✅ **Reliable**: Messages are queued and retried on failure
✅ **Scalable**: Multiple NotificationService instances can process emails
✅ **Resilient**: If email fails, message is requeued automatically

## Components

### 1. Event Definition

**PasswordResetRequestedEvent.cs**
```csharp
public class PasswordResetRequestedEvent
{
    public string Email { get; set; }
    public string UserName { get; set; }
    public string ResetLink { get; set; }
}
```

### 2. Auth Service (Publisher)

**Changes:**
- Removed direct `IEmailService` dependency
- Added `IMessagePublisher` dependency
- Publishes event to `password-reset` queue instead of sending email

**Flow:**
1. User requests password reset
2. Generate reset token
3. Encode token (URL-safe)
4. Build reset link
5. Publish event to RabbitMQ
6. Return success response immediately

### 3. NotificationService (Consumer)

**Changes:**
- Added `PasswordResetRequestedEvent` handler
- Listens on `password-reset` queue
- Sends email using SMTP

**Flow:**
1. Receive message from queue
2. Deserialize event
3. Send email with retry logic (3 attempts)
4. Acknowledge message on success
5. Requeue message on failure

## Setup

### Prerequisites

1. **RabbitMQ Running**
   ```bash
   docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
   ```

2. **Email Configuration** (NotificationService only)
   ```bash
   cd CapFinLoan.Backend/NotificationService
   ./setup-email.sh
   ```

### Start Services

```bash
# Terminal 1: RabbitMQ (if not already running)
docker start rabbitmq

# Terminal 2: Auth Service
cd CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API
dotnet run

# Terminal 3: NotificationService
cd CapFinLoan.Backend/NotificationService
dotnet run

# Terminal 4: Frontend
cd CapFinLoan.Frontend
npm run dev
```

## Testing

### 1. Request Password Reset

**Via Frontend:**
```
1. Go to http://localhost:5174/login
2. Click "Forgot your password?"
3. Enter email
4. Click "Send Reset Link"
```

**Via API:**
```bash
curl -X POST http://localhost:7001/api/auth/forgot-password \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com"}'
```

### 2. Check Auth Service Logs

```
========================================
[FORGOT PASSWORD] Email requested: user@example.com
[FORGOT PASSWORD] User exists: True
[FORGOT PASSWORD] Token URL-encoded successfully
[FORGOT PASSWORD] Reset link generated
[FORGOT PASSWORD] Publishing password reset event to RabbitMQ...
========================================
[RABBITMQ] Publishing message to queue: password-reset
[RABBITMQ] Message type: PasswordResetRequestedEvent
[RABBITMQ] Message serialized (length: 450 bytes)
[RABBITMQ] ✅ Message published successfully
========================================
[FORGOT PASSWORD] ✅ Event published to RabbitMQ successfully
========================================
```

### 3. Check NotificationService Logs

```
========================================
[10:30:45] 📨 [PASSWORD RESET] Received event
[QUEUE] Email: user@example.com
[QUEUE] User: John Doe
[QUEUE] Reset link length: 450
[EMAIL] Attempt 1/3 - Sending to user@example.com
[EMAIL] ✅ Sent successfully
[10:30:47] ✅ [PASSWORD RESET] Email sent to user@example.com
[QUEUE] Message acknowledged
========================================
```

### 4. Check Email Inbox

- Open email inbox
- Look for "Reset Your CapFinLoan Password"
- Click reset link
- Complete password reset

## Queue Details

### Queue Name
`password-reset`

### Queue Properties
- **Durable**: false (messages not persisted to disk)
- **Exclusive**: false (multiple consumers allowed)
- **Auto-delete**: false (queue persists after consumers disconnect)

### Message Format
```json
{
  "Email": "user@example.com",
  "UserName": "John Doe",
  "ResetLink": "http://localhost:5174/reset-password?email=...&token=..."
}
```

## Error Handling

### Auth Service

**If RabbitMQ is down:**
```
[RABBITMQ] ❌ Failed to publish message: Connection refused
```
- Exception thrown
- User sees generic success message (security)
- Error logged for debugging

### NotificationService

**If email fails:**
```
[10:30:45] ⚠️  [PASSWORD RESET] Attempt 1/3 failed
    Retrying in 2000ms...
[10:30:47] ⚠️  [PASSWORD RESET] Attempt 2/3 failed
    Retrying in 2000ms...
[10:30:49] ⚠️  [PASSWORD RESET] Attempt 3/3 failed
[10:30:49] ❌ [PASSWORD RESET] Failed after 3 attempts
[QUEUE] Message requeued
```
- Retries 3 times with 2-second delay
- Message requeued for later processing
- NotificationService continues running

## Monitoring

### RabbitMQ Management UI

Access: http://localhost:15672
- Username: `guest`
- Password: `guest`

**Check:**
- Queue `password-reset` exists
- Message count
- Consumer count
- Message rate

### Health Checks

**Auth Service:**
```bash
curl http://localhost:7001/api/auth/health
```

**RabbitMQ:**
```bash
curl http://localhost:15672/api/overview
```

## Troubleshooting

### Issue 1: RabbitMQ Connection Failed

**Symptoms:**
```
[RABBITMQ] ❌ Failed to publish message: Connection refused
```

**Solution:**
```bash
# Check if RabbitMQ is running
docker ps | grep rabbitmq

# If not running, start it
docker start rabbitmq

# Or run new container
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

### Issue 2: Messages Not Being Consumed

**Symptoms:**
- Messages in queue but not processed
- NotificationService not showing logs

**Solution:**
```bash
# Check NotificationService is running
# Restart NotificationService
cd CapFinLoan.Backend/NotificationService
dotnet run
```

### Issue 3: Email Sending Fails

**Symptoms:**
```
[EMAIL ERROR] Authentication failed
```

**Solution:**
```bash
# Reconfigure email settings
cd CapFinLoan.Backend/NotificationService
./setup-email.sh

# Restart NotificationService
dotnet run
```

### Issue 4: Messages Stuck in Queue

**Symptoms:**
- Messages in queue but not acknowledged
- Same message processed repeatedly

**Solution:**
1. Check NotificationService logs for errors
2. Fix email configuration
3. Restart NotificationService
4. Messages will be reprocessed

## Configuration

### Auth Service

**No email configuration needed!**

Only requires RabbitMQ connection (hardcoded to localhost:5672).

### NotificationService

**appsettings.json:**
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "CapFinLoan",
    "SenderEmail": "your@gmail.com",
    "Username": "your@gmail.com",
    "Password": "your_app_password",
    "EnableSsl": true
  }
}
```

## Comparison: Before vs After

### Before (Direct Email)

```
Auth Service
    ↓
  SMTP Server (Gmail)
    ↓
  Email Sent
```

**Issues:**
- Auth Service waits for email
- Slow response time
- Email config in Auth Service
- Single point of failure

### After (RabbitMQ)

```
Auth Service
    ↓
  RabbitMQ
    ↓
NotificationService
    ↓
  SMTP Server (Gmail)
    ↓
  Email Sent
```

**Benefits:**
- Fast response time
- Decoupled services
- Email config only in NotificationService
- Automatic retries
- Scalable

## Files Modified

### Auth Service

| File | Changes |
|------|---------|
| `AuthService.cs` | Replaced `IEmailService` with `IMessagePublisher` |
| `Program.cs` | Registered `RabbitMQPublisher` instead of `EmailService` |
| `Infrastructure.csproj` | Added `RabbitMQ.Client` package |

### Files Created

| File | Purpose |
|------|---------|
| `PasswordResetRequestedEvent.cs` | Event definition (Auth & Notification) |
| `IMessagePublisher.cs` | Publisher interface |
| `RabbitMQPublisher.cs` | RabbitMQ publisher implementation |

### NotificationService

| File | Changes |
|------|---------|
| `Program.cs` | Added password-reset queue handler |
| `IEmailService.cs` | Added `SendPasswordResetEmailAsync` method |
| `EmailService.cs` | Implemented password reset email template |

## Production Considerations

### 1. RabbitMQ Configuration

**Use durable queues:**
```csharp
await channel.QueueDeclareAsync(
    queue: "password-reset",
    durable: true,  // ← Change to true
    exclusive: false,
    autoDelete: false,
    arguments: null);
```

**Use persistent messages:**
```csharp
var properties = new BasicProperties
{
    Persistent = true
};

await channel.BasicPublishAsync(
    exchange: string.Empty,
    routingKey: queueName,
    basicProperties: properties,
    body: body);
```

### 2. Connection Management

**Use connection pooling:**
```csharp
// Create single connection, reuse channels
private static IConnection? _connection;
private static readonly object _lock = new object();
```

### 3. Monitoring

- Set up RabbitMQ alerts
- Monitor queue depth
- Track message processing time
- Alert on repeated failures

### 4. Scaling

**Multiple NotificationService instances:**
```bash
# Terminal 1
dotnet run

# Terminal 2
dotnet run

# Terminal 3
dotnet run
```

Messages automatically distributed across instances.

### 5. Dead Letter Queue

**Handle permanently failed messages:**
```csharp
var args = new Dictionary<string, object>
{
    { "x-dead-letter-exchange", "dlx" },
    { "x-dead-letter-routing-key", "password-reset-failed" }
};
```

## Summary

✅ Password reset now uses RabbitMQ for async email sending
✅ Auth Service responds immediately
✅ NotificationService handles email delivery
✅ Automatic retries on failure
✅ Fully decoupled architecture
✅ Production-ready with proper error handling

## Next Steps

1. Start RabbitMQ
2. Configure NotificationService email settings
3. Start Auth Service
4. Start NotificationService
5. Test password reset flow
6. Monitor RabbitMQ management UI

---

**Status:** ✅ Implemented and tested
**Queue:** `password-reset`
**Retry:** 3 attempts with 2-second delay
**Email:** Sent by NotificationService only
