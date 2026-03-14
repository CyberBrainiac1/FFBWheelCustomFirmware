# ============================================================
#  flash_firmware.ps1 — flash LeonardoWheel.hex to Arduino Leonardo
# ============================================================
#  Usage:
#    powershell -ExecutionPolicy Bypass -File flash_firmware.ps1 -Port COM4
#
#  If -Port is omitted the script lists available boards and lets
#  you pick one interactively.
# ============================================================

param(
    [string]$Port = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BuildDir  = Join-Path $ScriptDir 'build'
$HexFile   = Join-Path $BuildDir 'LeonardoWheel.ino.hex'
$Fqbn      = 'arduino:avr:leonardo'

if (-not (Get-Command 'arduino-cli' -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: arduino-cli not found." -ForegroundColor Red
    Write-Host "Run setup.ps1 from the repo root first:" -ForegroundColor Yellow
    Write-Host "  powershell -ExecutionPolicy Bypass -File setup.ps1" -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $HexFile)) {
    Write-Host "Hex file not found: $HexFile" -ForegroundColor Red
    Write-Host "Build the firmware first:" -ForegroundColor Yellow
    Write-Host "  powershell -ExecutionPolicy Bypass -File firmware\leonardo-wheel\build_firmware.ps1" -ForegroundColor Yellow
    exit 1
}

# ------------------------------------------------------------------
# Port auto-detection / interactive selection
# ------------------------------------------------------------------
if ($Port -eq '') {
    Write-Host ""
    Write-Host "Detecting connected boards..." -ForegroundColor Cyan
    $boardList = arduino-cli board list 2>&1
    Write-Host $boardList

    # Try to pick the first Leonardo automatically
    $autoLine = ($boardList | Select-String 'Leonardo') | Select-Object -First 1
    if ($autoLine) {
        $Port = ($autoLine.Line -split '\s+')[0]
        Write-Host "Auto-detected Leonardo on $Port" -ForegroundColor Green
    } else {
        Write-Host ""
        $Port = Read-Host "Enter the COM port for your Arduino Leonardo (e.g. COM4)"
    }
}

if ($Port -eq '') {
    Write-Host "No port specified. Aborting." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "---- Flashing LeonardoWheel to $Port ----" -ForegroundColor Cyan
Write-Host ""

arduino-cli upload -p $Port --fqbn $Fqbn --input-dir $BuildDir

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "arduino-cli upload failed. Trying avrdude directly..." -ForegroundColor Yellow
    Write-Host "NOTE: You may need to press the reset button on the Leonardo first." -ForegroundColor Yellow
    Write-Host ""

    avrdude -v -p atmega32u4 -c avr109 -P $Port -b 57600 -D -U "flash:w:${HexFile}:i"

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "FLASH FAILED." -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "Flash complete." -ForegroundColor Green
Write-Host ""
