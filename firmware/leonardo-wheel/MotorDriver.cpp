#include "MotorDriver.h"

/*
 * BTS7960 motor driver via Timer1 Phase & Frequency Correct PWM.
 *
 * D9  = OC1A = RPWM (one direction)
 * D10 = OC1B = LPWM (other direction)
 * D8  = R_EN / L_EN (tied together, set HIGH)
 * D11 = additional enable (set HIGH, matching EMC)
 *
 * Timer1 config: Phase & Frequency Correct, ICR1 = 500, no prescaler.
 * PWM frequency = 16 MHz / (2 * 500) = 16 kHz  (matches EMCLite)
 * Duty cycle range: 0..500
 */

static const uint8_t PIN_RPWM = 9;
static const uint8_t PIN_LPWM = 10;
static const uint8_t PIN_EN1  = 8;
static const uint8_t PIN_EN2  = 11;

static const uint16_t PWM_TOP = 500;

void motorInit() {
    pinMode(PIN_RPWM, OUTPUT);
    pinMode(PIN_LPWM, OUTPUT);
    pinMode(PIN_EN1,  OUTPUT);
    pinMode(PIN_EN2,  OUTPUT);

    digitalWrite(PIN_EN1, HIGH);
    digitalWrite(PIN_EN2, HIGH);

    /* Timer1: Phase & Frequency Correct PWM, TOP = ICR1 (mode 8)
       WGM13:12:11:10 = 1:0:0:0 → mode 8
       COM1A1 + COM1B1 = non-inverting on both OC1A and OC1B */
    TCCR1A = _BV(COM1A1) | _BV(COM1B1);    /* WGM11:10 = 00 */
    TCCR1B = _BV(WGM13) | _BV(CS10);       /* WGM13 = 1, no prescaler */
    ICR1   = PWM_TOP;
    OCR1A  = 0;
    OCR1B  = 0;
}

void motorSetForce(int force) {
    if (force > 500)  force = 500;
    if (force < -500) force = -500;

    if (force > 0) {
        OCR1B = 0;
        OCR1A = (uint16_t)force;
    } else if (force < 0) {
        OCR1A = 0;
        OCR1B = (uint16_t)(-force);
    } else {
        OCR1A = 0;
        OCR1B = 0;
    }
}
