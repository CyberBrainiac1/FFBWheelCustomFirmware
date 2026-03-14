# ============================================================
#  build_desktop_app.ps1 — restore & build FFBWheelConfig
# ============================================================
#  Prerequisites: .NET 8 SDK installed.
#  Run setup.ps1 from the repo root if you haven't already.
#
#  Usage (from any directory):
#    powershell -ExecutionPolicy Bypass -File desktop-app\build_desktop_app.ps1
# ============================================================

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectFile = Join-Path $ScriptDir 'FFBWheelConfig.csproj'

if (-not (Get-Command 'dotnet' -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: dotnet not found." -ForegroundColor Red
    Write-Host "Run setup.ps1 from the repo root first:" -ForegroundColor Yellow
    Write-Host "  powershell -ExecutionPolicy Bypass -File setup.ps1" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "---- Restoring NuGet packages ----" -ForegroundColor Cyan
dotnet restore $ProjectFile

if ($LASTEXITCODE -ne 0) {
    Write-Host "Restore FAILED." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "---- Building FFBWheelConfig (Release) ----" -ForegroundColor Cyan
dotnet build $ProjectFile --configuration Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "BUILD FAILED." -ForegroundColor Red
    exit 1
}

$ExePath = Join-Path $ScriptDir 'bin\Release\net8.0-windows\FFBWheelConfig.exe'
Write-Host ""
Write-Host "Build succeeded." -ForegroundColor Green
Write-Host "Executable: $ExePath"
Write-Host ""
Write-Host "To run the app:"
Write-Host "  powershell -ExecutionPolicy Bypass -File desktop-app\run_desktop_app.ps1"
Write-Host ""
