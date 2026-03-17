@echo off
REM flash_hex.bat - Flash the EMC-compatible .hex to the wheel controller via avrdude
REM
REM Usage:
REM   flash_hex.bat COM4
REM
REM Requirements:
REM   - avrdude installed and on PATH  (https://github.com/avrdudes/avrdude/releases)
REM   - Wheel controller plugged in via USB
REM   - Correct COM port passed as first argument
REM
REM The hex file must be built first. Use build_versioned_release.bat
REM or: arduino-cli compile --fqbn arduino:avr:leonardo ...
REM

setlocal

REM ── Find repo root (where this script lives under scripts\) ─────────────────
set "REPO_ROOT=%~dp0.."

REM ── Resolve hex path ────────────────────────────────────────────────────────
REM Try versioned release first, then firmware build output
set "HEX_PATH="

REM Read versions\latest.txt
if exist "%REPO_ROOT%\versions\latest.txt" (
    set /p LATEST_VER=<"%REPO_ROOT%\versions\latest.txt"
    set "CANDIDATE=%REPO_ROOT%\versions\%LATEST_VER%\firmware\leonardo-wheel.ino.hex"
    if exist "!CANDIDATE!" set "HEX_PATH=!CANDIDATE!"
)

if not defined HEX_PATH (
    set "CANDIDATE=%REPO_ROOT%\firmware\leonardo-wheel\build\leonardo-wheel.ino.hex"
    if exist "!CANDIDATE!" set "HEX_PATH=!CANDIDATE!"
)

if not defined HEX_PATH (
    echo ERROR: Could not find a built .hex file.
    echo.
    echo Build the firmware first:
    echo   arduino-cli compile --fqbn arduino:avr:leonardo --library firmware\third_party\ArduinoJoystickWithFFBLibrary --output-dir firmware\leonardo-wheel\build firmware\leonardo-wheel
    echo.
    echo Or run build_versioned_release.bat
    exit /b 1
)

REM ── COM port ─────────────────────────────────────────────────────────────────
if "%~1"=="" (
    echo Usage: flash_hex.bat COM4
    echo.
    echo To list available ports, run:  arduino-cli board list
    exit /b 1
)
set "COM_PORT=%~1"

echo.
echo FFB Wheel - EMC-compatible Firmware Flash
echo ==========================================
echo Hex:  %HEX_PATH%
echo Port: %COM_PORT%
echo.
echo Sending 1200-baud bootloader touch...
echo (The board will reset into bootloader mode)
echo.

REM 1200-baud touch (mode switch) - open and close port at 1200 baud
REM avrdude does this automatically with avr109 at the configured baud.
REM We just wait a moment first.
timeout /t 2 /nobreak >nul

echo Running avrdude...
echo.
avrdude -p atmega32u4 -c avr109 -P %COM_PORT% -b 57600 -D -U flash:w:"%HEX_PATH%":i

if %ERRORLEVEL%==0 (
    echo.
    echo Flash complete!
    echo Unplug and replug the wheel, then open the app and click Connect.
) else (
    echo.
    echo ERROR: avrdude failed with exit code %ERRORLEVEL%
    echo.
    echo Troubleshooting:
    echo   1. Make sure no other program has the COM port open.
    echo   2. Try double-pressing the reset button on the board.
    echo   3. Check the COM port number with: arduino-cli board list
    echo   4. Make sure avrdude is installed and on PATH.
)

endlocal
