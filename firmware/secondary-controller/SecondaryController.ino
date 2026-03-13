// SecondaryController.ino — pedal controller firmware
// Target: Seeed Studio XIAO RP2040 (or any RP2040-based board)
//
// Reads up to three analog pedal inputs (throttle, brake, clutch) and
// reports their 12-bit ADC values over USB CDC serial at 115 200 baud.
//
// Pin mapping (XIAO RP2040 default ADC pins):
//   A0 – throttle pedal
//   A1 – brake pedal
//   A2 – clutch pedal
//
// Output format (one line per report interval):
//   PEDALS=<throttle>,<brake>,<clutch>
//   e.g.  PEDALS=2048,0,4095
//
// Values are raw ADC counts (0..4095 for 12-bit, 0..1023 for 10-bit).

#define PIN_THROTTLE A0
#define PIN_BRAKE    A1
#define PIN_CLUTCH   A2

#define REPORT_INTERVAL_MS 10

static uint32_t lastMs = 0;

void setup(void) {
    Serial.begin(115200);
    analogReadResolution(12);  // RP2040 supports 12-bit ADC
}

void loop(void) {
    uint32_t now = millis();
    if ((now - lastMs) >= REPORT_INTERVAL_MS) {
        lastMs = now;
        uint16_t thr = (uint16_t)analogRead(PIN_THROTTLE);
        uint16_t brk = (uint16_t)analogRead(PIN_BRAKE);
        uint16_t clt = (uint16_t)analogRead(PIN_CLUTCH);
        char line[28];
        snprintf(line, sizeof(line), "PEDALS=%u,%u,%u", thr, brk, clt);
        Serial.println(line);
    }
}
