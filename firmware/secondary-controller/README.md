# Secondary Controller Firmware

Firmware for the optional pedal controller based on a **Seeed Studio XIAO RP2040**.

---

## Hardware

| Signal | Pin |
|---|---|
| Throttle pedal (pot wiper) | A0 |
| Brake pedal (pot wiper) | A1 |
| Clutch pedal (pot wiper) | A2 |

Connect the outer legs of each potentiometer to **3.3 V** and **GND**.

---

## Output

The controller sends one line every 10 ms over USB CDC serial at **115 200 baud**:

```
PEDALS=<throttle>,<brake>,<clutch>
```

Values are raw 12-bit ADC counts (**0 – 4095**).

Example:

```
PEDALS=2048,0,4095
PEDALS=2060,120,4090
```

---

## Building and Flashing

### Option A — Arduino IDE

1. Install the **XIAO RP2040** board package:  
   Board Manager URL: `https://files.seeedstudio.com/arduino/package_seeeduino_boards_index.json`
2. Select **Seeed XIAO RP2040** as the target board.
3. Open `SecondaryController.ino` and click **Upload**.

### Option B — arduino-cli

```bash
# Install the Seeed board core (first time only)
arduino-cli core install Seeeduino:rp2040 \
    --additional-urls https://files.seeedstudio.com/arduino/package_seeeduino_boards_index.json

# Build
arduino-cli compile \
    --fqbn Seeeduino:rp2040:seeed_XIAO_rp2040 \
    --output-dir build \
    .

# Flash (replace /dev/ttyACM1 with your port)
arduino-cli upload \
    --fqbn Seeeduino:rp2040:seeed_XIAO_rp2040 \
    --port /dev/ttyACM1 \
    --input-dir build \
    .
```

---

## Adjusting ADC Resolution

The sketch calls `analogReadResolution(12)` for 12-bit values (0–4095).  
Remove that line or change it to `10` if you need 0–1023 compatibility.

---

## Notes

- If a pedal axis is not used, the corresponding value will still be reported (typically 0 or mid-scale, depending on wiring).
- The main Leonardo wheel controller does not depend on this secondary controller — they are independent USB HID devices.
