#include "WheelMath.h"

double wheelMathComputeAngle(long rawCounts, long centerOffset,
                             int steeringRange, bool invertEncoder) {
    long effective = rawCounts - centerOffset;
    if (invertEncoder) effective = -effective;

    double angle = (double)effective * 360.0 / (double)ENCODER_COUNTS_PER_REV;

    double halfRange = steeringRange / 2.0;
    if (angle >  halfRange) angle =  halfRange;
    if (angle < -halfRange) angle = -halfRange;

    return angle;
}

int wheelMathComputeMotorOutput(double angle, double prevAngle,
                                uint8_t force, uint8_t minForce,
                                uint8_t spring, uint8_t damping,
                                uint8_t friction, bool invertMotor) {
    if (force == 0) return 0;

    double forceScale = force / 100.0;

    /* Spring: restoring force toward centre. */
    double springF = -angle * (spring / 100.0) * forceScale * 255.0;

    /* Damping: opposes velocity (approximated by angle delta). */
    double velocity  = angle - prevAngle;
    double dampingF  = -velocity * (damping / 100.0) * forceScale * 255.0;

    /* Friction: constant opposing force in direction of motion. */
    double frictionF = 0.0;
    if (velocity > 0.01) {
        frictionF = -(friction / 100.0) * forceScale * 255.0;
    } else if (velocity < -0.01) {
        frictionF =  (friction / 100.0) * forceScale * 255.0;
    }

    double total = springF + dampingF + frictionF;

    /* Dead-zone elimination: if motor should move but the PWM is below
       the minimum needed to overcome static friction, snap up to minF. */
    double minF = (minForce / 100.0) * 255.0;
    if (total > 0 && total < minF)       total = minF;
    else if (total < 0 && total > -minF) total = -minF;

    /* Clamp to PWM range. */
    if (total >  255.0) total =  255.0;
    if (total < -255.0) total = -255.0;

    int output = (int)total;
    if (invertMotor) output = -output;

    return output;
}
