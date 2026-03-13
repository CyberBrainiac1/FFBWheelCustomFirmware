#pragma once
#include <stdint.h>
#include <stddef.h>

#define SETTINGS_MAGIC   0xAB12
#define SETTINGS_VERSION 1
#define FW_VERSION_STR   "1.0.0"

// Default values match the desktop utility defaults.
#define DEFAULT_RANGE       900
#define DEFAULT_CENTER      0
#define DEFAULT_INV_ENCODER 0
#define DEFAULT_INV_MOTOR   0
#define DEFAULT_FORCE       60
#define DEFAULT_MIN_FORCE   5
#define DEFAULT_DAMPING     10
#define DEFAULT_FRICTION    4
#define DEFAULT_SPRING      15

struct __attribute__((packed)) WheelConfig {
    uint16_t magic;
    uint8_t  version;
    uint16_t range;       // steering range in degrees (90..1800)
    int32_t  center;      // encoder counts at steering centre
    uint8_t  invEncoder;  // 0 = normal, 1 = invert
    uint8_t  invMotor;    // 0 = normal, 1 = invert
    uint8_t  force;       // overall force  0..100 %
    uint8_t  minForce;    // minimum force  0..100 %
    uint8_t  damping;     // damping        0..100 %
    uint8_t  friction;    // friction       0..100 %
    uint8_t  spring;      // spring         0..100 %
    uint8_t  checksum;    // XOR of all preceding bytes
};

void    settings_loadDefaults(WheelConfig* cfg);
uint8_t settings_calcChecksum(const WheelConfig* cfg);
