#include "SerialProtocol.h"
#include "WheelSettings.h"
#include "EepromStorage.h"
#include "EncoderReader.h"
#include "WheelMath.h"

#define FW_VERSION "1.0.0"
#define SERIAL_BUF_SIZE 64

static char cmdBuf[SERIAL_BUF_SIZE];
static uint8_t cmdIdx = 0;

/* Forward declarations. */
static void handleCommand(const char* cmd);
static void sendSettings();
static void sendLiveState();
static bool startsWith(const char* str, const char* prefix);
static int  parseInt(const char* str);

void serialProtocolInit() {
    Serial.begin(115200);
}

void serialProtocolProcess() {
    while (Serial.available()) {
        char c = (char)Serial.read();
        if (c == '\n' || c == '\r') {
            if (cmdIdx > 0) {
                cmdBuf[cmdIdx] = '\0';
                handleCommand(cmdBuf);
                cmdIdx = 0;
            }
        } else if (cmdIdx < SERIAL_BUF_SIZE - 1) {
            cmdBuf[cmdIdx++] = c;
        }
    }
}

/* ---- command dispatch ---- */

static void handleCommand(const char* cmd) {
    if (strcmp(cmd, "GET_SETTINGS") == 0) {
        sendSettings();
    } else if (strcmp(cmd, "GET_LIVE_STATE") == 0) {
        sendLiveState();
    } else if (strcmp(cmd, "APPLY") == 0) {
        settingsApplyPending();
    } else if (strcmp(cmd, "SAVE") == 0) {
        eepromSave(activeSettings);
    } else if (strcmp(cmd, "LOAD_DEFAULTS") == 0) {
        settingsLoadDefaults();
    } else if (strcmp(cmd, "SET_CENTER") == 0) {
        long raw = encoderRead();
        activeSettings.center  = raw;
        pendingSettings.center = raw;
    } else if (startsWith(cmd, "SET ")) {
        /* Parse "SET <KEY> <VALUE>" */
        const char* rest = cmd + 4;

        if (startsWith(rest, "FORCE ")) {
            pendingSettings.force = (uint8_t)constrain(parseInt(rest + 6), 0, 100);
        } else if (startsWith(rest, "MIN_FORCE ")) {
            pendingSettings.minForce = (uint8_t)constrain(parseInt(rest + 10), 0, 100);
        } else if (startsWith(rest, "DAMPING ")) {
            pendingSettings.damping = (uint8_t)constrain(parseInt(rest + 8), 0, 100);
        } else if (startsWith(rest, "FRICTION ")) {
            pendingSettings.friction = (uint8_t)constrain(parseInt(rest + 9), 0, 100);
        } else if (startsWith(rest, "SPRING ")) {
            pendingSettings.spring = (uint8_t)constrain(parseInt(rest + 7), 0, 100);
        } else if (startsWith(rest, "RANGE ")) {
            pendingSettings.range = (uint16_t)constrain(parseInt(rest + 6), 90, 1800);
        } else if (startsWith(rest, "INV_ENCODER ")) {
            pendingSettings.invertEncoder = (parseInt(rest + 12) != 0);
        } else if (startsWith(rest, "INV_MOTOR ")) {
            pendingSettings.invertMotor = (parseInt(rest + 10) != 0);
        }
    }
}

/* ---- response helpers ---- */

static void sendSettings() {
    Serial.println(F("BEGIN_SETTINGS"));
    Serial.print(F("FORCE="));          Serial.println(activeSettings.force);
    Serial.print(F("MIN_FORCE="));      Serial.println(activeSettings.minForce);
    Serial.print(F("DAMPING="));        Serial.println(activeSettings.damping);
    Serial.print(F("FRICTION="));       Serial.println(activeSettings.friction);
    Serial.print(F("SPRING="));         Serial.println(activeSettings.spring);
    Serial.print(F("RANGE="));          Serial.println(activeSettings.range);
    Serial.print(F("CENTER="));         Serial.println(activeSettings.center);
    Serial.print(F("INV_ENCODER="));    Serial.println(activeSettings.invertEncoder ? 1 : 0);
    Serial.print(F("INV_MOTOR="));      Serial.println(activeSettings.invertMotor ? 1 : 0);
    Serial.print(F("FW_VERSION="));     Serial.println(F(FW_VERSION));
    Serial.println(F("END_SETTINGS"));
}

static void sendLiveState() {
    long raw   = encoderRead();
    double ang = wheelMathComputeAngle(raw, activeSettings.center,
                                       activeSettings.range,
                                       activeSettings.invertEncoder);
    Serial.println(F("BEGIN_LIVE"));
    Serial.print(F("LIVE_ANGLE="));  Serial.println((int)ang);
    Serial.print(F("RAW_COUNTS="));  Serial.println(raw);
    Serial.println(F("END_LIVE"));
}

/* ---- string utilities ---- */

static bool startsWith(const char* str, const char* prefix) {
    while (*prefix) {
        if (*str++ != *prefix++) return false;
    }
    return true;
}

static int parseInt(const char* str) {
    return atoi(str);
}
