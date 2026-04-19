# RabbitMQ Password Reset - Quick Start

## 🚀 Start Services

```bash
# 1. Start RabbitMQ
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# 2. Configure NotificationService email (one-time)
cd CapFinLoan.Backend/NotificationService
./setup-email.sh

# 3. Start Auth Service
cd CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API
dotnet run

# 4. Start NotificationService
cd CapFinLoan.Backend/NotificationService
dotnet run

# 5. Start Frontend
cd CapFinLoan.Frontend
npm run dev
```

## ✅ Success Indicators

### Auth Service Console
```
[RABBITMQ] ✅ Message published successfully
[FORGOT PASSWORD] ✅ Event published to RabbitMQ successfully
```

### NotificationService Console
```
[PASSWORD RESET] Received event
[EMAIL] ✅ Sent successfully
[QUEUE] Message acknowledged
```

## 🧪 Test Flow

1. Go to http://localhost:5174/login
2. Click "Forgot your password?"
3. Enter email
4. Check Auth Service logs → Should see RabbitMQ publish
5. Check NotificationService logs → Should see email sent
6. Check email inbox → Should receive reset email

## 📊 Monitoring

- **RabbitMQ UI:** http://localhost:15672 (guest/guest)
- **Auth Health:** http://localhost:7001/api/auth/health
- **Queue Name:** `password-reset`

## ❌ Troubleshooting

### RabbitMQ Not Running
```bash
docker start rabbitmq
# OR
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

### Email Not Sending
```bash
cd CapFinLoan.Backend/NotificationService
./setup-email.sh
# Restart NotificationService
```

### Messages Stuck in Queue
- Check NotificationService is running
- Check email configuration
- Restart NotificationService

## 📁 Architecture

```
Auth Service → RabbitMQ → NotificationService → Email
   (Fast)      (Queue)       (Async Send)      (SMTP)
```

## 🎯 Key Benefits

✅ Fast response (< 100ms)
✅ Decoupled services
✅ Automatic retries
✅ Scalable
✅ Resilient

## 📚 Documentation

- **Full Guide:** `RABBITMQ_PASSWORD_RESET.md`
- **Summary:** `RABBITMQ_REFACTOR_SUMMARY.md`
- **This File:** Quick reference

---

**Queue:** `password-reset`
**Retry:** 3 attempts, 2-second delay
**Email:** Sent by NotificationService only
