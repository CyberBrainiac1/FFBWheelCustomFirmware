#!/usr/bin/env bash
# build_firmware.sh — compile LeonardoWheel firmware using arduino-cli
#
# Prerequisites:
#   arduino-cli installed and on PATH
#   Arduino AVR core:  arduino-cli core install arduino:avr
#
# Usage:  bash build_firmware.sh

set -e

SKETCH_DIR="$(cd "$(dirname "$0")" && pwd)"
BUILD_DIR="$SKETCH_DIR/build"

echo "[build] Sketch : $SKETCH_DIR"
echo "[build] Output : $BUILD_DIR"

arduino-cli compile \
    --fqbn arduino:avr:leonardo \
    --output-dir "$BUILD_DIR" \
    "$SKETCH_DIR"

HEX="$BUILD_DIR/LeonardoWheel.ino.hex"
if [ -f "$HEX" ]; then
    echo "[build] SUCCESS — $HEX"
else
    echo "[build] ERROR: hex not found at $HEX"
    exit 1
fi
