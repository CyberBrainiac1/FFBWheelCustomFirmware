#include "EepromStorage.h"
#include <EEPROM.h>

static const uint32_t EEPROM_MAGIC   = 0xFFB10001UL;
static const uint16_t EEPROM_VERSION = 1;

/* EEPROM layout:
   [0..3]   uint32_t magic
   [4..5]   uint16_t version
   [6..N]   WheelSettingsData
   [N..N+1] uint16_t checksum           */

static const int ADDR_MAGIC   = 0;
static const int ADDR_VERSION = 4;
static const int ADDR_DATA    = 6;
static const int ADDR_CKSUM   = 6 + (int)sizeof(WheelSettingsData);

static uint16_t computeChecksum(const WheelSettingsData& data) {
    const uint8_t* p = (const uint8_t*)&data;
    uint16_t sum = 0;
    for (uint8_t i = 0; i < sizeof(data); i++) {
        sum += p[i];
    }
    return sum;
}

bool eepromIsValid() {
    uint32_t magic;
    uint16_t ver;
    EEPROM.get(ADDR_MAGIC, magic);
    if (magic != EEPROM_MAGIC) return false;

    EEPROM.get(ADDR_VERSION, ver);
    if (ver != EEPROM_VERSION) return false;

    WheelSettingsData tmp;
    EEPROM.get(ADDR_DATA, tmp);

    uint16_t stored;
    EEPROM.get(ADDR_CKSUM, stored);

    return computeChecksum(tmp) == stored;
}

void eepromSave(const WheelSettingsData& data) {
    uint32_t magic = EEPROM_MAGIC;
    uint16_t ver   = EEPROM_VERSION;
    uint16_t cksum = computeChecksum(data);

    EEPROM.put(ADDR_MAGIC,   magic);
    EEPROM.put(ADDR_VERSION, ver);
    EEPROM.put(ADDR_DATA,    data);
    EEPROM.put(ADDR_CKSUM,   cksum);
}

void eepromLoad(WheelSettingsData& data) {
    EEPROM.get(ADDR_DATA, data);
}
