#pragma once

#include "WheelSettings.h"

/* EEPROM persistence with a magic-byte validity check. */

void eepromSave(const WheelSettingsData& data);
void eepromLoad(WheelSettingsData& data);
bool eepromIsValid();
