#!/bin/bash
set -e

echo ""
echo " ================================================"
echo "  Ash Bot - Setup"
echo " ================================================"
echo ""

# Check for .NET
if ! command -v dotnet &> /dev/null; then
    echo "[!] .NET not found."
    echo "    Install .NET 10 from: https://dot.net/download"
    echo "    Or on Linux: https://learn.microsoft.com/dotnet/core/install/linux"
    exit 1
fi
echo "[OK] .NET found."

# Check for Ollama
if ! command -v ollama &> /dev/null; then
    echo "[..] Ollama not found - installing..."
    curl -fsSL https://ollama.com/install.sh | sh
    echo "[OK] Ollama installed."
else
    echo "[OK] Ollama already installed."
fi

# Create appsettings.json if missing
if [ ! -f appsettings.json ]; then
    echo ""
    echo "[..] Creating appsettings.json from template..."
    cp appsettings.example.json appsettings.json
    echo "[OK] appsettings.json created."
    echo ""
    echo " *** ACTION REQUIRED ***"
    echo " Edit appsettings.json and fill in:"
    echo "   - DiscordToken   (from Discord Developer Portal)"
    echo "   - ChannelId      (right-click channel -> Copy Channel ID)"
    echo "   - GuildId        (right-click server -> Copy Server ID)"
    echo "   - AdminUserId    (right-click your profile -> Copy User ID)"
    echo ""
    echo " Then run ./setup.sh again or launch with ./start-ash.sh"
else
    echo "[OK] appsettings.json already exists."
fi

echo ""
echo " ================================================"
echo "  Setup complete! Launch Ash with ./start-ash.sh"
echo " ================================================"
echo ""
