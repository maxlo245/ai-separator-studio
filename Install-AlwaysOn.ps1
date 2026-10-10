[CmdletBinding()]
param(
    [string]$ExecutablePath = (Join-Path $PSScriptRoot "artifacts\SeparatorStudio\SeparatorStudio.App.exe")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ExecutablePath)) {
    throw "L'exécutable est introuvable : $ExecutablePath. Exécutez d'abord .\Publish-SeparatorStudio.ps1."
}

$taskName = "AI Separator Studio"
$action = New-ScheduledTaskAction -Execute $ExecutablePath
$trigger = New-ScheduledTaskTrigger -AtLogOn -User "$env:USERDOMAIN\$env:USERNAME"
$settings = New-ScheduledTaskSettingsSet -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1) -ExecutionTimeLimit (New-TimeSpan -Days 3650) -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Description "Démarre AI Separator Studio à l'ouverture de session." -Force | Out-Null
Write-Host "Tâche '$taskName' installée. Configurez le mode « Surveiller un dossier » dans l'application, puis laissez Windows allumé et sans veille." -ForegroundColor Green
