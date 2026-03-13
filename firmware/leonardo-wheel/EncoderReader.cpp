#include "EncoderReader.h"
#include <Arduino.h>

static volatile int32_t rawCounts = 0;

// Quadrature ISRs — fires on both edges of each channel (4× resolution).
// Channel A on D2 (INT0), channel B on D3 (INT1).
static void isr_a(void) {
    rawCounts += (digitalRead(2) == digitalRead(3)) ? 1 : -1;
}

static void isr_b(void) {
    rawCounts += (digitalRead(2) != digitalRead(3)) ? 1 : -1;
}

void encoder_init(void) {
    pinMode(2, INPUT_PULLUP);
    pinMode(3, INPUT_PULLUP);
    attachInterrupt(digitalPinToInterrupt(2), isr_a, CHANGE);
    attachInterrupt(digitalPinToInterrupt(3), isr_b, CHANGE);
}

int32_t encoder_getRaw(void) {
    int32_t v;
    noInterrupts();
    v = rawCounts;
    interrupts();
    return v;
}

int16_t encoder_getAngle(int32_t center, uint16_t range, uint8_t invert) {
    int32_t rel = encoder_getRaw() - center;
    if (invert) rel = -rel;
    int16_t angle = (int16_t)(rel / COUNTS_PER_DEG);
    int16_t half  = (int16_t)(range >> 1);
    if (angle >  half) angle =  half;
    if (angle < -half) angle = -half;
    return angle;
}
