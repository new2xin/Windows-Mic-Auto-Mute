@echo off
setlocal
cd /d "%~dp0"
if not exist dist\LyraAutoMute.exe call build.bat || exit /b 1
dist\LyraAutoMute.exe --mute --config devices.json
endlocal
