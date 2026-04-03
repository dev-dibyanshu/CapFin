# RabbitMQ Event-Driven Communication Implementation

## Overview
Implemented event-driven communication using RabbitMQ for ApplicationSubmitted events in the CapFinLoan project.

## Components Implemented

### 1. Application Service (Publisher)

**Package Installed:**
- RabbitMQ.Client v7.2.1

**Files Created/Modified:**

1. **ApplicationSubmittedEvent.cs**
   - Location: `CapFinLoan.Application.Application/Events/ApplicationSubmittedEvent.cs`
   - Properties:
     - ApplicationId (Guid)
     - Email (string)
     - ApplicantName (string)

2. **LoanApplicationService.cs** (Modified)
   - Added RabbitMQ publishing logic in `SubmitAsync()` method
   - Publishes event AFTER successful database save
   - Event contains:
     - ApplicationId
     - Email
     - ApplicantName (FirstName + LastName)
   - Queue: "application-submitted"
   - Error handling: Failures don't break application submission

**Publishing Flow:**
```
1. Application submitted successfully
2. Database updated
3. Event published to RabbitMQ queue "application-submitted"
4. Response returned to client
```

### 2. Notification Service (Consumer)

**Project Created:**
- Type: .NET Console Application
- Name: NotificationService
- Location: `CapFinLoan.Backend/NotificationService/`

**Package Installed:**
- RabbitMQ.Client v7.2.1

**Files Created:**

1. **ApplicationSubmittedEvent.cs**
   - Same structure as publisher event
   - Used for deserialization

2. **Program.cs**
   - Connects to RabbitMQ (localhost:5672)
   - Declares queue "application-submitted"
   - Creates AsyncEventingBasicConsumer
   - Listens for messages
   - Deserializes ApplicationSubmittedEvent
   - Simulates email sending by printing to console

**Consumer Output Format:**
```
[HH:mm:ss] ✉️  Email sent to {Email}:
    Hello {ApplicantName}, your application is submitted.
    Application ID: {ApplicationId}
```

## RabbitMQ Configuration

**Connection Settings:**
- Host: localhost
- Port: 5672 (default)
- Queue Name: "application-submitted"
- Queue Properties:
  - Durable: false
  - Exclusive: false
  - AutoDelete: false

## How to Run

### Prerequisites
1. Install RabbitMQ:
   ```bash
   # macOS
   brew install rabbitmq
   brew services start rabbitmq
   
   # Or using Docker
   docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
   ```

2. Verify RabbitMQ is running:
   - Management UI: http://localhost:15672
   - Default credentials: guest/guest

### Start Services

1. **Start NotificationService (Consumer):**
   ```bash
   cd CapFinLoan.Backend/NotificationService
   dotnet run
   ```
   Expected output:
   ```
   === Notification Service Started ===
   Waiting for ApplicationSubmitted events...
   [*] Connected to RabbitMQ
   [*] Listening on queue: application-submitted
   ```

2. **Start ApplicationService (Publisher):**
   ```bash
   cd CapFinLoan.Backend/ApplicationService/CapFinLoan.Application.API
   dotnet run
   ```

3. **Submit an Application:**
   - Use the frontend or API to submit a loan application
   - POST to `/applications/{id}/submit`

### Expected Flow

1. **Frontend/API Call:**
   ```
   POST http://localhost:7000/applications/{id}/submit
   ```

2. **ApplicationService:**
   - Validates application
   - Updates status to "Submitted"
   - Saves to database
   - Publishes event to RabbitMQ
   - Console output: `[RabbitMQ] Published ApplicationSubmitted event for {id}`

3. **NotificationService:**
   - Receives event from queue
   - Deserializes message
   - Prints simulated email:
   ```
   [14:30:45] ✉️  Email sent to user@example.com:
       Hello John Doe, your application is submitted.
       Application ID: 12345678-1234-1234-1234-123456789abc
   ```

## Testing

### Test Scenario 1: Successful Submission
1. Create a draft application
2. Fill in all required details
3. Submit the application
4. Check NotificationService console for email simulation

### Test Scenario 2: RabbitMQ Unavailable
1. Stop RabbitMQ
2. Submit an application
3. Application should still be saved successfully
4. Console shows: `[RabbitMQ] Failed to publish event: ...`
5. Application submission is NOT affected

## Architecture Benefits

1. **Decoupling:** ApplicationService doesn't need to know about notification logic
2. **Resilience:** Event publishing failures don't break application submission
3. **Scalability:** Multiple consumers can process events independently
4. **Extensibility:** Easy to add more event consumers (SMS, Push notifications, etc.)

## Future Enhancements

1. Add more event types:
   - ApplicationApproved
   - ApplicationRejected
   - DocumentUploaded

2. Add more consumers:
   - SMS Notification Service
   - Push Notification Service
   - Analytics Service

3. Implement retry logic with dead-letter queues
4. Add message persistence (durable queues)
5. Implement event versioning
6. Add distributed tracing

## Troubleshooting

### Issue: "None of the specified endpoints were reachable"
**Solution:** Start RabbitMQ server
```bash
brew services start rabbitmq
# or
docker start rabbitmq
```

### Issue: Events not being received
**Solution:** 
1. Check RabbitMQ management UI (http://localhost:15672)
2. Verify queue "application-submitted" exists
3. Check for messages in queue
4. Restart NotificationService

### Issue: Application submission fails
**Solution:** 
- Event publishing is wrapped in try-catch
- Check ApplicationService console for RabbitMQ errors
- Application should still save successfully even if event fails

## Code Locations

```
CapFinLoan.Backend/
├── ApplicationService/
│   └── CapFinLoan.Application.Application/
│       ├── Events/
│       │   └── ApplicationSubmittedEvent.cs
│       └── Services/
│           └── LoanApplicationService.cs (modified)
└── NotificationService/
    ├── ApplicationSubmittedEvent.cs
    ├── Program.cs
    └── NotificationService.csproj
```

## Dependencies Added

**ApplicationService:**
```xml
<PackageReference Include="RabbitMQ.Client" Version="7.2.1" />
```

**NotificationService:**
```xml
<PackageReference Include="RabbitMQ.Client" Version="7.2.1" />
```
