#!/bin/bash
cd "$(dirname "$0")"

if [ ! -f appsettings.json ]; then
    echo "[!] appsettings.json not found. Run ./setup.sh first."
    exit 1
fi

echo "Starting Ash..."
dotnet run
