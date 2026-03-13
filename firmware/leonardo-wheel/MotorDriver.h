#pragma once
#include <stdint.h>

// BTS7960 half-bridge driver pin mapping.
// Change these to match your wiring.
#define MOTOR_RPWM_PIN 9   // forward PWM
#define MOTOR_LPWM_PIN 10  // reverse PWM
#define MOTOR_REN_PIN  7   // right-side enable
#define MOTOR_LEN_PIN  8   // left-side enable

void motor_init(void);

// power: -255 (full reverse) … 0 (stop) … +255 (full forward)
void motor_setPower(int16_t power);
void motor_stop(void);
