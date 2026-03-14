# ============================================================
#  build_firmware.ps1 — compile LeonardoWheel firmware
# ============================================================
#  Prerequisites: arduino-cli + arduino:avr core installed.
#  Run setup.ps1 from the repo root if you haven't already.
#
#  Usage (from any directory):
#    powershell -ExecutionPolicy Bypass -File build_firmware.ps1
# ============================================================

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BuildDir  = Join-Path $ScriptDir 'build'
$Fqbn      = 'arduino:avr:leonardo'

Write-Host ""
Write-Host "---- Building LeonardoWheel firmware ----" -ForegroundColor Cyan
Write-Host ""

if (-not (Get-Command 'arduino-cli' -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: arduino-cli not found." -ForegroundColor Red
    Write-Host "Run setup.ps1 from the repo root first:" -ForegroundColor Yellow
    Write-Host "  powershell -ExecutionPolicy Bypass -File setup.ps1" -ForegroundColor Yellow
    exit 1
}

arduino-cli compile --fqbn $Fqbn --output-dir $BuildDir $ScriptDir

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "BUILD FAILED." -ForegroundColor Red
    exit 1
}

$HexFile = Join-Path $BuildDir 'LeonardoWheel.ino.hex'
Write-Host ""
Write-Host "Build succeeded." -ForegroundColor Green
Write-Host "Output directory : $BuildDir"
Write-Host "Hex file         : $HexFile"
Write-Host ""
