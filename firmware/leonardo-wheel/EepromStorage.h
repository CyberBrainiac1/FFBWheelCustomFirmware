#pragma once
#include "WheelSettings.h"

// EEPROM start address for the settings block.
#define EEPROM_ADDR 0

// Returns true if data was valid (magic, version, checksum all match).
// On false the caller should load defaults and save them.
bool eeprom_load(WheelConfig* cfg);
void eeprom_save(const WheelConfig* cfg);
