#include "EncoderReader.h"

/*
 * x4 quadrature decoder on D0 (PD2/INT2) and D1 (PD3/INT3).
 * Matches EMCLite: lookup-table ISR, fires on CHANGE for both channels,
 * supports +-2 missed-step recovery.
 *
 * Leonardo ATmega32U4 pin mapping:
 *   D0 = PD2 = INT2    digitalPinToInterrupt(0) = 2
 *   D1 = PD3 = INT3    digitalPinToInterrupt(1) = 3
 */

static volatile long encoderCount = 0;
static volatile uint8_t prevState = 0;

/* Lookup table indexed by (prevBA << 2 | currBA).
   Same table as EMCLite: 0=no change, +-1=normal step, +-2=missed step. */
static const int8_t QEM[16] = {
     0,  1, -1,  2,   /* prev=00: curr=00,01,10,11 */
    -1,  0,  2,  1,   /* prev=01 */
     1,  2,  0, -1,   /* prev=10 */
     2, -1,  1,  0    /* prev=11 */
};

/* Shared ISR — reads both pins via direct port access for speed. */
static void encoderISR() {
    uint8_t pind = PIND;
    uint8_t curr = ((pind >> 2) & 0x01) | ((pind >> 2) & 0x02);  /* bit0=PD2, bit1=PD3 */
    uint8_t idx  = (prevState << 2) | curr;
    encoderCount += QEM[idx];
    prevState = curr;
}

void encoderInit() {
    pinMode(0, INPUT_PULLUP);
    pinMode(1, INPUT_PULLUP);

    /* Read initial state */
    uint8_t pind = PIND;
    prevState = ((pind >> 2) & 0x01) | ((pind >> 2) & 0x02);

    attachInterrupt(digitalPinToInterrupt(0), encoderISR, CHANGE);
    attachInterrupt(digitalPinToInterrupt(1), encoderISR, CHANGE);
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
