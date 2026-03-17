/*
 * leonardo-wheel.ino
 *
 * Native USB HID Force Feedback steering wheel for Arduino Leonardo.
 * Uses the force-feedback joystick library for the HID/PID path while
 * keeping the existing serial settings channel for the desktop app.
 *
 * Hardware:
 *   Encoder:  D0 (PD2/INT2) + D1 (PD3/INT3), 600 PPR = 2400 counts/rev
 *   Motor:    BTS7960 on D9 (RPWM/OC1A) + D10 (LPWM/OC1B), D8/D11 enable
 *   USB:      Native HID game controller + CDC serial configuration
 */

#include <Joystick.h>
#include "EncoderReader.h"
#include "MotorDriver.h"
#include "WheelSettings.h"
#include "EepromStorage.h"
#include "SerialProtocol.h"
#include "WheelMath.h"

Joystick_ Joystick(
    JOYSTICK_DEFAULT_REPORT_ID,
    JOYSTICK_TYPE_JOYSTICK,
    8,
    0,
    true,
    false,
    false,
    false,
    false,
    false,
    false,
    false,
    false,
    false,
    false
);

static Gains ffbGains[1];
static EffectParams ffbParams[1];
static int32_t forces[1] = {0};

static long prevPositionRaw = 0;
static long prevVelocityRaw = 0;

static const int32_t HID_AXIS_MAX = 32767L;
static const int32_t DEFAULT_MAX_VELOCITY = 200;
static const int32_t DEFAULT_MAX_ACCELERATION = 100;
static const int32_t DEFAULT_MAX_POSITION_CHANGE = 200;

static void updateJoystickGains();
static int16_t scaleAngleToHidAxis(int16_t angleDeg, uint16_t steeringRange);
static int16_t clampForceToMotorRange(int32_t forceValue);
static int16_t applyMinimumForce(int16_t motorForce, uint8_t minForcePercent);

void setup() {
    serialProtocolInit();
    encoderInit();
    motorInit();

    if (eepromIsValid()) {
        eepromLoad(activeSettings);
        pendingSettings = activeSettings;
    } else {
        settingsLoadDefaults();
    }

    Joystick.setXAxisRange(-32768, 32767);
    Joystick.begin(false);
    updateJoystickGains();

    cli();
    TCCR3A = 0;
    TCCR3B = 0;
    TCNT3  = 0;
    OCR3A  = 3999;
    TCCR3B |= (1 << WGM32);
    TCCR3B |= (1 << CS31);
    TIMSK3 |= (1 << OCIE3A);
    sei();

    Serial.println(F("READY"));
}

ISR(TIMER3_COMPA_vect) {
    Joystick.getUSBPID();
}

void loop() {
    serialProtocolProcess();

    long raw = encoderRead();
    int16_t angleDeg = wheelMathComputeAngle(raw,
                                             activeSettings.center,
                                             activeSettings.range,
                                             activeSettings.invertEncoder);
    int16_t axisValue = scaleAngleToHidAxis(angleDeg, activeSettings.range);
    Joystick.setXAxis(axisValue);

    long centeredRaw = raw - activeSettings.center;
    if (activeSettings.invertEncoder) {
        centeredRaw = -centeredRaw;
    }

    int16_t velocity = (int16_t)(centeredRaw - prevPositionRaw);
    int16_t acceleration = (int16_t)(velocity - prevVelocityRaw);

    int32_t maxPosition = ((int32_t)activeSettings.range * ENCODER_COUNTS_PER_REV) / 720L;
    if (maxPosition < 1) {
        maxPosition = 1;
    }

    if (centeredRaw > maxPosition) centeredRaw = maxPosition;
    if (centeredRaw < -maxPosition) centeredRaw = -maxPosition;

    ffbParams[0].springMaxPosition = maxPosition;
    ffbParams[0].springPosition = centeredRaw;
    ffbParams[0].damperMaxVelocity = DEFAULT_MAX_VELOCITY;
    ffbParams[0].damperVelocity = velocity;
    ffbParams[0].inertiaMaxAcceleration = DEFAULT_MAX_ACCELERATION;
    ffbParams[0].inertiaAcceleration = acceleration;
    ffbParams[0].frictionMaxPositionChange = DEFAULT_MAX_POSITION_CHANGE;
    ffbParams[0].frictionPositionChange = velocity;

    updateJoystickGains();
    Joystick.setEffectParams(ffbParams);
    Joystick.getForce(forces);

    int16_t motorForce = clampForceToMotorRange((forces[0] * 500L) / 255L);
    motorForce = applyMinimumForce(motorForce, activeSettings.minForce);

    if (activeSettings.invertMotor) {
        motorForce = -motorForce;
    }

    motorSetForce(activeSettings.force == 0 ? 0 : motorForce);

    /* Test-force mode: if active, override the FFB-derived motor output. */
    if (serialProtocolTestActive()) {
        int16_t tf = serialProtocolTestForce();
        if (tf != 0) {
            /* Constant left or right force; scale by overall force setting. */
            int32_t scaled = ((int32_t)tf * activeSettings.force) / 100;
            if (activeSettings.invertMotor) scaled = -scaled;
            motorSetForce((int16_t)scaled);
        } else {
            /* Center mode: re-run spring math only. */
            int16_t springOnly = wheelMathComputeMotorOutput(
                angleDeg, (int16_t)(centeredRaw - prevPositionRaw),
                activeSettings.force, activeSettings.minForce,
                activeSettings.spring, 0, 0, activeSettings.invertMotor);
            motorSetForce(springOnly);
        }
    }
    Joystick.sendState();

    prevVelocityRaw = velocity;
    prevPositionRaw = centeredRaw;

    delayMicroseconds(500);
}

static void updateJoystickGains() {
    ffbGains[0].totalGain = activeSettings.force;
    ffbGains[0].constantGain = activeSettings.force;
    ffbGains[0].rampGain = activeSettings.force;
    ffbGains[0].squareGain = activeSettings.force;
    ffbGains[0].sineGain = activeSettings.force;
    ffbGains[0].triangleGain = activeSettings.force;
    ffbGains[0].sawtoothdownGain = activeSettings.force;
    ffbGains[0].sawtoothupGain = activeSettings.force;
    ffbGains[0].springGain = activeSettings.spring;
    ffbGains[0].damperGain = activeSettings.damping;
    ffbGains[0].inertiaGain = activeSettings.damping;
    ffbGains[0].frictionGain = activeSettings.friction;
    ffbGains[0].customGain = activeSettings.force;
    Joystick.setGains(ffbGains);
}

static int16_t scaleAngleToHidAxis(int16_t angleDeg, uint16_t steeringRange) {
    int16_t halfRange = (int16_t)(steeringRange / 2);
    if (halfRange <= 0) {
        return 0;
    }

    int32_t scaled = ((int32_t)angleDeg * HID_AXIS_MAX) / halfRange;
    if (scaled > HID_AXIS_MAX) scaled = HID_AXIS_MAX;
    if (scaled < -32768L) scaled = -32768L;
    return (int16_t)scaled;
}

static int16_t clampForceToMotorRange(int32_t forceValue) {
    if (forceValue > 500L) {
        return 500;
    }
    if (forceValue < -500L) {
        return -500;
    }
    return (int16_t)forceValue;
}

static int16_t applyMinimumForce(int16_t motorForce, uint8_t minForcePercent) {
    int16_t minimumForce = (int16_t)((int32_t)minForcePercent * 500L / 100L);
    if (motorForce > 0 && motorForce < minimumForce) {
        return minimumForce;
    }
    if (motorForce < 0 && motorForce > -minimumForce) {
        return -minimumForce;
    }
    return motorForce;
}
