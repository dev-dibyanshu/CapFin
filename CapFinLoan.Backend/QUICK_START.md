# Quick Start Guide - CapFinLoan Microservices

## Prerequisites

- .NET 8 SDK installed
- SQL Server running (localhost:1433)
- RabbitMQ running (localhost:5672) - optional for notifications

## Quick Start (Automated)

### Start All Services
```bash
cd CapFinLoan.Backend
./start-all-services.sh
```

This will:
1. Check if ports are available
2. Start Auth Service (7001)
3. Start Application Service (7002)
4. Start Document Service (7003)
5. Start Admin Service (7004)
6. Start API Gateway (7000)
7. Wait for services to be ready
8. Display service URLs and PIDs

### Stop All Services
```bash
cd CapFinLoan.Backend
./stop-all-services.sh
```

## Manual Start (Step by Step)

### Terminal 1 - Auth Service
```bash
cd CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API
dotnet run
```
Wait for: "Auth Service Started - Listening on: http://localhost:7001"

### Terminal 2 - Application Service
```bash
cd CapFinLoan.Backend/ApplicationService/CapFinLoan.Application.API
dotnet run
```
Wait for: "Application Service Started - Listening on: http://localhost:7002"

### Terminal 3 - Document Service
```bash
cd CapFinLoan.Backend/DocumentService/CapFinLoan.Document.API
dotnet run
```
Wait for: "Document Service Started - Listening on: http://localhost:7003"

### Terminal 4 - Admin Service
```bash
cd CapFinLoan.Backend/AdminService/CapFinLoan.Admin.API
dotnet run
```
Wait for: "Admin Service Started - Listening on: http://localhost:7004"

### Terminal 5 - API Gateway
```bash
cd CapFinLoan.Backend/ApiGateway/CapFinLoan.Gateway.API
dotnet run
```
Wait for: "API Gateway Started - Listening on: http://localhost:7000"

## Verify Services

### Health Check
```bash
# Via Gateway (Recommended)
curl http://localhost:7000/api/auth/health

# Direct to Auth Service
curl http://localhost:7001/api/auth/health
```

Expected Response:
```json
{
  "status": "Auth service running",
  "timestamp": "2026-03-31T16:30:00Z"
}
```

### Test Login
```bash
curl -X POST http://localhost:7000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "Password123!"
  }'
```

### Test Signup
```bash
curl -X POST http://localhost:7000/api/auth/signup \
  -H "Content-Type: application/json" \
  -d '{
    "email": "newuser@example.com",
    "password": "Password123!",
    "name": "New User",
    "phone": "1234567890"
  }'
```

## Service Ports

| Service | Port | URL |
|---------|------|-----|
| API Gateway | 7000 | http://localhost:7000 |
| Auth Service | 7001 | http://localhost:7001 |
| Application Service | 7002 | http://localhost:7002 |
| Document Service | 7003 | http://localhost:7003 |
| Admin Service | 7004 | http://localhost:7004 |

## API Endpoints (via Gateway)

### Auth Service
- `POST /api/auth/signup` - Create new account
- `POST /api/auth/login` - Login
- `POST /api/auth/forgot-password` - Request password reset
- `POST /api/auth/reset-password` - Reset password
- `GET /api/auth/health` - Health check

### Application Service
- `POST /api/applications` - Create application
- `GET /api/applications` - List applications
- `GET /api/applications/{id}` - Get application details
- `PUT /api/applications/{id}/personal-details` - Update personal details
- `PUT /api/applications/{id}/employment-details` - Update employment details
- `PUT /api/applications/{id}/loan-details` - Update loan details
- `POST /api/applications/{id}/submit` - Submit application

### Document Service
- `POST /api/documents/upload` - Upload document
- `GET /api/documents/application/{applicationId}` - List documents
- `GET /api/documents/{id}` - Get document details
- `GET /api/documents/{id}/download` - Download document

### Admin Service
- `GET /api/admin/applications` - List all applications
- `GET /api/admin/applications/{id}` - Get application details
- `POST /api/admin/applications/{id}/review` - Review application
- `GET /api/admin/dashboard` - Dashboard statistics

## Troubleshooting

### Port Already in Use
```bash
# Check what's using the port
lsof -i :7001

# Kill the process
kill -9 <PID>
```

### Service Not Starting
1. Check SQL Server is running
2. Check connection string in appsettings.json
3. Check logs in `./logs/` directory
4. Run `dotnet build` to check for compilation errors

### 502 Bad Gateway
1. Ensure Auth Service is running on port 7001
2. Test direct connectivity: `curl http://localhost:7001/api/auth/health`
3. Check Gateway logs for routing errors
4. Verify ocelot.json configuration

### Database Errors
```bash
# Reset database (WARNING: Deletes all data)
cd AuthService/CapFinLoan.Auth.API
dotnet ef database drop --force
dotnet ef database update
```

## Logs

Logs are stored in `./logs/` directory:
- `auth-service.log`
- `application-service.log`
- `document-service.log`
- `admin-service.log`
- `gateway.log`

View logs in real-time:
```bash
tail -f logs/auth-service.log
```

## Frontend

Start the React frontend:
```bash
cd CapFinLoan.Frontend
npm install
npm run dev
```

Frontend will run on: http://localhost:5174

Update `src/api/axios.js` to use Gateway:
```javascript
const api = axios.create({
  baseURL: 'http://localhost:7000'
});
```

## Next Steps

1. ✅ Start all services
2. ✅ Test health check
3. ✅ Test login/signup
4. ✅ Start frontend
5. ✅ Test complete flow

## Useful Commands

```bash
# Build all services
dotnet build CapFinLoan.Backend.slnx

# Clean build artifacts
dotnet clean CapFinLoan.Backend.slnx

# Check running services
lsof -i :7000,7001,7002,7003,7004

# View all logs
tail -f logs/*.log
```

## Documentation

- [502 Fix Guide](./502_FIX_GUIDE.md) - Detailed troubleshooting for 502 errors
- [Password Reset Guide](./AuthService/PASSWORD_RESET_TESTING.md) - Password reset testing
- [RabbitMQ Implementation](./RABBITMQ_IMPLEMENTATION.md) - Event-driven architecture
- [Notification Service](./NotificationService/README.md) - Email notifications
