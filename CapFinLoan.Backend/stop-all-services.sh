#!/bin/bash

echo "========================================="
echo "Stopping CapFinLoan Microservices"
echo "========================================="
echo ""

# Function to kill process on port
kill_port() {
    local port=$1
    local service_name=$2
    
    echo "Stopping $service_name (Port $port)..."
    
    # Find and kill process on macOS/Linux
    PID=$(lsof -ti :$port)
    
    if [ -n "$PID" ]; then
        kill -9 $PID 2>/dev/null
        echo "✓ $service_name stopped (PID: $PID)"
    else
        echo "  $service_name not running"
    fi
}

# Stop all services
kill_port 7000 "API Gateway"
kill_port 7001 "Auth Service"
kill_port 7002 "Application Service"
kill_port 7003 "Document Service"
kill_port 7004 "Admin Service"

echo ""
echo "========================================="
echo "All Services Stopped"
echo "========================================="
