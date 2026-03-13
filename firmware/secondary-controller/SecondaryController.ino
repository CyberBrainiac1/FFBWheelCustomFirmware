/*
 * SecondaryController.ino
 *
 * Starter template for an optional secondary controller
 * (e.g. Seeed Studio XIAO RP2040) that provides extra inputs
 * such as pedals, buttons, or a shifter to the host PC.
 *
 * This file is a minimal skeleton.  Adapt the pin numbers,
 * axis count, and button count to your own hardware.
 */

// ---- Pin assignments (change to match your wiring) --------
static const int PIN_THROTTLE = A0;
static const int PIN_BRAKE    = A1;
static const int PIN_CLUTCH   = A2;

static const int BUTTON_PINS[] = { 2, 3, 4, 5 };
static const int NUM_BUTTONS   = sizeof(BUTTON_PINS) / sizeof(BUTTON_PINS[0]);

// ---- Setup ------------------------------------------------
void setup() {
    Serial.begin(115200);

    for (int i = 0; i < NUM_BUTTONS; i++) {
        pinMode(BUTTON_PINS[i], INPUT_PULLUP);
    }

    // If using a game-controller library (e.g. Joystick),
    // initialise it here.
}

// ---- Main loop --------------------------------------------
void loop() {
    // Read analogue axes (0-1023)
    int throttle = analogRead(PIN_THROTTLE);
    int brake    = analogRead(PIN_BRAKE);
    int clutch   = analogRead(PIN_CLUTCH);

    // Read buttons (active-low with pull-up)
    uint8_t buttons = 0;
    for (int i = 0; i < NUM_BUTTONS; i++) {
        if (digitalRead(BUTTON_PINS[i]) == LOW) {
            buttons |= (1 << i);
        }
    }

    // ---- Send to host ----
    // Option 1: Print to serial (for debugging)
    Serial.print("T="); Serial.print(throttle);
    Serial.print(" B="); Serial.print(brake);
    Serial.print(" C="); Serial.print(clutch);
    Serial.print(" BTN="); Serial.println(buttons, BIN);

    // Option 2: Use a HID game-controller library to send
    // axes and buttons directly as a USB joystick.
    // See: https://github.com/MHeironworkarounds/ArduinoJoystickLibrary

    delay(10); // ~100 Hz update rate
}
