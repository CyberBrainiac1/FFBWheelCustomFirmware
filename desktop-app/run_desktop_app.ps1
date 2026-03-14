# ============================================================
#  run_desktop_app.ps1 — build (if needed) and launch FFBWheelConfig
# ============================================================
#  Prerequisites: .NET 8 SDK installed.
#  Run setup.ps1 from the repo root if you haven't already.
#
#  Usage (from any directory):
#    powershell -ExecutionPolicy Bypass -File desktop-app\run_desktop_app.ps1
# ============================================================

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectFile = Join-Path $ScriptDir 'FFBWheelConfig.csproj'
$ExePath     = Join-Path $ScriptDir 'bin\Release\net8.0-windows\FFBWheelConfig.exe'

if (-not (Get-Command 'dotnet' -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: dotnet not found." -ForegroundColor Red
    Write-Host "Run setup.ps1 from the repo root first:" -ForegroundColor Yellow
    Write-Host "  powershell -ExecutionPolicy Bypass -File setup.ps1" -ForegroundColor Yellow
    exit 1
}

# Build first if the exe doesn't exist yet
if (-not (Test-Path $ExePath)) {
    Write-Host ""
    Write-Host "Exe not found — building first..." -ForegroundColor Yellow
    & "$ScriptDir\build_desktop_app.ps1"
}

Write-Host ""
Write-Host "---- Launching FFBWheelConfig ----" -ForegroundColor Cyan
Write-Host ""

dotnet run --project $ProjectFile --configuration Release
