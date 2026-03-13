@echo off
REM flash_firmware.bat — flash LeonardoWheel.ino.hex to an Arduino Leonardo
REM
REM Prerequisites:
REM   avrdude installed and on PATH  (bundled with Arduino IDE, or install separately)
REM   Build the firmware first:  build_firmware.bat
REM
REM Usage:  flash_firmware.bat [COM_PORT]
REM   COM_PORT defaults to COM3
REM
REM The Leonardo bootloader uses the avr109 (Caterina) protocol at 57600 baud.

setlocal

set PORT=%1
if "%PORT%"=="" set PORT=COM3

set SKETCH_DIR=%~dp0
set HEX=%SKETCH_DIR%build\LeonardoWheel.ino.hex

if not exist "%HEX%" (
    echo [flash] ERROR: hex not found — run build_firmware.bat first
    echo         Expected: %HEX%
    exit /b 1
)

echo [flash] Port : %PORT%
echo [flash] Hex  : %HEX%

avrdude ^
    -p atmega32u4 ^
    -c avr109 ^
    -P %PORT% ^
    -b 57600 ^
    -D ^
    -U flash:w:"%HEX%":i

if %ERRORLEVEL% NEQ 0 (
    echo [flash] ERROR: flashing failed.
    exit /b 1
)

echo [flash] Done.
