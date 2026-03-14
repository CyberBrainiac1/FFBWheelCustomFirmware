/*
 * secondary-controller.ino
 *
 * USB HID game controller for pedals and buttons.
 * Runs on a Seeed Studio XIAO RP2040 (or any RP2040 board).
 *
 * Appears as a USB gamepad with 3 axes (throttle, brake, clutch)
 * and 4 buttons.  No drivers needed — works out of the box on
 * Windows, Linux, and macOS.
 */

#include <Joystick.h>

// ---- Pin assignments (change to match your wiring) --------
static const int PIN_THROTTLE = A0;
static const int PIN_BRAKE    = A1;
static const int PIN_CLUTCH   = A2;

static const int BUTTON_PINS[] = { 2, 3, 4, 5 };
static const int NUM_BUTTONS   = sizeof(BUTTON_PINS) / sizeof(BUTTON_PINS[0]);

// ---- Setup ------------------------------------------------
void setup() {
    for (int i = 0; i < NUM_BUTTONS; i++) {
        pinMode(BUTTON_PINS[i], INPUT_PULLUP);
    }

    Joystick.begin();
    Joystick.use10bit();        // axes accept 0-1023, matches analogRead
    Joystick.useManualSend(true);
}

// ---- Main loop --------------------------------------------
void loop() {
    Joystick.X(analogRead(PIN_THROTTLE));
    Joystick.Y(analogRead(PIN_BRAKE));
    Joystick.Z(analogRead(PIN_CLUTCH));

    for (int i = 0; i < NUM_BUTTONS; i++) {
        Joystick.button(i + 1, digitalRead(BUTTON_PINS[i]) == LOW);
    }

    Joystick.send_now();
    delay(10); // ~100 Hz update rate
}
