@echo off
REM build_installer.bat  (lives in the installer\ directory)
REM Publishes the app and builds the Inno Setup installer.
REM Requires:
REM   - .NET 8 SDK
REM   - Inno Setup 6.x (iscc on PATH, or installed at default location)

setlocal enabledelayedexpansion

REM INSTALLER_DIR = the installer\ folder; REPO_ROOT = one level up
set "INSTALLER_DIR=%~dp0"
set "REPO_ROOT=%~dp0..\"

echo.
echo Step 1: Publishing FFB Wheel Tester app...
echo.
dotnet publish "%REPO_ROOT%wheel-ffb-tester\FFBWheelTester.csproj" ^
    -c Release -r win-x64 --self-contained true ^
    -o "%INSTALLER_DIR%publish"

if %ERRORLEVEL% neq 0 (
    echo ERROR: dotnet publish failed.
    exit /b 1
)

echo.
echo Step 2: Building Inno Setup installer...
echo.

REM Try iscc on PATH first
where iscc >nul 2>&1
if %ERRORLEVEL%==0 (
    iscc "%INSTALLER_DIR%setup.iss"
    goto :done
)

REM Try default Inno Setup install location
set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if exist "%ISCC%" (
    "%ISCC%" "%INSTALLER_DIR%setup.iss"
    goto :done
)

echo ERROR: Inno Setup (iscc.exe) not found.
echo Download from: https://jrsoftware.org/isdl.php
exit /b 1

:done
if %ERRORLEVEL%==0 (
    echo.
    echo Installer created: installer\FFBWheelTester-Setup.exe
) else (
    echo ERROR: Inno Setup build failed.
)

endlocal
