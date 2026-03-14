#!/usr/bin/env bash
# ============================================================
#  Build LeonardoWheel firmware into a .hex using arduino-cli
# ============================================================
#  Prerequisites:
#    1. Install arduino-cli  (https://arduino.github.io/arduino-cli/)
#    2. Run once:  arduino-cli core install arduino:avr
# ============================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
BUILD_DIR="$SCRIPT_DIR/build"
FQBN="arduino:avr:leonardo"

echo
echo "---- Building LeonardoWheel firmware ----"
echo

arduino-cli compile --fqbn "$FQBN" --output-dir "$BUILD_DIR" "$SCRIPT_DIR"

echo
echo "Build succeeded.  Output in: $BUILD_DIR"
echo "Hex file: $BUILD_DIR/LeonardoWheel.ino.hex"
echo
