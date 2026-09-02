@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"
set "DOTNET_CLI_HOME=%~dp0.dotnet-cli"
set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
where dotnet >nul 2>&1 || (
  echo dotnet が見つかりません。 .NET 8 SDK をインストールしてください。
  exit /b 1
)
set "STAMP="
for /f "delims=" %%T in ('powershell.exe -NoProfile -Command "Get-Date -Format yyyyMMdd_HHmmss"') do if not defined STAMP set "STAMP=%%T"
if exist dist\WindowsMicAutoMute.exe (
  ren dist "dist.bak-v1-!STAMP!"
  if errorlevel 1 exit /b 1
)
if exist buildobj (
  ren buildobj "buildobj.bak-v1-!STAMP!"
  if errorlevel 1 exit /b 1
)
dotnet build WindowsMicAutoMute.csproj -c Release --ignore-failed-sources -p:BaseIntermediateOutputPath=buildobj\ -p:OutputPath=dist\ -p:UseSharedCompilation=false
if errorlevel 1 exit /b %errorlevel%
echo Build complete: %~dp0dist\WindowsMicAutoMute.exe
endlocal
