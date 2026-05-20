# Same as PosNetworkSetup WinForms — no build required.
# Example:
#   .\scripts\Set-PosLanConfig.ps1 -RepoRoot "C:\Users\lee\Documents\GitHub\open-source-pos" -HostName "10.0.157.138"

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
$devPath = Join-Path $apiDir 'appsettings.Development.json'

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
    imageServerUrlHttps = "https://${HostName}:${ImagePort}/"
}

$newOrigins = @(
    "http://${HostName}:${AngularPort}",
    "https://${HostName}:${AngularPort}",
    "http://${HostName}:${HttpApiPort}",
    "https://${HostName}:${HttpsApiPort}",
    "http://${HostName}:${ImagePort}",
    "https://${HostName}:${ImagePort}"
)

$opts = @{ Depth = 5 }

$localPath = Join-Path $apiDir 'appsettings.Local.json'
$runtimePath = Join-Path $feDir 'app-runtime-config.json'

$localJson | ConvertTo-Json @opts | Set-Content -Path $localPath -Encoding UTF8
$runtimeJson | ConvertTo-Json @opts | Set-Content -Path $runtimePath -Encoding UTF8

if (Test-Path $devPath) {
    $dev = Get-Content $devPath -Raw | ConvertFrom-Json
    if (-not $dev.Cors) { $dev | Add-Member -NotePropertyName Cors -NotePropertyValue (@{}) }
    if (-not $dev.Cors.AllowedOrigins) { $dev.Cors.AllowedOrigins = @() }

    $localhostPrefixes = @(
        'http://localhost:', 'https://localhost:',
        'http://127.0.0.1:', 'https://127.0.0.1:'
    )
    $kept = @($dev.Cors.AllowedOrigins | Where-Object {
        $o = $_
        ($localhostPrefixes | Where-Object { $o -like "$_*" }).Count -gt 0
    })
    $merged = @($kept) + @($newOrigins | Where-Object { $kept -notcontains $_ })
    $dev.Cors.AllowedOrigins = $merged | Select-Object -Unique
    $dev | ConvertTo-Json -Depth 10 | Set-Content -Path $devPath -Encoding UTF8
}

Write-Host "Saved:" -ForegroundColor Green
Write-Host "  $localPath"
Write-Host "  $runtimePath"
if (Test-Path $devPath) { Write-Host "  $devPath (CORS)" }
Write-Host ""
Write-Host "Restart the API and ng serve." -ForegroundColor Yellow
