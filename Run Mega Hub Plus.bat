@echo off
cd /d "%~dp0src"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET 8 SDK is not installed. Get it from https://dotnet.microsoft.com/download then run this again.
  pause
  exit /b
)
dotnet run
pause
