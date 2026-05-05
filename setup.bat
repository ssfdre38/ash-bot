@echo off
echo.
echo  ================================================
echo   Ash Bot - Setup
echo  ================================================
echo.

:: Check for .NET 10
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [!] .NET not found.
    echo     Download and install .NET 10 from: https://dot.net/download
    echo     Then run setup.bat again.
    pause
    exit /b 1
)
echo [OK] .NET found.

:: Check for Ollama
where ollama >nul 2>&1
if %errorlevel% neq 0 (
    echo [..] Ollama not found - downloading installer...
    curl -L -o OllamaSetup.exe https://ollama.com/download/OllamaSetup.exe
    if %errorlevel% neq 0 (
        echo [!] Download failed. Get Ollama manually from https://ollama.com/download
        pause
        exit /b 1
    )
    echo [..] Running Ollama installer...
    OllamaSetup.exe
    del OllamaSetup.exe
    echo [OK] Ollama installed.
) else (
    echo [OK] Ollama already installed.
)

:: Create appsettings.json if missing
if not exist appsettings.json (
    echo.
    echo [..] Creating appsettings.json from template...
    copy appsettings.example.json appsettings.json >nul
    echo [OK] appsettings.json created.
    echo.
    echo  *** ACTION REQUIRED ***
    echo  Edit appsettings.json and fill in:
    echo    - DiscordToken   ^(from Discord Developer Portal^)
    echo    - ChannelId      ^(right-click channel -^> Copy Channel ID^)
    echo    - GuildId        ^(right-click server -^> Copy Server ID^)
    echo    - AdminUserId    ^(right-click your profile -^> Copy User ID^)
    echo.
    echo  Then run setup.bat again or launch with start-ash.bat
) else (
    echo [OK] appsettings.json already exists.
)

echo.
echo  ================================================
echo   Setup complete! Launch Ash with start-ash.bat
echo  ================================================
echo.
pause
