@echo off
REM ============================================================
REM  Build LeonardoWheel firmware into a .hex using arduino-cli
REM ============================================================
REM  Prerequisites:
REM    1. Install arduino-cli  (https://arduino.github.io/arduino-cli/)
REM    2. Run once:  arduino-cli core install arduino:avr
REM ============================================================

setlocal

for %%I in ("%~dp0.") do set "SKETCH_DIR=%%~fI"
set BUILD_DIR=%SKETCH_DIR%\build
set LIB_DIR=%SKETCH_DIR%\..\third_party\ArduinoJoystickWithFFBLibrary
set FQBN=arduino:avr:leonardo

echo.
echo ---- Building LeonardoWheel firmware ----
echo.

if not exist "%LIB_DIR%\library.properties" (
    echo Required library not found: %LIB_DIR%
    echo Clone YukMingLaw/ArduinoJoystickWithFFBLibrary into firmware\third_party first.
    exit /b 1
)

arduino-cli compile --fqbn %FQBN% --library "%LIB_DIR%" --output-dir "%BUILD_DIR%" "%SKETCH_DIR%"

if %ERRORLEVEL% neq 0 (
    echo.
    echo BUILD FAILED.
    exit /b 1
)

echo.
echo Build succeeded.  Output in: %BUILD_DIR%
echo Hex file: %BUILD_DIR%\leonardo-wheel.ino.hex
echo.
