$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$exe = Join-Path $root 'dist\WindowsMicAutoMute.exe'
$config = Join-Path $root 'devices.json'
$taskName = 'Windows Mic Auto Mute'

if (-not (Test-Path -LiteralPath $exe)) {
    throw "実行ファイルがありません: $exe"
}

$action = New-ScheduledTaskAction `
    -Execute $exe `
    -Argument ('--watch --config "{0}"' -f $config) `
    -WorkingDirectory (Join-Path $root 'dist')
$trigger = New-ScheduledTaskTrigger -AtLogOn
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -RunLevel Limited -Force | Out-Null
Write-Output "登録完了: $taskName"
