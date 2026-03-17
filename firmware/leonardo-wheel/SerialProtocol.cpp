#include "SerialProtocol.h"
#include "WheelSettings.h"
#include "EepromStorage.h"
#include "EncoderReader.h"
#include "WheelMath.h"
#include "MotorDriver.h"
#include "BuildVersion.h"

/* Test-force state: when non-zero the protocol loop drives the motor directly
   instead of the normal FFB calculation.  The main sketch checks this flag. */
static int16_t s_testForce   = 0;   /* -500..+500, 0 = off              */
static bool    s_testActive  = false;

bool  serialProtocolTestActive()  { return s_testActive; }
int16_t serialProtocolTestForce() { return s_testForce; }

#define SERIAL_BUF_SIZE 64

static char cmdBuf[SERIAL_BUF_SIZE];
static uint8_t cmdIdx = 0;

/* Forward declarations. */
static void handleCommand(const char* cmd);
static void sendSettings();
static void sendLiveState();
static bool startsWith(const char* str, const char* prefix);
static int  parseIntVal(const char* str);
static long parseLongVal(const char* str);

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
        Serial.println(F("OK"));
    } else if (strcmp(cmd, "SAVE") == 0) {
        eepromSave(activeSettings);
        Serial.println(F("OK"));
    } else if (strcmp(cmd, "LOAD_DEFAULTS") == 0) {
        settingsLoadDefaults();
        Serial.println(F("OK"));
    } else if (strcmp(cmd, "SET_CENTER_NOW") == 0 ||
               strcmp(cmd, "SET_CENTER") == 0) {
        long raw = encoderRead();
        activeSettings.center  = (int32_t)raw;
        pendingSettings.center = (int32_t)raw;
        Serial.println(F("OK"));
    } else if (startsWith(cmd, "SET ")) {
        /* Parse "SET <KEY> <VALUE>" */
        const char* rest = cmd + 4;

        if (startsWith(rest, "FORCE ")) {
            pendingSettings.force = (uint8_t)constrain(parseIntVal(rest + 6), 0, 100);
        } else if (startsWith(rest, "MIN_FORCE ")) {
            pendingSettings.minForce = (uint8_t)constrain(parseIntVal(rest + 10), 0, 100);
        } else if (startsWith(rest, "DAMPING ")) {
            pendingSettings.damping = (uint8_t)constrain(parseIntVal(rest + 8), 0, 100);
        } else if (startsWith(rest, "FRICTION ")) {
            pendingSettings.friction = (uint8_t)constrain(parseIntVal(rest + 9), 0, 100);
        } else if (startsWith(rest, "SPRING ")) {
            pendingSettings.spring = (uint8_t)constrain(parseIntVal(rest + 7), 0, 100);
        } else if (startsWith(rest, "RANGE ")) {
            pendingSettings.range = (uint16_t)constrain(parseIntVal(rest + 6), 90, 1800);
        } else if (startsWith(rest, "CENTER ")) {
            pendingSettings.center = (int32_t)parseLongVal(rest + 7);
        } else if (startsWith(rest, "INV_ENCODER ")) {
            pendingSettings.invertEncoder = (parseIntVal(rest + 12) != 0) ? 1 : 0;
        } else if (startsWith(rest, "INV_MOTOR ")) {
            pendingSettings.invertMotor = (parseIntVal(rest + 10) != 0) ? 1 : 0;
        } else {
            Serial.println(F("ERROR INVALID_COMMAND"));
            return;
        }
        Serial.println(F("OK"));
    } else if (startsWith(cmd, "TEST_FORCE ")) {
        /* TEST_FORCE LEFT [0-100]
           TEST_FORCE RIGHT [0-100]
           TEST_FORCE CENTER
           TEST_FORCE STOP          */
        const char* rest = cmd + 11;
        if (startsWith(rest, "LEFT")) {
            int pct = 50;
            if (rest[4] == ' ') pct = constrain(parseIntVal(rest + 5), 0, 100);
            s_testForce  = -(int16_t)(pct * 5);   /* map 0-100 → 0-500 */
            s_testActive = true;
            motorSetForce(s_testForce);
            Serial.println(F("OK"));
        } else if (startsWith(rest, "RIGHT")) {
            int pct = 50;
            if (rest[5] == ' ') pct = constrain(parseIntVal(rest + 6), 0, 100);
            s_testForce  = (int16_t)(pct * 5);
            s_testActive = true;
            motorSetForce(s_testForce);
            Serial.println(F("OK"));
        } else if (startsWith(rest, "CENTER")) {
            /* Apply a mild centering (spring) force toward encoder zero.
               The angle is re-evaluated on each firmware tick, so we just set
               the flag and let the main loop run spring-only math. */
            s_testForce  = 0;
            s_testActive = true;   /* main loop handles centering when active+0 */
            motorSetForce(0);
            Serial.println(F("OK"));
        } else if (startsWith(rest, "STOP")) {
            s_testForce  = 0;
            s_testActive = false;
            motorSetForce(0);
            Serial.println(F("OK"));
        } else {
            Serial.println(F("ERROR INVALID_COMMAND"));
        }
    } else {
        Serial.println(F("ERROR INVALID_COMMAND"));
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
    Serial.print(F("INV_ENCODER="));    Serial.println(activeSettings.invertEncoder);
    Serial.print(F("INV_MOTOR="));      Serial.println(activeSettings.invertMotor);
    Serial.print(F("FW_VERSION="));     Serial.println(F(FW_VERSION));
    Serial.println(F("PRODUCT_NAME=EMC-compatible wheel"));
    Serial.println(F("PROFILE=EMC-style serial setup"));
    Serial.println(F("USB_MODE=CDC config channel"));
    Serial.println(F("END_SETTINGS"));
}

static void sendLiveState() {
    long raw   = encoderRead();
    int16_t ang = wheelMathComputeAngle(raw, activeSettings.center,
                                        activeSettings.range,
                                        activeSettings.invertEncoder);
    Serial.println(F("BEGIN_LIVE"));
    Serial.print(F("LIVE_ANGLE="));  Serial.println(ang);
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

static int parseIntVal(const char* str) {
    return atoi(str);
}

static long parseLongVal(const char* str) {
    return atol(str);
}
