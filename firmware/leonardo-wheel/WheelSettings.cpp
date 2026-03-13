#include "WheelSettings.h"

void settings_loadDefaults(WheelConfig* cfg) {
    cfg->magic      = SETTINGS_MAGIC;
    cfg->version    = SETTINGS_VERSION;
    cfg->range      = DEFAULT_RANGE;
    cfg->center     = DEFAULT_CENTER;
    cfg->invEncoder = DEFAULT_INV_ENCODER;
    cfg->invMotor   = DEFAULT_INV_MOTOR;
    cfg->force      = DEFAULT_FORCE;
    cfg->minForce   = DEFAULT_MIN_FORCE;
    cfg->damping    = DEFAULT_DAMPING;
    cfg->friction   = DEFAULT_FRICTION;
    cfg->spring     = DEFAULT_SPRING;
    cfg->checksum   = settings_calcChecksum(cfg);
}

uint8_t settings_calcChecksum(const WheelConfig* cfg) {
    const uint8_t* p = (const uint8_t*)cfg;
    uint8_t result = 0;
    for (size_t i = 0; i < offsetof(WheelConfig, checksum); i++) {
        result ^= p[i];
    }
    return result;
}
