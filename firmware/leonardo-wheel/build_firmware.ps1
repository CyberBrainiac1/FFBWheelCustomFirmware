# build_firmware.ps1 — compile LeonardoWheel firmware using arduino-cli
#
# Prerequisites:
#   arduino-cli installed and on PATH
#   Arduino AVR core:  arduino-cli core install arduino:avr
#
# Usage:  .\build_firmware.ps1

$ErrorActionPreference = 'Stop'

$SketchDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BuildDir  = Join-Path $SketchDir 'build'

Write-Host "[build] Sketch : $SketchDir"
Write-Host "[build] Output : $BuildDir"

arduino-cli compile `
    --fqbn arduino:avr:leonardo `
    --output-dir $BuildDir `
    $SketchDir

$Hex = Join-Path $BuildDir 'LeonardoWheel.ino.hex'
if (Test-Path $Hex) {
    Write-Host "[build] SUCCESS — $Hex"
} else {
    Write-Error "[build] ERROR: hex not found at $Hex"
}
