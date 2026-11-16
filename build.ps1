param([ValidateSet('win-x64','win-arm64')][string]$Runtime = 'win-x64', [string]$OutputDirectory = (Join-Path $env:USERPROFILE 'Downloads\freshos'))
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Install the .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0 then reopen Command Prompt. You need the SDK, not just the runtime.' }
if (-not ((dotnet --list-sdks) -match '^8\.')) { throw 'The .NET 8 SDK is required.' }
function Publish-Project($project, $destination) {
    dotnet publish $project -c Release -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o $destination
    if ($LASTEXITCODE -ne 0) { throw "Publish failed: $project" }
}
$app = Join-Path $PSScriptRoot "artifacts\$Runtime\app"
$setup = Join-Path $PSScriptRoot "artifacts\$Runtime\setup"
New-Item -ItemType Directory -Force $app,$setup,$OutputDirectory | Out-Null
Publish-Project 'FreshOS\FreshOS.csproj' $app
$payload=Join-Path $PSScriptRoot 'FreshOS.Setup\payload.zip'
if (Test-Path $payload) { Remove-Item $payload }
Compress-Archive -Path (Join-Path $app '*') -DestinationPath $payload
Publish-Project 'FreshOS.Setup\FreshOS.Setup.csproj' $setup
Copy-Item (Join-Path $setup 'FreshOS-Setup.exe') $OutputDirectory -Force
Copy-Item (Join-Path $app 'FreshOS.exe') $OutputDirectory -Force
Write-Host "`nReady: $OutputDirectory\FreshOS-Setup.exe" -ForegroundColor Green
Write-Host "Portable app: $OutputDirectory\FreshOS.exe"
Write-Host 'No system changes or startup entries have been made by this builder.'
