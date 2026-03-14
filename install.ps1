# ============================================================
#  install.ps1 — complete one-shot installer for FFB Wheel
# ============================================================
#  Runs ALL installation steps in order:
#    1. Install prerequisites  (arduino-cli, .NET 8 SDK, AVR core)
#    2. Build firmware          (compiles LeonardoWheel.ino → .hex)
#    3. Flash firmware          (uploads .hex to the Arduino Leonardo)
#    4. Build desktop app       (restores NuGet packages + Release build)
#
#  Usage (from the repo root, after cloning):
#    powershell -ExecutionPolicy Bypass -File install.ps1
#
#  Optional parameters:
#    -Port COM4          COM port for the Arduino (auto-detected if omitted)
#    -SkipFlash          Skip the firmware flash step
#    -SkipDesktopApp     Skip building the desktop configuration app
#    -SkipDotNet         Skip .NET 8 SDK installation
#    -SkipArduino        Skip arduino-cli / AVR core installation
# ============================================================

param(
    [string]$Port          = '',
    [switch]$SkipFlash,
    [switch]$SkipDesktopApp,
    [switch]$SkipDotNet,
    [switch]$SkipArduino
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  FFB Wheel — Full Installation" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""

# ------------------------------------------------------------------
# Step 1 — Install prerequisites
# ------------------------------------------------------------------
Write-Host "Step 1/4  Installing prerequisites..." -ForegroundColor Cyan

$setupArgs = @()
if ($SkipDotNet)  { $setupArgs += '-SkipDotNet' }
if ($SkipArduino) { $setupArgs += '-SkipArduino' }

& "$RepoRoot\setup.ps1" @setupArgs

# ------------------------------------------------------------------
# Step 2 — Build firmware
# ------------------------------------------------------------------
Write-Host ""
Write-Host "Step 2/4  Building firmware..." -ForegroundColor Cyan

& "$RepoRoot\firmware\leonardo-wheel\build_firmware.ps1"

# ------------------------------------------------------------------
# Step 3 — Flash firmware
# ------------------------------------------------------------------
if (-not $SkipFlash) {
    Write-Host ""
    Write-Host "Step 3/4  Flashing firmware..." -ForegroundColor Cyan

    $flashArgs = @()
    if ($Port -ne '') { $flashArgs += @('-Port', $Port) }

    & "$RepoRoot\firmware\leonardo-wheel\flash_firmware.ps1" @flashArgs
} else {
    Write-Host ""
    Write-Host "Step 3/4  Skipping firmware flash (-SkipFlash was specified)." -ForegroundColor Yellow
    Write-Host "         Flash manually when ready:" -ForegroundColor Yellow
    Write-Host "           powershell -ExecutionPolicy Bypass -File firmware\leonardo-wheel\flash_firmware.ps1" -ForegroundColor Yellow
}

# ------------------------------------------------------------------
# Step 4 — Build desktop app
# ------------------------------------------------------------------
if (-not $SkipDesktopApp) {
    Write-Host ""
    Write-Host "Step 4/4  Building desktop app..." -ForegroundColor Cyan

    & "$RepoRoot\desktop-app\build_desktop_app.ps1"
} else {
    Write-Host ""
    Write-Host "Step 4/4  Skipping desktop app build (-SkipDesktopApp was specified)." -ForegroundColor Yellow
    Write-Host "         Build manually when ready:" -ForegroundColor Yellow
    Write-Host "           powershell -ExecutionPolicy Bypass -File desktop-app\build_desktop_app.ps1" -ForegroundColor Yellow
}

# ------------------------------------------------------------------
# Done
# ------------------------------------------------------------------
Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  Installation complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Launch the desktop configuration app:"
Write-Host "  powershell -ExecutionPolicy Bypass -File desktop-app\run_desktop_app.ps1"
Write-Host ""
