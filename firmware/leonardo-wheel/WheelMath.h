#pragma once

#include <Arduino.h>

/* 600 PPR encoder in quadrature → 2400 counts per revolution. */
#define ENCODER_COUNTS_PER_REV 2400

/*
 * Convert raw encoder counts to a steering angle in degrees,
 * clamped to ±(steeringRange / 2).
 */
double wheelMathComputeAngle(long rawCounts, long centerOffset,
                             int steeringRange, bool invertEncoder);

/*
 * Compute the motor output (-255 … +255) from the current angle
 * and the active force-feedback parameters.
 */
int wheelMathComputeMotorOutput(double angle, double prevAngle,
                                uint8_t force, uint8_t minForce,
                                uint8_t spring, uint8_t damping,
                                uint8_t friction, bool invertMotor);
