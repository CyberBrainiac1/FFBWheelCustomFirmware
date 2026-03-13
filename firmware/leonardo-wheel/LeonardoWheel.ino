// LeonardoWheel.ino — main entry point for the FFB wheel firmware.
// Target: Arduino Leonardo (ATmega32u4)
//
// Pin summary
//   D2  – encoder channel A (INT0)
//   D3  – encoder channel B (INT1)
//   D7  – BTS7960 R_EN
//   D8  – BTS7960 L_EN
//   D9  – BTS7960 RPWM (forward)
//   D10 – BTS7960 LPWM (reverse)

#include "WheelSettings.h"
#include "EncoderReader.h"
#include "MotorDriver.h"
#include "SerialProtocol.h"
#include "EepromStorage.h"
#include "WheelMath.h"

static WheelConfig cfg;

// Motor update rate: 10 ms (100 Hz) is plenty for spring / damping effects.
#define FORCE_INTERVAL_MS 10

static uint32_t lastForceMs = 0;

void setup(void) {
    serial_init();
    encoder_init();
    motor_init();

    if (!eeprom_load(&cfg)) {
        // EEPROM blank or corrupt — write factory defaults.
        settings_loadDefaults(&cfg);
        eeprom_save(&cfg);
    }
}

void loop(void) {
    uint32_t now   = millis();
    int32_t  raw   = encoder_getRaw();
    int16_t  angle = encoder_getAngle(cfg.center, cfg.range, cfg.invEncoder);

    // Handle incoming serial commands; may update cfg in place.
    serial_process(&cfg, raw, angle);

    // Update motor output at FORCE_INTERVAL_MS rate.
    if ((now - lastForceMs) >= FORCE_INTERVAL_MS) {
        lastForceMs = now;

        int16_t power = math_mapForce(angle, cfg.range,
                                      cfg.spring, cfg.force, cfg.minForce);
        if (cfg.invMotor) power = -power;
        motor_setPower(power);
    }
}
