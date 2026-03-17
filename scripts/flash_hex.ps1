# flash_hex.ps1 - Flash the EMC-compatible .hex to the wheel controller via avrdude
#
# Usage:
#   .\flash_hex.ps1 -Port COM4
#
# Requirements:
#   - avrdude installed and on PATH  (https://github.com/avrdudes/avrdude/releases)
#   - Wheel controller plugged in via USB
#   - Correct COM port passed with -Port
#
# The hex file must be built first.  Run build_versioned_release.bat / .ps1
# or: arduino-cli compile --fqbn arduino:avr:leonardo ...

param(
    [Parameter(Mandatory = $true, HelpMessage = "COM port for the wheel (e.g. COM4)")]
    [string]$Port
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── Repo root (script lives under scripts\) ──────────────────────────────────
$RepoRoot = Split-Path $MyInvocation.MyCommand.Path -Parent | Split-Path -Parent

# ── Find hex file ─────────────────────────────────────────────────────────────
$HexPath = $null

$LatestTxt = Join-Path $RepoRoot 'versions\latest.txt'
if (Test-Path $LatestTxt) {
    $ver = (Get-Content $LatestTxt -Raw).Trim()
    $candidate = Join-Path $RepoRoot "versions\$ver\firmware\leonardo-wheel.ino.hex"
    if (Test-Path $candidate) { $HexPath = $candidate }
}

if (-not $HexPath) {
    $candidate = Join-Path $RepoRoot 'firmware\leonardo-wheel\build\leonardo-wheel.ino.hex'
    if (Test-Path $candidate) { $HexPath = $candidate }
}

if (-not $HexPath) {
    Write-Error @"
Could not find a built .hex file.

Build the firmware first:
  arduino-cli compile --fqbn arduino:avr:leonardo ``
    --library firmware\third_party\ArduinoJoystickWithFFBLibrary ``
    --output-dir firmware\leonardo-wheel\build ``
    firmware\leonardo-wheel

Or run build_versioned_release.bat
"@
    exit 1
}

Write-Host ""
Write-Host "FFB Wheel - EMC-compatible Firmware Flash" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Hex:  $HexPath"
Write-Host "Port: $Port"
Write-Host ""
Write-Host "Sending 1200-baud bootloader touch..." -ForegroundColor Yellow

# 1200-baud touch to reset Leonardo into bootloader
try {
    $sp = New-Object System.IO.Ports.SerialPort $Port, 1200
    $sp.DtrEnable = $true
    $sp.Open()
    Start-Sleep -Milliseconds 50
    $sp.Close()
    $sp.Dispose()
    Write-Host "Bootloader touch sent."
} catch {
    Write-Warning "Bootloader touch failed ($_) - continuing anyway."
}

Write-Host "Waiting for bootloader..." -ForegroundColor Yellow
$BootloaderDelaySeconds = 3
Start-Sleep -Seconds $BootloaderDelaySeconds

$AvrdudeArgs = @(
    '-p', 'atmega32u4',
    '-c', 'avr109',
    '-P', $Port,
    '-b', '57600',
    '-D',
    '-U', "flash:w:`"$HexPath`":i"
)

Write-Host "Running: avrdude $($AvrdudeArgs -join ' ')" -ForegroundColor Gray
Write-Host ""

try {
    & avrdude @AvrdudeArgs
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "Flash complete!" -ForegroundColor Green
        Write-Host "Unplug and replug the wheel, then open the app and click Connect."
    } else {
        Write-Error "avrdude failed with exit code $LASTEXITCODE"
    }
} catch {
    Write-Error "Could not run avrdude: $_`n`nIs avrdude installed and on PATH?"
}
