@echo off
REM ============================================================
REM  Flash LeonardoWheel.hex to an Arduino Leonardo using avrdude
REM ============================================================
REM  Usage:  flash_firmware.bat COM4
REM
REM  The Leonardo uses the CDC bootloader which briefly appears
REM  on a different COM port after a 1200-baud touch.  If you
REM  use arduino-cli you can let it handle the reset for you:
REM
REM    arduino-cli upload -p COM4 --fqbn arduino:avr:leonardo ^
REM        --input-dir build
REM ============================================================

setlocal

set HEX=%~dp0build\LeonardoWheel.ino.hex
set PORT=%1

if "%PORT%"=="" (
    echo Usage: flash_firmware.bat COMx
    echo Example: flash_firmware.bat COM4
    exit /b 1
)

if not exist "%HEX%" (
    echo Hex file not found: %HEX%
    echo Run build_firmware.bat first.
    exit /b 1
)

echo.
echo ---- Flashing LeonardoWheel to %PORT% ----
echo.

REM Preferred method: let arduino-cli handle the 1200-baud reset.
arduino-cli upload -p %PORT% --fqbn arduino:avr:leonardo --input-dir "%~dp0build"

if %ERRORLEVEL% neq 0 (
    echo.
    echo FLASH FAILED.  Trying avrdude directly...
    echo NOTE: You may need to manually reset the Leonardo first.
    echo.

    avrdude -v -p atmega32u4 -c avr109 -P %PORT% -b 57600 -D -U flash:w:"%HEX%":i

    if %ERRORLEVEL% neq 0 (
        echo.
        echo FLASH FAILED.
        exit /b 1
    )
)

echo.
echo Flash complete.
echo.
