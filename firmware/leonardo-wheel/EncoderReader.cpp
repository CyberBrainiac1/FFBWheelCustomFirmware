#include "EncoderReader.h"

/* Pins: D2 = channel A (INT0), D3 = channel B (INT1) on Leonardo. */
static const uint8_t PIN_ENC_A = 2;
static const uint8_t PIN_ENC_B = 3;

static volatile long encoderCount = 0;

/* ISR for channel A rising/falling edge. */
static void isrA() {
    bool a = digitalRead(PIN_ENC_A);
    bool b = digitalRead(PIN_ENC_B);
    encoderCount += (a == b) ? 1 : -1;
}

/* ISR for channel B rising/falling edge. */
static void isrB() {
    bool a = digitalRead(PIN_ENC_A);
    bool b = digitalRead(PIN_ENC_B);
    encoderCount += (a != b) ? 1 : -1;
}

void encoderInit() {
    pinMode(PIN_ENC_A, INPUT_PULLUP);
    pinMode(PIN_ENC_B, INPUT_PULLUP);
    attachInterrupt(digitalPinToInterrupt(PIN_ENC_A), isrA, CHANGE);
    attachInterrupt(digitalPinToInterrupt(PIN_ENC_B), isrB, CHANGE);
}

long encoderRead() {
    noInterrupts();
    long val = encoderCount;
    interrupts();
    return val;
}

void encoderReset() {
    noInterrupts();
    encoderCount = 0;
    interrupts();
}
