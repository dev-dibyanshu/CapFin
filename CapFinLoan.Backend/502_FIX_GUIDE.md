# 502 Bad Gateway Fix - Complete Guide

## Problem
API Gateway (Ocelot) was returning 502 Bad Gateway when trying to route requests to Auth Service.

## Root Causes Identified

1. **Inconsistent Route Paths**: Ocelot was configured with `/auth/{everything}` but Auth Service uses `/api/auth/{everything}`
2. **No Explicit Port Binding**: Services were not explicitly bound to fixed ports
3. **Missing Health Check**: No way to verify service connectivity
4. **Insufficient Logging**: Hard to debug routing issues

## Fixes Applied

### 1. Auth Service (Port 7001)

#### Added Health Check Endpoint
**File:** `CapFinLoan.Auth.API/Controllers/AuthController.cs`

```csharp
[HttpGet("health")]
[AllowAnonymous]
[ProducesResponseType(StatusCodes.Status200OK)]
public IActionResult Health()
{
    return Ok(new { status = "Auth service running", timestamp = DateTime.UtcNow });
}
```

**Test:**
```bash
curl http://localhost:7001/api/auth/health
```

**Expected Response:**
```json
{
  "status": "Auth service running",
  "timestamp": "2026-03-31T16:30:00Z"
}
```

#### Fixed Port Binding
**File:** `CapFinLoan.Auth.API/Program.cs`

```csharp
// Explicitly configure URL to ensure fixed port
builder.WebHost.UseUrls("http://localhost:7001");

// Add detailed logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);
```

#### Added Startup Banner
```csharp
Console.WriteLine("========================================");
Console.WriteLine("Auth Service Started");
Console.WriteLine($"Listening on: http://localhost:7001");
Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
Console.WriteLine("========================================");
```

### 2. API Gateway (Port 7000)

#### Fixed Ocelot Routes
**File:** `CapFinLoan.Gateway.API/ocelot.json`

**BEFORE (Wrong):**
```json
{
  "UpstreamPathTemplate": "/auth/{everything}",
  "DownstreamPathTemplate": "/api/auth/{everything}"
}
```

**AFTER (Correct):**
```json
{
  "UpstreamPathTemplate": "/api/auth/{everything}",
  "DownstreamPathTemplate": "/api/auth/{everything}"
}
```

**Key Change:** Both upstream and downstream now use `/api/auth/` prefix for consistency.

#### Added Debug Logging
**File:** `CapFinLoan.Gateway.API/Program.cs`

```csharp
// Explicitly configure URL to ensure fixed port
builder.WebHost.UseUrls("http://localhost:7000");

// Add detailed logging for debugging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

#### Added Startup Banner
```csharp
Console.WriteLine("========================================");
Console.WriteLine("API Gateway Started");
Console.WriteLine($"Listening on: http://localhost:7000");
Console.WriteLine("Routes configured:");
Console.WriteLine("  /api/auth/* -> http://localhost:7001");
Console.WriteLine("  /api/applications/* -> http://localhost:7002");
Console.WriteLine("  /api/documents/* -> http://localhost:7003");
Console.WriteLine("  /api/admin/* -> http://localhost:7004");
Console.WriteLine("========================================");
```

### 3. Other Services

Applied same fixes to:
- **ApplicationService** (Port 7002)
- **DocumentService** (Port 7003)
- **AdminService** (Port 7004)

All services now:
- Bind to fixed ports explicitly
- Display startup banner with port info
- Have consistent configuration

## Port Mapping

| Service | Port | Direct URL | Via Gateway |
|---------|------|------------|-------------|
| Auth Service | 7001 | http://localhost:7001/api/auth/* | http://localhost:7000/api/auth/* |
| Application Service | 7002 | http://localhost:7002/api/applications/* | http://localhost:7000/api/applications/* |
| Document Service | 7003 | http://localhost:7003/api/documents/* | http://localhost:7000/api/documents/* |
| Admin Service | 7004 | http://localhost:7004/api/admin/* | http://localhost:7000/api/admin/* |
| API Gateway | 7000 | http://localhost:7000 | - |

## Testing Guide

### Step 1: Start Services in Order

```bash
# Terminal 1 - Auth Service
cd CapFinLoan.Backend/AuthService/CapFinLoan.Auth.API
dotnet run

# Terminal 2 - Application Service
cd CapFinLoan.Backend/ApplicationService/CapFinLoan.Application.API
dotnet run

# Terminal 3 - Document Service
cd CapFinLoan.Backend/DocumentService/CapFinLoan.Document.API
dotnet run

# Terminal 4 - Admin Service
cd CapFinLoan.Backend/AdminService/CapFinLoan.Admin.API
dotnet run

# Terminal 5 - API Gateway (Start LAST)
cd CapFinLoan.Backend/ApiGateway/CapFinLoan.Gateway.API
dotnet run
```

### Step 2: Verify Direct Connectivity

Test each service directly:

```bash
# Auth Service Health Check
curl http://localhost:7001/api/auth/health

# Application Service (requires auth, expect 401)
curl http://localhost:7002/api/applications

# Document Service (requires auth, expect 401)
curl http://localhost:7003/api/documents

# Admin Service (requires auth, expect 401)
curl http://localhost:7004/api/admin/applications
```

### Step 3: Test Via Gateway

```bash
# Auth Service via Gateway
curl http://localhost:7000/api/auth/health

# Login via Gateway
curl -X POST http://localhost:7000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Password123!"}'

# Signup via Gateway
curl -X POST http://localhost:7000/api/auth/signup \
  -H "Content-Type: application/json" \
  -d '{
    "email":"test@example.com",
    "password":"Password123!",
    "name":"Test User",
    "phone":"1234567890"
  }'
```

### Step 4: Verify Routing

Check Gateway logs for routing information:
- Should see Ocelot middleware processing requests
- Should see successful downstream calls
- No 502 errors

## Troubleshooting

### Issue: Still Getting 502

**Check 1: Service Running?**
```bash
# Check if Auth Service is responding
curl http://localhost:7001/api/auth/health
```

If this fails, Auth Service isn't running or not on port 7001.

**Check 2: Port Conflicts?**
```bash
# macOS/Linux
lsof -i :7001
lsof -i :7000

# Windows
netstat -ano | findstr :7001
netstat -ano | findstr :7000
```

**Check 3: Firewall?**
Ensure localhost traffic is allowed.

**Check 4: Gateway Logs**
Look for Ocelot errors in Gateway console output.

### Issue: 404 Not Found

**Cause:** Route path mismatch

**Solution:** Ensure you're using correct paths:
- ✅ `http://localhost:7000/api/auth/login`
- ❌ `http://localhost:7000/auth/login`

### Issue: 401 Unauthorized

**This is EXPECTED** for protected endpoints without a token.

**Solution:** Login first, get token, then use it:
```bash
# 1. Login
TOKEN=$(curl -X POST http://localhost:7000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Password123!"}' \
  | jq -r '.token')

# 2. Use token
curl http://localhost:7000/api/applications \
  -H "Authorization: Bearer $TOKEN"
```

### Issue: Connection Refused

**Cause:** Service not started or wrong port

**Solution:**
1. Check service is running
2. Verify port in startup banner
3. Check launchSettings.json matches Program.cs

## Verification Checklist

- [ ] Auth Service shows "Listening on: http://localhost:7001"
- [ ] Application Service shows "Listening on: http://localhost:7002"
- [ ] Document Service shows "Listening on: http://localhost:7003"
- [ ] Admin Service shows "Listening on: http://localhost:7004"
- [ ] Gateway shows "Listening on: http://localhost:7000"
- [ ] `curl http://localhost:7001/api/auth/health` returns 200 OK
- [ ] `curl http://localhost:7000/api/auth/health` returns 200 OK (via gateway)
- [ ] Login via gateway works
- [ ] No 502 errors in Gateway logs

## Key Takeaways

1. **Always use explicit port binding** with `builder.WebHost.UseUrls()`
2. **Keep route paths consistent** between gateway and services
3. **Add health check endpoints** for connectivity testing
4. **Enable detailed logging** during development
5. **Start services in order**: Backend services first, Gateway last
6. **Test direct connectivity** before testing via gateway

## Files Modified

### Created
- `CapFinLoan.Backend/502_FIX_GUIDE.md` (this file)

### Modified
- `CapFinLoan.Auth.API/Controllers/AuthController.cs` (added health endpoint)
- `CapFinLoan.Auth.API/Program.cs` (fixed port, added logging)
- `CapFinLoan.Gateway.API/ocelot.json` (fixed routes)
- `CapFinLoan.Gateway.API/Program.cs` (fixed port, added logging)
- `CapFinLoan.Application.API/Program.cs` (fixed port, added banner)
- `CapFinLoan.Document.API/Program.cs` (fixed port, added banner)
- `CapFinLoan.Admin.API/Program.cs` (fixed port, added banner)

## Next Steps

1. Start all services in order
2. Test health endpoint
3. Test login via gateway
4. Update frontend to use gateway URL (http://localhost:7000)
5. Monitor logs for any issues
