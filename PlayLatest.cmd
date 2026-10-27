@echo off
setlocal
cd /d "%~dp0"
if not exist "Builds\Latest\FireRescue.exe" (
  echo Latest build is not available. Please build the Unity prototype first.
  pause
  exit /b 1
)
start "" "Builds\Latest\FireRescue.exe"
