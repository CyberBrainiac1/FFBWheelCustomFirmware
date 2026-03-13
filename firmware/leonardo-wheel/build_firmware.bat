@echo off
REM build_firmware.bat — compile LeonardoWheel firmware using arduino-cli
REM
REM Prerequisites:
REM   arduino-cli installed and on PATH
REM   Arduino AVR core:  arduino-cli core install arduino:avr
REM
REM Usage:  build_firmware.bat

setlocal

set SKETCH_DIR=%~dp0
set BUILD_DIR=%SKETCH_DIR%build

echo [build] Sketch : %SKETCH_DIR%
echo [build] Output : %BUILD_DIR%

arduino-cli compile ^
    --fqbn arduino:avr:leonardo ^
    --output-dir "%BUILD_DIR%" ^
    "%SKETCH_DIR%"

if %ERRORLEVEL% NEQ 0 (
    echo [build] ERROR: compilation failed.
    exit /b 1
)

set HEX=%BUILD_DIR%\LeonardoWheel.ino.hex
if exist "%HEX%" (
    echo [build] SUCCESS — %HEX%
) else (
    echo [build] ERROR: hex not found at %HEX%
    exit /b 1
)
