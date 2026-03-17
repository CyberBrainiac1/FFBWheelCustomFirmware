#pragma once

#include <Arduino.h>

/* Text-based serial command interface at 115200 8N1. */

void serialProtocolInit();
void serialProtocolProcess();   // call every loop iteration

/* Test-force mode: set by TEST_FORCE commands.  When active, the main sketch
   bypasses the normal FFB calculation and drives the motor directly. */
bool    serialProtocolTestActive();
int16_t serialProtocolTestForce();
