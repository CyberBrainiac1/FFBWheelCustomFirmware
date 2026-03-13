#pragma once
#include <stdint.h>

// Encoder PPR * 4 (quadrature) / 360.  Adjust for your encoder.
// Example: 600 PPR encoder => 2400 CPR => 6.67 counts/deg => use 6 or 7.
// Default 4 works well for 360 PPR encoders.
#define COUNTS_PER_DEG 4

void    encoder_init(void);
int32_t encoder_getRaw(void);

// Returns processed steering angle in whole degrees, clamped to ±(range/2).
// center    – encoder count that represents wheel centre
// range     – full steering range in degrees (e.g. 900 for ±450°)
// invert    – 1 to flip direction
int16_t encoder_getAngle(int32_t center, uint16_t range, uint8_t invert);
