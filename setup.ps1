# ============================================================
#  setup.ps1 — one-shot prerequisite installer for FFBWheel
# ============================================================
#  Run this ONCE after cloning the repository.
#  It installs all required tools and configures arduino-cli.
#
#  Usage (from the repo root):
#    powershell -ExecutionPolicy Bypass -File setup.ps1
# ============================================================

param(
    [switch]$SkipGit,
    [switch]$SkipDotNet,
    [switch]$SkipArduino
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step([string]$msg) {
    Write-Host ""
    Write-Host "==== $msg ====" -ForegroundColor Cyan
    Write-Host ""
}

function Test-Command([string]$cmd) {
    return $null -ne (Get-Command $cmd -ErrorAction SilentlyContinue)
}

Write-Host ""
Write-Host "FFB Wheel — Setup Script" -ForegroundColor Green
Write-Host "This will install: Git, arduino-cli, .NET 8 SDK, and the AVR board core."
Write-Host ""

# ------------------------------------------------------------------
# 1. Git
# ------------------------------------------------------------------
if (-not $SkipGit) {
    Write-Step "Installing Git"

    if (Test-Command 'git') {
        Write-Host "Git is already installed: $(git --version)" -ForegroundColor Green
    } else {
        Write-Host "Installing Git via winget..."
        winget install --id Git.Git --accept-source-agreements --accept-package-agreements
        $env:PATH = [System.Environment]::GetEnvironmentVariable('PATH', 'Machine') + ';' +
                    [System.Environment]::GetEnvironmentVariable('PATH', 'User')
    }
}

# ------------------------------------------------------------------
# 2. arduino-cli
# ------------------------------------------------------------------
if (-not $SkipArduino) {
    Write-Step "Installing arduino-cli"

    if (Test-Command 'arduino-cli') {
        Write-Host "arduino-cli is already installed: $(arduino-cli version)" -ForegroundColor Green
    } else {
        Write-Host "Installing arduino-cli via winget..."
        winget install --id ArduinoSA.CLI --accept-source-agreements --accept-package-agreements
        # Refresh PATH so the new binary is available in the same session
        $env:PATH = [System.Environment]::GetEnvironmentVariable('PATH', 'Machine') + ';' +
                    [System.Environment]::GetEnvironmentVariable('PATH', 'User')
    }

    Write-Step "Installing AVR board core (arduino:avr)"
    arduino-cli core update-index
    arduino-cli core install arduino:avr
    Write-Host "AVR core installed." -ForegroundColor Green
}

# ------------------------------------------------------------------
# 3. .NET 8 SDK
# ------------------------------------------------------------------
if (-not $SkipDotNet) {
    Write-Step "Installing .NET 8 SDK"

    $dotnetOk = $false
    if (Test-Command 'dotnet') {
        $ver = (dotnet --version 2>$null)
        if ($ver -match '^8\.') {
            Write-Host ".NET 8 SDK already installed: $ver" -ForegroundColor Green
            $dotnetOk = $true
        } else {
            Write-Host "Found dotnet $ver — a .NET 8 SDK is also needed."
        }
    }

    if (-not $dotnetOk) {
        Write-Host "Installing .NET 8 SDK via winget..."
        winget install --id Microsoft.DotNet.SDK.8 --accept-source-agreements --accept-package-agreements
        $env:PATH = [System.Environment]::GetEnvironmentVariable('PATH', 'Machine') + ';' +
                    [System.Environment]::GetEnvironmentVariable('PATH', 'User')
    }
}

# ------------------------------------------------------------------
# Done
# ------------------------------------------------------------------
Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host " Setup complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:"
Write-Host ""
Write-Host "  Option A — run all remaining steps in one command (recommended):"
Write-Host "       powershell -ExecutionPolicy Bypass -File install.ps1 -SkipGit -SkipArduino -SkipDotNet"
Write-Host ""
Write-Host "  Option B — run each step individually:"
Write-Host "    1. Build the firmware:"
Write-Host "         powershell -ExecutionPolicy Bypass -File firmware\leonardo-wheel\build_firmware.ps1"
Write-Host ""
Write-Host "    2. Flash the firmware (replace COM4 with your port):"
Write-Host "         powershell -ExecutionPolicy Bypass -File firmware\leonardo-wheel\flash_firmware.ps1 -Port COM4"
Write-Host ""
Write-Host "    3. Build and run the desktop app:"
Write-Host "         powershell -ExecutionPolicy Bypass -File desktop-app\run_desktop_app.ps1"
Write-Host ""
