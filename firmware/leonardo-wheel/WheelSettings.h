#pragma once

#include <Arduino.h>

struct WheelSettingsData {
    uint8_t  force;          // 0-100  overall force %
    uint8_t  minForce;       // 0-100  minimum force %
    uint8_t  damping;        // 0-100
    uint8_t  friction;       // 0-100
    uint8_t  spring;         // 0-100
    uint16_t range;          // 90-1800  steering range in degrees
    long     center;         // encoder count at centre position
    bool     invertEncoder;
    bool     invertMotor;
};

/* Default values matching the desktop-app defaults. */
static const WheelSettingsData kDefaultSettings = {
    60,     // force
    5,      // minForce
    10,     // damping
    4,      // friction
    15,     // spring
    900,    // range
    0,      // center
    false,  // invertEncoder
    false   // invertMotor
};

/* Active (running) settings and pending (staged) settings. */
extern WheelSettingsData activeSettings;
extern WheelSettingsData pendingSettings;

void settingsLoadDefaults();
void settingsApplyPending();   // copy pending → active
void settingsCopyActiveToPending();
