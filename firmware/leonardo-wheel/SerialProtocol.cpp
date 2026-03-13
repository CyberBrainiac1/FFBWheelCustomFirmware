#include "SerialProtocol.h"
#include "WheelSettings.h"
#include "EepromStorage.h"
#include <Arduino.h>
#include <string.h>
#include <stdlib.h>

#define SERIAL_BUF_SIZE 48

static char    buf[SERIAL_BUF_SIZE];
static uint8_t bufLen = 0;

static void processLine(char* line, WheelConfig* cfg,
                        int32_t rawCounts, int16_t liveAngle);

void serial_init(void) {
    Serial.begin(115200);
}

void serial_process(WheelConfig* cfg, int32_t rawCounts, int16_t liveAngle) {
    while (Serial.available()) {
        char c = (char)Serial.read();
        if (c == '\r') continue;
        if (c == '\n') {
            buf[bufLen] = '\0';
            if (bufLen > 0) {
                processLine(buf, cfg, rawCounts, liveAngle);
            }
            bufLen = 0;
        } else if (bufLen < SERIAL_BUF_SIZE - 1) {
            buf[bufLen++] = c;
        } else {
            // Buffer full: discard accumulated data so the next line can
            // be received cleanly.  The current partial command is lost.
            bufLen = 0;
        }
    }
}

void serial_sendSettings(const WheelConfig* cfg) {
    Serial.println(F("BEGIN_SETTINGS"));
    Serial.print(F("FORCE="));      Serial.println(cfg->force);
    Serial.print(F("MIN_FORCE="));  Serial.println(cfg->minForce);
    Serial.print(F("DAMPING="));    Serial.println(cfg->damping);
    Serial.print(F("FRICTION="));   Serial.println(cfg->friction);
    Serial.print(F("SPRING="));     Serial.println(cfg->spring);
    Serial.print(F("RANGE="));      Serial.println(cfg->range);
    Serial.print(F("CENTER="));     Serial.println(cfg->center);
    Serial.print(F("INV_ENCODER=")); Serial.println(cfg->invEncoder);
    Serial.print(F("INV_MOTOR="));  Serial.println(cfg->invMotor);
    Serial.println(F("FW_VERSION=" FW_VERSION_STR));
    Serial.println(F("END_SETTINGS"));
}

void serial_sendLiveState(int16_t angle, int32_t raw) {
    Serial.println(F("BEGIN_LIVE"));
    Serial.print(F("LIVE_ANGLE=")); Serial.println(angle);
    Serial.print(F("RAW_COUNTS=")); Serial.println(raw);
    Serial.println(F("END_LIVE"));
}

// ── helpers ──────────────────────────────────────────────────────────────────
static inline uint8_t  toU8(const char* s)  { return (uint8_t)atoi(s); }
static inline uint16_t toU16(const char* s) { return (uint16_t)atoi(s); }

static void processLine(char* line, WheelConfig* cfg,
                        int32_t rawCounts, int16_t liveAngle) {
    (void)liveAngle; // not needed in outgoing responses

    if (strcmp(line, "GET_SETTINGS") == 0) {
        serial_sendSettings(cfg);
        return;
    }

    if (strcmp(line, "GET_LIVE_STATE") == 0) {
        serial_sendLiveState(liveAngle, rawCounts);
        return;
    }

    if (strcmp(line, "APPLY") == 0) {
        Serial.println(F("OK_APPLIED"));
        return;
    }

    if (strcmp(line, "SAVE") == 0) {
        cfg->checksum = settings_calcChecksum(cfg);
        eeprom_save(cfg);
        Serial.println(F("OK_SAVED"));
        return;
    }

    if (strcmp(line, "LOAD_DEFAULTS") == 0) {
        settings_loadDefaults(cfg);
        eeprom_save(cfg);
        serial_sendSettings(cfg);
        return;
    }

    if (strcmp(line, "SET_CENTER") == 0) {
        cfg->center = rawCounts;
        Serial.print(F("OK_CENTER="));
        Serial.println(cfg->center);
        return;
    }

    // SET <KEY> <VALUE>
    if (strncmp(line, "SET ", 4) == 0) {
        char* rest = line + 4;
        char* sp   = strchr(rest, ' ');
        if (!sp) { Serial.println(F("ERR_BAD_FORMAT")); return; }
        *sp = '\0';
        const char* val = sp + 1;

        if      (strcmp(rest, "FORCE")       == 0) cfg->force      = toU8(val);
        else if (strcmp(rest, "MIN_FORCE")   == 0) cfg->minForce   = toU8(val);
        else if (strcmp(rest, "DAMPING")     == 0) cfg->damping    = toU8(val);
        else if (strcmp(rest, "FRICTION")    == 0) cfg->friction   = toU8(val);
        else if (strcmp(rest, "SPRING")      == 0) cfg->spring     = toU8(val);
        else if (strcmp(rest, "RANGE")       == 0) cfg->range      = toU16(val);
        else if (strcmp(rest, "INV_ENCODER") == 0) cfg->invEncoder = toU8(val);
        else if (strcmp(rest, "INV_MOTOR")   == 0) cfg->invMotor   = toU8(val);
        else { Serial.println(F("ERR_UNKNOWN")); return; }

        Serial.println(F("OK"));
        return;
    }

    Serial.println(F("ERR_UNKNOWN"));
}
