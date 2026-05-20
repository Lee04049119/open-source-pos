# Same as PosNetworkSetup WinForms — no build required.
# Example:
#   .\scripts\Set-PosLanConfig.ps1 -RepoRoot "C:\GITHUB\open-source-pos" -HostName "192.168.0.15"

param(
    [Parameter(Mandatory = $true)]
    [string] $RepoRoot,

    [Parameter(Mandatory = $true)]
    [string] $HostName,

    [int] $AngularPort = 4200,
    [int] $HttpApiPort = 5000,
    [int] $HttpsApiPort = 5001,
    [int] $ImagePort = 9096
)

$ErrorActionPreference = 'Stop'
$RepoRoot = $RepoRoot.Trim().TrimEnd('\')
$HostName = $HostName.Trim()

$apiDir = Join-Path $RepoRoot 'open-source-pos'
$feDir = Join-Path $RepoRoot 'open-source-pos-frontend\src\assets'

if (-not (Test-Path $apiDir)) { throw "Not found: $apiDir" }
if (-not (Test-Path $feDir)) { throw "Not found: $feDir" }

$localJson = @{
    Lan = @{
        Host = $HostName
        AngularPort = $AngularPort
        HttpApiPort = $HttpApiPort
        HttpsApiPort = $HttpsApiPort
        ImagePort = $ImagePort
    }
}

$runtimeJson = @{
    apiBaseUrl = "http://${HostName}:${HttpApiPort}/api"
    apiBaseUrlHttps = "https://${HostName}:${HttpsApiPort}/api"
    imageServerUrl = "http://${HostName}:${ImagePort}/"
    imageServerUrlHttps = "http://${HostName}:${ImagePort}/"
}

$opts = @{ Depth = 5 }

$localPath = Join-Path $apiDir 'appsettings.Local.json'
$runtimePath = Join-Path $feDir 'app-runtime-config.json'

$localJson | ConvertTo-Json @opts | Set-Content -Path $localPath -Encoding UTF8
$runtimeJson | ConvertTo-Json @opts | Set-Content -Path $runtimePath -Encoding UTF8

Write-Host "Saved:" -ForegroundColor Green
Write-Host "  $localPath"
Write-Host "  $runtimePath"
Write-Host ""
Write-Host "Restart the API and ng serve." -ForegroundColor Yellow
