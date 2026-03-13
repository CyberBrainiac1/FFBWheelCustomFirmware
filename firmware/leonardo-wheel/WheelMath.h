#pragma once
#include <stdint.h>

int16_t math_clamp16(int16_t v, int16_t lo, int16_t hi);

// Calculate spring-return motor power from current steering angle.
// angle    – current angle in degrees (negative = left of centre)
// range    – full steering range in degrees
// spring   – spring coefficient 0..100
// force    – overall force scale 0..100
// minForce – minimum force threshold 0..100
// Returns motor power in -255..255
int16_t math_mapForce(int16_t angle, uint16_t range,
                      uint8_t spring, uint8_t force, uint8_t minForce);
