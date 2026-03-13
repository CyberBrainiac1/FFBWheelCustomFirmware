# Leonardo Wheel Firmware

Firmware for the Arduino Leonardo FFB steering wheel controller.

---

## Hardware

| Signal | Pin |
|---|---|
| Encoder channel A | D2 (INT0) |
| Encoder channel B | D3 (INT1) |
| BTS7960 R_EN | D7 |
| BTS7960 L_EN | D8 |
| BTS7960 RPWM (forward) | D9 |
| BTS7960 LPWM (reverse) | D10 |

Serial baud rate: **115 200**  
MCU: ATmega32u4 (Arduino Leonardo)

---

## Prerequisites

### arduino-cli (build tool)

```
# Install arduino-cli (https://arduino.github.io/arduino-cli)
# Then install the AVR core:
arduino-cli core install arduino:avr
```

### avrdude (flash tool)

avrdude is bundled with the Arduino IDE (`hardware/tools/avr/bin/avrdude`).  
Alternatively, install it system-wide:

- **Windows**: [WinAVR](https://sourceforge.net/projects/winavr/) or via Arduino IDE
- **Linux/macOS**: `sudo apt install avrdude` / `brew install avrdude`

---

## Build

The compiled hex file is written to the `build/` sub-folder of this directory.

### Linux / macOS

```bash
bash build_firmware.sh
```

### Windows (CMD)

```cmd
build_firmware.bat
```

### Windows (PowerShell)

```powershell
.\build_firmware.ps1
```

### Manual arduino-cli command

```bash
arduino-cli compile \
    --fqbn arduino:avr:leonardo \
    --output-dir build \
    .
```

### Expected output

```
build/LeonardoWheel.ino.hex        ← flash this file
build/LeonardoWheel.ino.elf
build/LeonardoWheel.ino.with_bootloader.hex
```

The exact hex to flash is:

```
build/LeonardoWheel.ino.hex
```

---

## Flash

The Arduino Leonardo uses the **avr109** (Caterina) bootloader at **57600 baud**.

Before flashing, the Leonardo must be in bootloader mode.  
On most systems, `avrdude -c avr109` will auto-reset the board by briefly opening the
port at 1200 baud — the scripts do **not** do this step; just plug in the Leonardo and run:

### Linux / macOS

```bash
# Replace /dev/ttyACM0 with your actual port (check: ls /dev/ttyACM* or ls /dev/ttyUSB*)
bash flash_firmware.sh /dev/ttyACM0
```

### Windows (CMD)

```cmd
flash_firmware.bat COM3
```

### Windows (PowerShell)

```powershell
.\flash_firmware.ps1 -Port COM3
```

### Manual avrdude command

```bash
avrdude \
    -p atmega32u4 \
    -c avr109 \
    -P /dev/ttyACM0 \
    -b 57600 \
    -D \
    -U flash:w:build/LeonardoWheel.ino.hex:i
```

---

## Serial Protocol

Baud rate: **115 200 8N1**  
Line terminator: `\n` (LF) or `\r\n` (CRLF)

### Commands (PC → wheel)

| Command | Description |
|---|---|
| `GET_SETTINGS` | Return all settings as a block |
| `GET_LIVE_STATE` | Return current angle and raw counts |
| `SET FORCE <0-100>` | Overall force percentage |
| `SET MIN_FORCE <0-100>` | Minimum force |
| `SET DAMPING <0-100>` | Damping level |
| `SET FRICTION <0-100>` | Friction level |
| `SET SPRING <0-100>` | Spring level |
| `SET RANGE <90-1800>` | Steering range in degrees |
| `SET INV_ENCODER <0\|1>` | Invert encoder direction |
| `SET INV_MOTOR <0\|1>` | Invert motor direction |
| `APPLY` | Acknowledge pending settings (returns `OK_APPLIED`) |
| `SAVE` | Persist current settings to EEPROM |
| `LOAD_DEFAULTS` | Reset to defaults, save, and reply with full settings block |
| `SET_CENTER` | Set current wheel position as steering centre |

### Responses (wheel → PC)

Settings block:
```
BEGIN_SETTINGS
FORCE=60
MIN_FORCE=5
DAMPING=10
FRICTION=4
SPRING=15
RANGE=900
CENTER=0
INV_ENCODER=0
INV_MOTOR=0
FW_VERSION=1.0.0
END_SETTINGS
```

Live state block:
```
BEGIN_LIVE
LIVE_ANGLE=42
RAW_COUNTS=12054
END_LIVE
```

---

## EEPROM Layout

Settings are stored from EEPROM address 0 using a packed struct:

| Field | Type | Default |
|---|---|---|
| magic | uint16 | 0xAB12 |
| version | uint8 | 1 |
| range | uint16 | 900 |
| center | int32 | 0 |
| invEncoder | uint8 | 0 |
| invMotor | uint8 | 0 |
| force | uint8 | 60 |
| minForce | uint8 | 5 |
| damping | uint8 | 10 |
| friction | uint8 | 4 |
| spring | uint8 | 15 |
| checksum | uint8 | XOR of all above |

If magic, version, or checksum do not match on boot, defaults are loaded and saved automatically.

---

## Encoder Counts-per-Degree

The constant `COUNTS_PER_DEG` in `EncoderReader.h` controls scaling:

```cpp
#define COUNTS_PER_DEG 4   // default — suits 360 PPR quadrature encoders
```

| Encoder PPR | Quadrature CPR | Counts per degree |
|---|---|---|
| 100 | 400 | 1.1 → use 1 |
| 360 | 1440 | 4.0 → use 4 |
| 600 | 2400 | 6.7 → use 6 or 7 |

Adjust `COUNTS_PER_DEG` to match your hardware, then rebuild.
