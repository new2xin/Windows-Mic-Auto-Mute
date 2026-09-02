@echo off
setlocal
cd /d "%~dp0"
if not exist dist\WindowsMicAutoMute.exe call build.bat || exit /b 1
dist\WindowsMicAutoMute.exe --status --config devices.json
pause
endlocal
