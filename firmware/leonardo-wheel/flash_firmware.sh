#!/usr/bin/env bash
# flash_firmware.sh — flash LeonardoWheel.ino.hex to an Arduino Leonardo
#
# Prerequisites:
#   avrdude installed and on PATH
#   Build the firmware first:  bash build_firmware.sh
#
# Usage:  bash flash_firmware.sh [PORT]
#   PORT defaults to /dev/ttyACM0
#
# The Leonardo bootloader uses the avr109 (Caterina) protocol at 57600 baud.
# Plug in the Leonardo and press the reset button before running this script
# if the board does not auto-reset.

set -e

PORT="${1:-/dev/ttyACM0}"
SKETCH_DIR="$(cd "$(dirname "$0")" && pwd)"
HEX="$SKETCH_DIR/build/LeonardoWheel.ino.hex"

if [ ! -f "$HEX" ]; then
    echo "[flash] ERROR: hex not found — run build_firmware.sh first"
    echo "        Expected: $HEX"
    exit 1
fi

echo "[flash] Port : $PORT"
echo "[flash] Hex  : $HEX"

avrdude \
    -p atmega32u4 \
    -c avr109 \
    -P "$PORT" \
    -b 57600 \
    -D \
    -U "flash:w:$HEX:i"

echo "[flash] Done."
