#pragma once

#include <Arduino.h>

/* Text-based serial command interface at 115200 8N1. */

void serialProtocolInit();
void serialProtocolProcess();   // call every loop iteration
