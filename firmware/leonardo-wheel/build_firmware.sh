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
LIB_DIR="$SCRIPT_DIR/../third_party/ArduinoJoystickWithFFBLibrary"
FQBN="arduino:avr:leonardo"

echo
echo "---- Building LeonardoWheel firmware ----"
echo

if [ ! -f "$LIB_DIR/library.properties" ]; then
    echo "Required library not found: $LIB_DIR"
    echo "Clone YukMingLaw/ArduinoJoystickWithFFBLibrary into firmware/third_party first."
    exit 1
fi

arduino-cli compile --fqbn "$FQBN" --library "$LIB_DIR" --output-dir "$BUILD_DIR" "$SCRIPT_DIR"

echo
echo "Build succeeded.  Output in: $BUILD_DIR"
echo "Hex file: $BUILD_DIR/leonardo-wheel.ino.hex"
echo
