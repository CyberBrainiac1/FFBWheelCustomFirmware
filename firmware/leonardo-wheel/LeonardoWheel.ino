/*
 * LeonardoWheel.ino
 *
 * Main sketch for an Arduino Leonardo DIY force-feedback steering wheel.
 * Hardware: BTS7960 motor driver, 600 PPR quadrature encoder, USB serial.
 */

#include "EncoderReader.h"
#include "MotorDriver.h"
#include "WheelSettings.h"
#include "EepromStorage.h"
#include "SerialProtocol.h"
#include "WheelMath.h"

static double prevAngle = 0.0;
static unsigned long lastMotorUpdate = 0;
static const unsigned long MOTOR_INTERVAL_US = 1000; // 1 ms

void setup() {
    serialProtocolInit();
    encoderInit();
    motorInit();

    /* Load persisted settings or fall back to compiled defaults. */
    if (eepromIsValid()) {
        eepromLoad(activeSettings);
        pendingSettings = activeSettings;
    } else {
        settingsLoadDefaults();
    }
}

void loop() {
    /* Always service serial commands. */
    serialProtocolProcess();

    /* Update motor at ~1 kHz. */
    unsigned long now = micros();
    if (now - lastMotorUpdate >= MOTOR_INTERVAL_US) {
        lastMotorUpdate = now;

        long raw = encoderRead();
        double angle = wheelMathComputeAngle(raw,
                                             activeSettings.center,
                                             activeSettings.range,
                                             activeSettings.invertEncoder);

        int motorOut = wheelMathComputeMotorOutput(angle, prevAngle,
                                                   activeSettings.force,
                                                   activeSettings.minForce,
                                                   activeSettings.spring,
                                                   activeSettings.damping,
                                                   activeSettings.friction,
                                                   activeSettings.invertMotor);
        motorSetForce(motorOut);
        prevAngle = angle;
    }
}
