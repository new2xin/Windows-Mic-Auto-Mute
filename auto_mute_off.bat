@echo off
setlocal
set "TASK=Windows Mic Auto Mute"
schtasks /End /TN "%TASK%" >nul 2>&1
schtasks /Delete /TN "%TASK%" /F >nul 2>&1
if exist "%~dp0dist\WindowsMicAutoMute.exe" "%~dp0dist\WindowsMicAutoMute.exe" --stop >nul 2>&1
echo 起動時ミュートを無効化しました。現在のミュート状態は変更していません。
endlocal
