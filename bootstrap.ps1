# ============================================================
#  bootstrap.ps1 — fresh-machine one-shot installer
# ============================================================
#  Run this on a machine that does NOT yet have the repo.
#  It installs Git (if needed), clones the repository, and
#  then runs install.ps1 to do everything else automatically.
#
#  One-liner (paste into any PowerShell window):
#    irm https://raw.githubusercontent.com/CyberBrainiac1/FFBWheelCustomFirmware/main/bootstrap.ps1 | iex
#
#  To pass extra flags (e.g. a specific COM port or skip steps),
#  download and run directly:
#    Invoke-WebRequest -Uri https://raw.githubusercontent.com/CyberBrainiac1/FFBWheelCustomFirmware/main/bootstrap.ps1 -OutFile bootstrap.ps1
#    powershell -ExecutionPolicy Bypass -File bootstrap.ps1 -Port COM4 -SkipFlash
# ============================================================

param(
    [string]$Port          = '',
    [string]$InstallDir    = "$env:USERPROFILE\FFBWheelCustomFirmware",
    [switch]$SkipFlash,
    [switch]$SkipDesktopApp,
    [switch]$SkipDotNet,
    [switch]$SkipArduino
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  FFB Wheel — Bootstrap Installer" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Install directory: $InstallDir"
Write-Host ""

# ------------------------------------------------------------------
# Helper: refresh PATH in the current session after winget installs
# ------------------------------------------------------------------
function Update-SessionPath {
    $env:PATH = [System.Environment]::GetEnvironmentVariable('PATH', 'Machine') + ';' +
                [System.Environment]::GetEnvironmentVariable('PATH', 'User')
}

# ------------------------------------------------------------------
# Step 1 — Install Git if missing
# ------------------------------------------------------------------
Write-Host "Step 1/3  Checking for Git..." -ForegroundColor Cyan

if (Get-Command 'git' -ErrorAction SilentlyContinue) {
    Write-Host "Git is already installed: $(git --version)" -ForegroundColor Green
} else {
    Write-Host "Git not found. Installing via winget..." -ForegroundColor Yellow
    winget install --id Git.Git --accept-source-agreements --accept-package-agreements
    Update-SessionPath

    # winget modifies the system PATH; reload from registry so git is usable now
    if (-not (Get-Command 'git' -ErrorAction SilentlyContinue)) {
        # Common default install path as a fallback
        $gitPaths = @(
            "$env:ProgramFiles\Git\cmd",
            "$env:ProgramFiles\Git\bin",
            "${env:ProgramFiles(x86)}\Git\cmd"
        )
        foreach ($p in $gitPaths) {
            if (Test-Path (Join-Path $p 'git.exe')) {
                $env:PATH = "$p;$env:PATH"
                break
            }
        }
    }

    if (-not (Get-Command 'git' -ErrorAction SilentlyContinue)) {
        Write-Host ""
        Write-Host "ERROR: Git was installed but is not on PATH yet." -ForegroundColor Red
        Write-Host "Please close this window, open a NEW PowerShell session, and re-run:" -ForegroundColor Yellow
        Write-Host "  irm https://raw.githubusercontent.com/CyberBrainiac1/FFBWheelCustomFirmware/main/bootstrap.ps1 | iex" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "Git installed successfully." -ForegroundColor Green
}

# ------------------------------------------------------------------
# Step 2 — Clone (or update) the repository
# ------------------------------------------------------------------
Write-Host ""
Write-Host "Step 2/3  Setting up repository at $InstallDir..." -ForegroundColor Cyan

$RepoUrl = 'https://github.com/CyberBrainiac1/FFBWheelCustomFirmware.git'

if (Test-Path (Join-Path $InstallDir '.git')) {
    Write-Host "Repository already exists — pulling latest changes..." -ForegroundColor Yellow
    Push-Location $InstallDir
    git pull
    Pop-Location
} else {
    if (Test-Path $InstallDir) {
        Write-Host "Directory exists but is not a git repo — cloning into it..." -ForegroundColor Yellow
        git clone $RepoUrl $InstallDir
    } else {
        Write-Host "Cloning $RepoUrl ..." -ForegroundColor Yellow
        git clone $RepoUrl $InstallDir
    }
    Write-Host "Repository cloned." -ForegroundColor Green
}

# ------------------------------------------------------------------
# Step 3 — Run install.ps1 from the cloned repo
# ------------------------------------------------------------------
Write-Host ""
Write-Host "Step 3/3  Running full installer..." -ForegroundColor Cyan

$installScript = Join-Path $InstallDir 'install.ps1'

if (-not (Test-Path $installScript)) {
    Write-Host "ERROR: install.ps1 not found at $installScript" -ForegroundColor Red
    exit 1
}

$installArgs = @()
$installArgs += '-SkipGit'          # bootstrap already installed / verified Git above
if ($Port -ne '')      { $installArgs += @('-Port', $Port) }
if ($SkipFlash)        { $installArgs += '-SkipFlash' }
if ($SkipDesktopApp)   { $installArgs += '-SkipDesktopApp' }
if ($SkipDotNet)       { $installArgs += '-SkipDotNet' }
if ($SkipArduino)      { $installArgs += '-SkipArduino' }

& $installScript @installArgs

# ------------------------------------------------------------------
# Done
# ------------------------------------------------------------------
Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  Bootstrap complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Repository is at: $InstallDir"
Write-Host ""
Write-Host "To launch the desktop app in future:"
Write-Host "  powershell -ExecutionPolicy Bypass -File `"$InstallDir\desktop-app\run_desktop_app.ps1`""
Write-Host ""
