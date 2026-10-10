[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "SeparatorStudio.App\SeparatorStudio.App.csproj"
$output = Join-Path $PSScriptRoot "artifacts\SeparatorStudio"

dotnet publish $project `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output $output `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true

Write-Host "Executable créé : $(Join-Path $output 'SeparatorStudio.App.exe')" -ForegroundColor Green
