# Build and run PosNetworkSetup (WinForms). Run from this folder or repo root.
$ErrorActionPreference = 'Stop'
$projectDir = $PSScriptRoot
$csproj = Join-Path $projectDir 'PosNetworkSetup.csproj'

function Find-DotNet {
    $candidates = @(
        "$env:ProgramFiles\dotnet\dotnet.exe",
        "${env:ProgramFiles(x86)}\dotnet\dotnet.exe"
    )
    foreach ($p in $candidates) {
        if (Test-Path $p) { return $p }
    }
    $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return $null
}

$dotnet = Find-DotNet
if (-not $dotnet) {
    Write-Host ''
    Write-Host 'ERROR: .NET SDK not found.' -ForegroundColor Red
    Write-Host 'Install .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0'
    Write-Host 'In Visual Studio Installer, also enable workload: ".NET desktop development"'
    Write-Host ''
    Write-Host 'Alternative (no build): run from repo root:'
    Write-Host '  powershell -ExecutionPolicy Bypass -File scripts\Set-PosLanConfig.ps1 -RepoRoot "C:\GITHUB\open-source-pos" -HostName "YOUR_IP"'
    Write-Host ''
    exit 1
}

Write-Host "Using: $dotnet" -ForegroundColor Cyan
& $dotnet --version

Write-Host "`nBuilding PosNetworkSetup..." -ForegroundColor Cyan
& $dotnet build $csproj -c Release -v minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host 'Build failed. Common fixes:' -ForegroundColor Yellow
    Write-Host '  1. Visual Studio Installer -> Modify -> check ".NET desktop development"'
    Write-Host '  2. Install .NET 8 SDK (not only ASP.NET runtime)'
    Write-Host '  3. Build this project only: dotnet build PosNetworkSetup\PosNetworkSetup.csproj'
    Write-Host ''
    exit $LASTEXITCODE
}

$exe = Join-Path $projectDir 'bin\Release\net8.0-windows\PosNetworkSetup.exe'
if (-not (Test-Path $exe)) {
    & $dotnet build $csproj -c Debug -v minimal
    $exe = Join-Path $projectDir 'bin\Debug\net8.0-windows\PosNetworkSetup.exe'
}
if (-not (Test-Path $exe)) {
    Write-Host 'Executable not found after build.' -ForegroundColor Red
    exit 1
}

Write-Host "`nStarting $exe`n" -ForegroundColor Green
Start-Process -FilePath $exe -WorkingDirectory $projectDir
