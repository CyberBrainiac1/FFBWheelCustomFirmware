#pragma once

#include "WheelSettings.h"

/* EEPROM persistence with magic, version, and checksum validation. */

void eepromSave(const WheelSettingsData& data);
void eepromLoad(WheelSettingsData& data);
bool eepromIsValid();
