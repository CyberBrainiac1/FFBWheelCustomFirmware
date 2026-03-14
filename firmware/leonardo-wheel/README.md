# Leonardo Wheel Firmware

Arduino Leonardo firmware for a DIY force-feedback steering wheel.

This build exposes two USB paths at once:
- a HID force-feedback wheel interface for games
- a USB CDC serial configuration channel for the desktop app

Designed for the ATmega32u4: integer-only math, no Arduino String class,
compact EEPROM layout with checksum validation. Target flash usage < 20 KB.

## Hardware

| Component            | Connection                                             |
| -------------------- | ------------------------------------------------------ |
| Quadrature encoder   | Channel A → D2, Channel B → D3                         |
| BTS7960 motor driver | RPWM → D9, LPWM → D10, R_EN+L_EN → D8                  |
| USB                  | Connects to PC for serial config + HID game controller |

Encoder spec: 600 PPR (2 400 counts/revolution in quadrature mode).

## Source files

| File                    | Purpose                                            |
| ----------------------- | -------------------------------------------------- |
| `leonardo-wheel.ino`    | Main sketch – setup, 1 kHz motor loop              |
| `EncoderReader.h/.cpp`  | Interrupt-driven quadrature encoder                |
| `MotorDriver.h/.cpp`    | BTS7960 PWM motor control                          |
| `WheelSettings.h/.cpp`  | Settings struct with defaults                      |
| `EepromStorage.h/.cpp`  | EEPROM save/load with magic + version + checksum   |
| `SerialProtocol.h/.cpp` | Text-based serial command interface                |
| `WheelMath.h/.cpp`      | Integer angle conversion and FFB force calculation |

## Building

### Prerequisites

1. Install [arduino-cli](https://arduino.github.io/arduino-cli/installation/)
2. Install the AVR core:

```
arduino-cli core install arduino:avr
```

3. Ensure the vendored FFB joystick library exists at `../third_party/ArduinoJoystickWithFFBLibrary/`.
   The repo now includes that library for reproducible builds.

### Compile

**Windows:**

```
build_firmware.bat
```

**Linux / macOS:**

```
./build_firmware.sh
```

The compiled `.hex` file will be in the `build/` folder.
The build scripts automatically add the vendored FFB joystick library.

### Flash

**Windows:**

```
flash_firmware.bat COM4
```

**Linux / macOS:**

```
./flash_firmware.sh /dev/ttyACM0
```

Replace `COM4` or `/dev/ttyACM0` with your Leonardo's actual port.

## Serial protocol

Baud rate: **115 200**, 8N1.

The desktop app communicates with this firmware using a simple text protocol. See the [project README](../../README.md) for full protocol details.

### Commands

| Command             | Response                                       |
| ------------------- | ---------------------------------------------- |
| `GET_SETTINGS`      | `BEGIN_SETTINGS` … `END_SETTINGS` block        |
| `GET_LIVE_STATE`    | `BEGIN_LIVE` … `END_LIVE` block                |
| `SET FORCE 60`      | `OK`                                           |
| `SET MIN_FORCE 5`   | `OK`                                           |
| `SET DAMPING 10`    | `OK`                                           |
| `SET FRICTION 4`    | `OK`                                           |
| `SET SPRING 15`     | `OK`                                           |
| `SET RANGE 900`     | `OK`                                           |
| `SET CENTER 12345`  | `OK`                                           |
| `SET INV_ENCODER 1` | `OK`                                           |
| `SET INV_MOTOR 0`   | `OK`                                           |
| `SET_CENTER_NOW`    | `OK` (sets center to current encoder position) |
| `APPLY`             | `OK` (copies pending → active)                 |
| `SAVE`              | `OK` (persists active settings to EEPROM)      |
| `LOAD_DEFAULTS`     | `OK` (resets to compiled defaults)             |
| _(unknown command)_ | `ERROR INVALID_COMMAND`                        |

### EEPROM layout

| Offset | Size | Field                                       |
| ------ | ---- | ------------------------------------------- |
| 0      | 4    | `uint32_t` magic (`0xFFB10001`)             |
| 4      | 2    | `uint16_t` version                          |
| 6      | N    | `WheelSettingsData` struct                  |
| 6+N    | 2    | `uint16_t` checksum (sum of settings bytes) |

## Default settings

| Setting        | Default |
| -------------- | ------- |
| Overall force  | 60 %    |
| Minimum force  | 5 %     |
| Damping        | 10 %    |
| Friction       | 4 %     |
| Spring         | 15 %    |
| Steering range | 900°    |
| Invert encoder | Off     |
| Invert motor   | Off     |

## Design notes

- **No floating point.** All angle and motor calculations use `int32_t`/`int16_t`.
- **No Arduino String class.** Fixed 64-byte `char` buffer for serial parsing.
- **All serial strings stored in flash** via the `F()` macro.
- **Velocity for damping** computed from raw encoder count delta (not integer
  degree delta) to preserve sub-degree precision at low speeds.
