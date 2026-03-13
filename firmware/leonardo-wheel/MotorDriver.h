#pragma once

#include <Arduino.h>

/* BTS7960 motor driver: RPWM pin 9, LPWM pin 10, enable pin 8. */

void motorInit();

/* Set motor output. force: -255 (left) … 0 (stop) … +255 (right). */
void motorSetForce(int force);
