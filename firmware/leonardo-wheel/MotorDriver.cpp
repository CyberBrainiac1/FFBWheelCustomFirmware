#include "MotorDriver.h"
#include <Arduino.h>

void motor_init(void) {
    pinMode(MOTOR_RPWM_PIN, OUTPUT);
    pinMode(MOTOR_LPWM_PIN, OUTPUT);
    pinMode(MOTOR_REN_PIN,  OUTPUT);
    pinMode(MOTOR_LEN_PIN,  OUTPUT);
    digitalWrite(MOTOR_REN_PIN, HIGH);
    digitalWrite(MOTOR_LEN_PIN, HIGH);
    motor_stop();
}

void motor_setPower(int16_t power) {
    if (power > 0) {
        analogWrite(MOTOR_LPWM_PIN, 0);
        analogWrite(MOTOR_RPWM_PIN, (uint8_t)power);
    } else if (power < 0) {
        analogWrite(MOTOR_RPWM_PIN, 0);
        analogWrite(MOTOR_LPWM_PIN, (uint8_t)(-power));
    } else {
        motor_stop();
    }
}

void motor_stop(void) {
    analogWrite(MOTOR_RPWM_PIN, 0);
    analogWrite(MOTOR_LPWM_PIN, 0);
}
