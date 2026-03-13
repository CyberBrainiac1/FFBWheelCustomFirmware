# Leonardo Wheel Firmware

Arduino Leonardo firmware for a DIY force-feedback steering wheel.

## Hardware

| Component | Connection |
|---|---|
| Quadrature encoder | Channel A → D2, Channel B → D3 |
| BTS7960 motor driver | RPWM → D9, LPWM → D10, R_EN+L_EN → D8 |
| USB | Connects to PC for serial config + HID game controller |

Encoder spec: 600 PPR (2 400 counts/revolution in quadrature mode).

## Source files

| File | Purpose |
|---|---|
| `LeonardoWheel.ino` | Main sketch – setup, 1 kHz motor loop |
| `EncoderReader.h/.cpp` | Interrupt-driven quadrature encoder |
| `MotorDriver.h/.cpp` | BTS7960 PWM motor control |
| `WheelSettings.h/.cpp` | Settings struct with defaults |
| `EepromStorage.h/.cpp` | EEPROM save/load with magic-byte check |
| `SerialProtocol.h/.cpp` | Text-based serial command interface |
| `WheelMath.h/.cpp` | Angle conversion and FFB force calculation |

## Building

### Prerequisites

1. Install [arduino-cli](https://arduino.github.io/arduino-cli/installation/)
2. Install the AVR core:

```
arduino-cli core install arduino:avr
```

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

### Quick reference

| Command | Response |
|---|---|
| `GET_SETTINGS` | `BEGIN_SETTINGS` … `END_SETTINGS` block |
| `GET_LIVE_STATE` | `BEGIN_LIVE` … `END_LIVE` block |
| `SET FORCE 60` | *(none – applied on `APPLY`)* |
| `APPLY` | Applies pending settings |
| `SAVE` | Persists to EEPROM |
| `LOAD_DEFAULTS` | Resets to compiled defaults |
| `SET_CENTER` | Saves current encoder position as centre |

## Default settings

| Setting | Default |
|---|---|
| Overall force | 60 % |
| Minimum force | 5 % |
| Damping | 10 % |
| Friction | 4 % |
| Spring | 15 % |
| Steering range | 900° |
| Invert encoder | Off |
| Invert motor | Off |
