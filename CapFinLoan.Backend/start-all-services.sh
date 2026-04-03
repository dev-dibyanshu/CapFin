#!/bin/bash

echo "========================================="
echo "Starting CapFinLoan Microservices"
echo "========================================="
echo ""

# Function to check if a port is in use
check_port() {
    lsof -i :$1 > /dev/null 2>&1
    return $?
}

# Function to wait for service to be ready
wait_for_service() {
    local url=$1
    local service_name=$2
    local max_attempts=30
    local attempt=0
    
    echo "Waiting for $service_name to be ready..."
    while [ $attempt -lt $max_attempts ]; do
        if curl -s "$url" > /dev/null 2>&1; then
            echo "✓ $service_name is ready!"
            return 0
        fi
        attempt=$((attempt + 1))
        sleep 1
    done
    
    echo "✗ $service_name failed to start within 30 seconds"
    return 1
}

# Check if ports are already in use
echo "Checking ports..."
for port in 7001 7002 7003 7004 7000; do
    if check_port $port; then
        echo "⚠️  Port $port is already in use. Please stop the service or change the port."
        exit 1
    fi
done
echo "✓ All ports are available"
echo ""

# Start Auth Service
echo "Starting Auth Service (Port 7001)..."
cd AuthService/CapFinLoan.Auth.API
dotnet run > ../../logs/auth-service.log 2>&1 &
AUTH_PID=$!
cd ../..
sleep 3
wait_for_service "http://localhost:7001/api/auth/health" "Auth Service"
echo ""

# Start Application Service
echo "Starting Application Service (Port 7002)..."
cd ApplicationService/CapFinLoan.Application.API
dotnet run > ../../logs/application-service.log 2>&1 &
APP_PID=$!
cd ../..
sleep 3
echo "✓ Application Service started"
echo ""

# Start Document Service
echo "Starting Document Service (Port 7003)..."
cd DocumentService/CapFinLoan.Document.API
dotnet run > ../../logs/document-service.log 2>&1 &
DOC_PID=$!
cd ../..
sleep 3
echo "✓ Document Service started"
echo ""

# Start Admin Service
echo "Starting Admin Service (Port 7004)..."
cd AdminService/CapFinLoan.Admin.API
dotnet run > ../../logs/admin-service.log 2>&1 &
ADMIN_PID=$!
cd ../..
sleep 3
echo "✓ Admin Service started"
echo ""

# Start API Gateway
echo "Starting API Gateway (Port 7000)..."
cd ApiGateway/CapFinLoan.Gateway.API
dotnet run > ../../logs/gateway.log 2>&1 &
GATEWAY_PID=$!
cd ../..
sleep 3
wait_for_service "http://localhost:7000/api/auth/health" "API Gateway"
echo ""

echo "========================================="
echo "All Services Started Successfully!"
echo "========================================="
echo ""
echo "Service URLs:"
echo "  Auth Service:        http://localhost:7001"
echo "  Application Service: http://localhost:7002"
echo "  Document Service:    http://localhost:7003"
echo "  Admin Service:       http://localhost:7004"
echo "  API Gateway:         http://localhost:7000"
echo ""
echo "Process IDs:"
echo "  Auth Service:        $AUTH_PID"
echo "  Application Service: $APP_PID"
echo "  Document Service:    $DOC_PID"
echo "  Admin Service:       $ADMIN_PID"
echo "  API Gateway:         $GATEWAY_PID"
echo ""
echo "Logs are available in: ./logs/"
echo ""
echo "To stop all services, run: ./stop-all-services.sh"
echo "Or manually kill processes: kill $AUTH_PID $APP_PID $DOC_PID $ADMIN_PID $GATEWAY_PID"
echo ""
echo "Test health check:"
echo "  curl http://localhost:7000/api/auth/health"
echo ""
