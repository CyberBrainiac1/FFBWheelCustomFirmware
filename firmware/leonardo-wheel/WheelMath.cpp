#include "WheelMath.h"

int16_t math_clamp16(int16_t v, int16_t lo, int16_t hi) {
    if (v < lo) return lo;
    if (v > hi) return hi;
    return v;
}

int16_t math_mapForce(int16_t angle, uint16_t range,
                      uint8_t spring, uint8_t force, uint8_t minForce) {
    if (force == 0 || spring == 0) return 0;

    int16_t half = (int16_t)(range >> 1);
    if (half == 0) return 0;

    // Spring force opposes displacement: -angle/half * 255 * spring/100 * force/100
    // All intermediate math in int32 to avoid overflow on 16-bit MCU.
    int32_t pwr = ((int32_t)(-angle) * 255L * spring * force)
                  / ((int32_t)half * 100L * 100L);

    int16_t result = math_clamp16((int16_t)pwr, -255, 255);

    // Minimum-force snap: only applied when the spring already produces a
    // non-zero output (wheel is off-centre).  At centre the output stays 0
    // so the wheel can rest freely there.  This intentionally creates a small
    // step at the threshold angle — tune minForce to taste (0 disables it).
    if (result > 0 && result < (int16_t)minForce) result = (int16_t)minForce;
    if (result < 0 && result > -(int16_t)minForce) result = -(int16_t)minForce;

    return result;
}
