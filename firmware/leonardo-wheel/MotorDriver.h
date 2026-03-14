#pragma once

#include <Arduino.h>

/* BTS7960 motor driver: D9=RPWM, D10=LPWM, D8+D11=enable.
   16 kHz PWM via Timer1. Force range: -500..+500. */

void motorInit();

/* Set motor output. force: -500 (left) … 0 (stop) … +500 (right). */
void motorSetForce(int force);
