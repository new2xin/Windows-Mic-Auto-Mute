@echo off
setlocal
set "TASK=Lyra Auto Mute"
schtasks /End /TN "%TASK%" >nul 2>&1
schtasks /Delete /TN "%TASK%" /F >nul 2>&1
if exist "%~dp0dist\LyraAutoMute.exe" "%~dp0dist\LyraAutoMute.exe" --stop >nul 2>&1
echo 自動ミュートを無効化しました。現在のミュート状態は変更していません。
endlocal
