#!/bin/bash
# Clean restart script - kills processes, removes NATS stores and databases
set -e

# Check if --show-processes flag is passed
if [[ "$1" == "--show-processes" ]]; then
    echo "📋 Current dotnet processes:"
    ps aux | grep dotnet | grep -v grep | head -20
    echo ""
    echo "🔍 TOOL-specific processes:"
    ps aux | grep "dotnet.*TOOL" | grep -v grep || echo "None found"
    echo ""
    echo "🏃 dotnet run processes:"
    ps aux | grep "dotnet run" | grep -v grep || echo "None found"
    exit 0
fi

echo "🧹 Starting clean restart..."

# Kill all dotnet processes (except VS Code extensions)
echo "🔪 Killing dotnet processes..."
pids=$(ps aux | grep dotnet | grep -v "Code Helper" | grep -v "OmniSharp" | awk '{print $2}' | grep -v PID || true)
if [ ! -z "$pids" ]; then
    echo "Found dotnet processes: $pids"
    kill $pids 2>/dev/null || true
    sleep 2
    # Force kill if still running
    kill -9 $pids 2>/dev/null || true
    echo "✅ Killed dotnet processes"
else
    echo "✅ No dotnet processes to kill"
fi

# Find and delete all NATS store directories recursively
echo "🗑️  Removing NATS stores..."
find . -type d -name "nats_store" -exec rm -rf {} + 2>/dev/null || true
find . -type d -name "jetstream" -exec rm -rf {} + 2>/dev/null || true
find . -type d -name "nats-data*" -exec rm -rf {} + 2>/dev/null || true
find . -type d -name ".nats" -exec rm -rf {} + 2>/dev/null || true
echo "✅ Removed NATS store directories"

# Find and delete all database files recursively
echo "🗄️  Removing database files..."
find . -name "*.db" -type f -delete 2>/dev/null || true
find . -name "*.db-*" -type f -delete 2>/dev/null || true
find . -name "promoter.db*" -type f -delete 2>/dev/null || true
find . -name "rules.db*" -type f -delete 2>/dev/null || true
echo "✅ Removed database files"

# Clean NATS streams if NATS is running
echo "🚿 Cleaning NATS streams..."
if command -v nats >/dev/null 2>&1; then
    # Try to list streams and capture output
    stream_output=$(nats stream ls 2>/dev/null || true)
    
    # Check if there are any streams (look for "No Streams defined" or table header)
    if echo "$stream_output" | grep -q "No Streams defined"; then
        echo "✅ No NATS streams to clean"
    elif [ ! -z "$stream_output" ]; then
        echo "Found NATS streams, removing them..."
        # Delete common streams we know about
        nats stream rm AUDITS -f 2>/dev/null || true
        nats stream rm EVENTS -f 2>/dev/null || true  
        nats stream rm DELTAS -f 2>/dev/null || true
        echo "✅ Cleaned NATS streams"
    else
        echo "✅ No NATS streams to clean"
    fi
else
    echo "⚠️  NATS CLI not available, skipping stream cleanup"
fi

echo "🎉 Clean restart complete! Ready for fresh start."
echo ""
echo "To start the application:"
echo "  dotnet run --project ./src/TOOL/"