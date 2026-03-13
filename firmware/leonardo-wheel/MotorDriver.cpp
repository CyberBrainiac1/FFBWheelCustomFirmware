#include "MotorDriver.h"

static const uint8_t PIN_RPWM = 9;   // positive / right
static const uint8_t PIN_LPWM = 10;  // negative / left
static const uint8_t PIN_EN   = 8;   // R_EN and L_EN tied together

void motorInit() {
    pinMode(PIN_RPWM, OUTPUT);
    pinMode(PIN_LPWM, OUTPUT);
    pinMode(PIN_EN,   OUTPUT);
    analogWrite(PIN_RPWM, 0);
    analogWrite(PIN_LPWM, 0);
    digitalWrite(PIN_EN, HIGH);       // enable driver
}

void motorSetForce(int force) {
    force = constrain(force, -255, 255);

    if (force > 0) {
        analogWrite(PIN_LPWM, 0);
        analogWrite(PIN_RPWM, (uint8_t)force);
    } else if (force < 0) {
        analogWrite(PIN_RPWM, 0);
        analogWrite(PIN_LPWM, (uint8_t)(-force));
    } else {
        analogWrite(PIN_RPWM, 0);
        analogWrite(PIN_LPWM, 0);
    }
}
