# flash_firmware.ps1 — flash LeonardoWheel.ino.hex to an Arduino Leonardo
#
# Prerequisites:
#   avrdude installed and on PATH  (bundled with Arduino IDE, or install separately)
#   Build the firmware first:  .\build_firmware.ps1
#
# Usage:  .\flash_firmware.ps1 [-Port COM3]
#
# The Leonardo bootloader uses the avr109 (Caterina) protocol at 57600 baud.

param(
    [string]$Port = 'COM3'
)

$ErrorActionPreference = 'Stop'

$SketchDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Hex = Join-Path $SketchDir 'build\LeonardoWheel.ino.hex'

if (-not (Test-Path $Hex)) {
    Write-Error "[flash] ERROR: hex not found — run build_firmware.ps1 first.`n        Expected: $Hex"
}

Write-Host "[flash] Port : $Port"
Write-Host "[flash] Hex  : $Hex"

avrdude `
    -p atmega32u4 `
    -c avr109 `
    -P $Port `
    -b 57600 `
    -D `
    -U "flash:w:${Hex}:i"

Write-Host "[flash] Done."
