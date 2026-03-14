#pragma once

#include <Arduino.h>

struct WheelSettingsData {
    int32_t  center;         // encoder count at centre position
    uint16_t range;          // 90-1800  steering range in degrees
    uint8_t  force;          // 0-100  overall force %
    uint8_t  minForce;       // 0-100  minimum force %
    uint8_t  damping;        // 0-100
    uint8_t  friction;       // 0-100
    uint8_t  spring;         // 0-100
    uint8_t  invertEncoder;  // 0 or 1
    uint8_t  invertMotor;    // 0 or 1
};

/* Default values matching the desktop-app defaults. */
static const WheelSettingsData kDefaultSettings = {
    0,      // center
    900,    // range
    60,     // force
    5,      // minForce
    10,     // damping
    4,      // friction
    15,     // spring
    0,      // invertEncoder
    0       // invertMotor
};

/* Active (running) settings and pending (staged) settings. */
extern WheelSettingsData activeSettings;
extern WheelSettingsData pendingSettings;

void settingsLoadDefaults();
void settingsApplyPending();   // copy pending → active
