@echo off
REM ============================================================
REM  Build LeonardoWheel firmware into a .hex using arduino-cli
REM ============================================================
REM  Prerequisites:
REM    1. Install arduino-cli  (https://arduino.github.io/arduino-cli/)
REM    2. Run once:  arduino-cli core install arduino:avr
REM ============================================================

setlocal

set SKETCH_DIR=%~dp0
set BUILD_DIR=%SKETCH_DIR%build
set FQBN=arduino:avr:leonardo

echo.
echo ---- Building LeonardoWheel firmware ----
echo.

arduino-cli compile --fqbn %FQBN% --output-dir "%BUILD_DIR%" "%SKETCH_DIR%"

if %ERRORLEVEL% neq 0 (
    echo.
    echo BUILD FAILED.
    exit /b 1
)

echo.
echo Build succeeded.  Output in: %BUILD_DIR%
echo Hex file: %BUILD_DIR%\LeonardoWheel.ino.hex
echo.
