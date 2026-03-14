#include "WheelSettings.h"

WheelSettingsData activeSettings;
WheelSettingsData pendingSettings;

void settingsLoadDefaults() {
    activeSettings  = kDefaultSettings;
    pendingSettings = kDefaultSettings;
}

void settingsApplyPending() {
    activeSettings = pendingSettings;
}
