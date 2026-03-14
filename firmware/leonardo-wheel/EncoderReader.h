#pragma once

#include <Arduino.h>

/* Quadrature encoder on D0/D1 (INT2/INT3 on Leonardo), 600 PPR = 2400 counts/rev. */

void encoderInit();
long encoderRead();
void encoderReset();
