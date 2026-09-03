@echo off
setlocal
cd /d "%~dp0"
if not exist dist\WindowsMicAutoMute.exe call build.bat || exit /b 1
set "EXE=%~dp0dist\WindowsMicAutoMute.exe"
set "CFG=%~dp0devices.json"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0register_task.ps1"
if errorlevel 1 exit /b %errorlevel%
"%EXE%" --stop >nul 2>&1
start "Windows Mic Auto Mute" /min "%EXE%" --startup-mute --config "%CFG%"
echo 起動時ミュートを有効化しました。ログオン時に一度だけミュートして終了します。
endlocal
