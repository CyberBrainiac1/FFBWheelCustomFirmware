#include "EepromStorage.h"
#include "WheelSettings.h"
#include <EEPROM.h>

bool eeprom_load(WheelConfig* cfg) {
    EEPROM.get(EEPROM_ADDR, *cfg);
    if (cfg->magic != SETTINGS_MAGIC || cfg->version != SETTINGS_VERSION) {
        return false;
    }
    return cfg->checksum == settings_calcChecksum(cfg);
}

void eeprom_save(const WheelConfig* cfg) {
    EEPROM.put(EEPROM_ADDR, *cfg);
}
