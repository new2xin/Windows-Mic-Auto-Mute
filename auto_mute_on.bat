@echo off
setlocal
cd /d "%~dp0"
if not exist dist\LyraAutoMute.exe call build.bat || exit /b 1
set "EXE=%~dp0dist\LyraAutoMute.exe"
set "CFG=%~dp0devices.json"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0register_task.ps1"
if errorlevel 1 exit /b %errorlevel%
"%EXE%" --stop >nul 2>&1
start "Lyra Auto Mute" /min "%EXE%" --watch --config "%CFG%"
echo 自動ミュートを有効化しました。現在の認識済みLyraにも適用します。
endlocal
