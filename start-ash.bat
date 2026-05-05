@echo off
title Ash Bot
cd /d "%~dp0"

if not exist appsettings.json (
    echo [!] appsettings.json not found. Run setup.bat first.
    pause
    exit /b 1
)

echo Starting Ash...
dotnet run
pause
