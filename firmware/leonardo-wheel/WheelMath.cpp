#include "WheelMath.h"

int16_t wheelMathComputeAngle(long rawCounts, int32_t centerOffset,
                              uint16_t steeringRange, bool invertEncoder) {
    int32_t effective = (int32_t)rawCounts - centerOffset;
    if (invertEncoder) effective = -effective;

    /* degrees = effective * 360 / 2400 = effective * 3 / 20 */
    int16_t angle = (int16_t)(effective * 3L / 20L);

    int16_t halfRange = (int16_t)(steeringRange / 2);
    if (angle >  halfRange) angle =  halfRange;
    if (angle < -halfRange) angle = -halfRange;

    return angle;
}

int16_t wheelMathComputeMotorOutput(int16_t angleDeg, int16_t velocityRaw,
                                    uint8_t force, uint8_t minForce,
                                    uint8_t spring, uint8_t damping,
                                    uint8_t friction, bool invertMotor) {
    if (force == 0) return 0;

    /* Spring: restoring force toward centre.
       = -angle * (spring/100) * (force/100) * 255
       Split multiplication to stay within int32_t. */
    int32_t tmp = (int32_t)angleDeg * (int32_t)spring * (int32_t)force;
    int32_t springF = -(tmp / 100 * 255 / 100);

    /* Damping: opposes velocity.
       velocityRaw is the raw encoder count delta per motor tick.
       Convert: velDeg = velocityRaw * 360 / 2400 = velocityRaw * 3 / 20
       dampingF = -velDeg * (damping/100) * (force/100) * 255
               = -(velocityRaw * 3 * damping * force * 255) / (20*100*100) */
    int32_t tmpD = (int32_t)velocityRaw * 3L * (int32_t)damping * (int32_t)force;
    int32_t dampingF = -(tmpD * 255L / 200000L);

    /* Friction: constant opposing force in direction of motion. */
    int32_t frictionF = 0;
    if (velocityRaw > 0) {
        frictionF = -((int32_t)friction * (int32_t)force * 255L / 10000L);
    } else if (velocityRaw < 0) {
        frictionF =  ((int32_t)friction * (int32_t)force * 255L / 10000L);
    }

    int32_t total = springF + dampingF + frictionF;

    /* Dead-zone elimination: snap up to minimum force if motor should
       move but PWM is below the static-friction threshold. */
    int16_t minF = (int16_t)((int32_t)minForce * 255 / 100);
    if (total > 0 && total <  minF) total =  minF;
    if (total < 0 && total > -minF) total = -minF;

    /* Clamp to PWM range. */
    if (total >  255) total =  255;
    if (total < -255) total = -255;

    int16_t output = (int16_t)total;
    if (invertMotor) output = -output;

    return output;
}
