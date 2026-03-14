#pragma once

#include <Arduino.h>

/* 600 PPR encoder in quadrature → 2400 counts per revolution. */
#define ENCODER_COUNTS_PER_REV 2400

/*
 * Convert raw encoder counts to a steering angle in integer degrees,
 * clamped to ±(steeringRange / 2).
 */
int16_t wheelMathComputeAngle(long rawCounts, int32_t centerOffset,
                              uint16_t steeringRange, bool invertEncoder);

/*
 * Compute the motor output (-255 … +255) from the current angle
 * and the active force-feedback parameters.
 * velocityRaw: raw encoder count change since the last call (for damping).
 */
int16_t wheelMathComputeMotorOutput(int16_t angleDeg, int16_t velocityRaw,
                                    uint8_t force, uint8_t minForce,
                                    uint8_t spring, uint8_t damping,
                                    uint8_t friction, bool invertMotor);
