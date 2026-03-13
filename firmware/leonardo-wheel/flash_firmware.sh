#!/usr/bin/env bash
# ============================================================
#  Flash LeonardoWheel.hex to an Arduino Leonardo using avrdude
# ============================================================
#  Usage:  ./flash_firmware.sh /dev/ttyACM0
#
#  The Leonardo uses the CDC bootloader which briefly appears
#  on a different serial port after a 1200-baud touch.
#  arduino-cli handles this automatically.
# ============================================================

set -e

HEX_DIR="$(cd "$(dirname "$0")" && pwd)/build"
HEX="$HEX_DIR/LeonardoWheel.ino.hex"
PORT="${1:-/dev/ttyACM0}"

if [ ! -f "$HEX" ]; then
    echo "Hex file not found: $HEX"
    echo "Run build_firmware.sh first."
    exit 1
fi

echo
echo "---- Flashing LeonardoWheel to $PORT ----"
echo

# Preferred method: let arduino-cli handle the 1200-baud reset.
if command -v arduino-cli &> /dev/null; then
    arduino-cli upload -p "$PORT" --fqbn arduino:avr:leonardo \
        --input-dir "$HEX_DIR"
else
    echo "arduino-cli not found, falling back to avrdude..."
    echo "NOTE: You may need to manually reset the Leonardo first."
    avrdude -v -p atmega32u4 -c avr109 -P "$PORT" -b 57600 \
        -D -U flash:w:"$HEX":i
fi

echo
echo "Flash complete."
echo
