#include "EepromStorage.h"
#include <EEPROM.h>

static const uint8_t  MAGIC_BYTE   = 0xA5;
static const int      ADDR_MAGIC   = 0;
static const int      ADDR_DATA    = 1;   // settings start right after magic

bool eepromIsValid() {
    return EEPROM.read(ADDR_MAGIC) == MAGIC_BYTE;
}

void eepromSave(const WheelSettingsData& data) {
    EEPROM.update(ADDR_MAGIC, MAGIC_BYTE);
    EEPROM.put(ADDR_DATA, data);
}

void eepromLoad(WheelSettingsData& data) {
    EEPROM.get(ADDR_DATA, data);
}
