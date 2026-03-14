#pragma once

#include <Arduino.h>

/* Quadrature encoder on D2/D3 (INT0/INT1), 600 PPR → 2400 counts/rev. */

void encoderInit();
long encoderRead();
void encoderReset();
