#pragma once
#include "WheelSettings.h"

void serial_init(void);

// Call every loop iteration: reads incoming bytes and processes complete lines.
// cfg       – mutable settings (may be updated by SET / SAVE / LOAD_DEFAULTS)
// rawCounts – current raw encoder count (used for SET_CENTER)
// liveAngle – current processed steering angle in degrees
void serial_process(WheelConfig* cfg, int32_t rawCounts, int16_t liveAngle);

void serial_sendSettings(const WheelConfig* cfg);
void serial_sendLiveState(int16_t angle, int32_t raw);
