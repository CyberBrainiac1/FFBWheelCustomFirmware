# Secondary Controller

Optional template for a secondary input device (pedals, buttons, shifter).

## Purpose

The main wheel controller is an Arduino Leonardo that handles the steering wheel, encoder, and force-feedback motor. This secondary controller is a **separate** microcontroller that provides additional inputs:

- Throttle, brake, and clutch pedals (analogue)
- Buttons (shifter paddles, handbrake, etc.)

## Suggested hardware

- Seeed Studio XIAO RP2040
- Arduino Pro Micro
- Any board that supports USB HID

## Getting started

1. Open `SecondaryController.ino` in the Arduino IDE (or use arduino-cli).
2. Change the pin numbers at the top to match your wiring.
3. Choose a board and upload.

The template prints pedal and button values over serial for testing. Replace the `Serial.print` section with a USB HID joystick library (e.g. [ArduinoJoystickLibrary](https://github.com/MHeironimus/ArduinoJoystickLibrary)) to make it appear as a game controller.

## Wiring example

| Signal | Pin |
|---|---|
| Throttle potentiometer | A0 |
| Brake potentiometer | A1 |
| Clutch potentiometer | A2 |
| Button 1 | D2 |
| Button 2 | D3 |
| Button 3 | D4 |
| Button 4 | D5 |

Connect each button between its pin and GND. Internal pull-ups are enabled in the sketch.
