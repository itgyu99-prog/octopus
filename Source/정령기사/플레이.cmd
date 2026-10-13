@echo off
cd /d "%~dp0"
if not exist "Build\Windows\AshenOath.exe" (
  echo Build/Windows/AshenOath.exe was not found. Build this project in Unity first.
  pause
  exit /b 1
)
start "" "Build\Windows\AshenOath.exe"
