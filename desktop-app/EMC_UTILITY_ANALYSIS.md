## EMC Utility Lite Analysis

Source installer:
- `desktop-app/EMCUtillityLite setup.exe`

Extracted files:
- `desktop-app/emc_utility_unpack/app/EMCLite.exe`
- `desktop-app/emc_utility_unpack/app/EMC.dll`
- `desktop-app/emc_utility_unpack/app/EMCHid.dll`
- `desktop-app/emc_utility_unpack/app/MaterialDesignThemes.Wpf.dll`
- `desktop-app/emc_utility_unpack/app/MaterialDesignColors.dll`

Decompiled output:
- `desktop-app/emc_utility_decompiled/EMCLite/`
- `desktop-app/emc_utility_decompiled/EMC/`
- `desktop-app/emc_utility_decompiled/EMCHid/`

## Packaging

- Installer format: Inno Setup
- Runtime: .NET Framework 4.6.1
- UI framework: WPF
- Theme library: `MaterialDesignThemes.Wpf` 1.2.0.33900

## Application Split

- `EMCLite.exe`: WPF window and UI event wiring
- `EMC.dll`: wheel view model / command layer
- `EMCHid.dll`: HID device transport layer

This is not a serial-port utility. The EMC desktop app talks to the wheel over HID.

## Visible UI Structure

Runtime UI automation shows a compact single-window utility around `300 x 438` px.

Top area:
- `EMC Utility Lite` title
- three radio-button sections: `Steering`, `Pedal`, `Force`
- settings button
- minimize and close buttons

Steering page:
- angle label/value
- `angleslider`
- `centerbutton`
- profile combo box `configcb`
- icon buttons for save/delete/load profile
- footer text `Firmware ver : Unknown`

Pedal page controls discovered from XAML/code-behind:
- accel and brake progress bars
- clutch enable checkbox with min/max buttons
- handbrake enable checkbox with min/max buttons

Force page controls discovered from XAML/code-behind:
- `softlockslider`
- `damperslider`
- `frictionslider`
- `inertiaslider`

Settings overlay controls discovered from XAML/code-behind:
- encoder CPR textbox `cprval`
- H-bridge mode combo box `configcbdriver`
- save/close buttons

Other UI elements:
- PayPal / Facebook / YouTube icon buttons
- local snackbar notifications

## Local Persistence

`EMCLite.Properties.Settings` stores:
- `sliderangle`
- `softlock`
- `damper`
- `inertia`
- `friction`
- `enableclutch`
- `enablehandbrake`
- `cpr`
- `hbridge`
- `array` for saved profiles

The profile system is local desktop-side storage, not wheel-side presets.

## Commands and Model Surface

Recovered property / command names from `EMC.dll` and BAML strings:
- `DoSetCenter`
- `GetSoftlock`
- `GetSpring`
- `GetDamper`
- `GetFriction`
- `GetInertia`
- `GetSetCpr`
- `HbridgeMode`
- `DisableClutch`
- `DisableHandbrake`
- `SetSoftlock`
- `SetSpring`
- `SetDamper`
- `SetFriction`
- `SetInertia`
- `SetCenter`
- `SetCpr`
- `SetClutchmin`
- `SetClutchmax`
- `SetHandbrakemin`
- `SetHandbrakemax`

There are also strings like:
- `firmwareCheck`
- `firmwarepass`
- `findCPR`
- `findCPRValue`

That suggests the app can read firmware state and perform encoder CPR-related setup.

## HID Transport Surface

Recovered transport/event names from `EMCHid.dll`:
- `DeviceList`
- `ConnectionStatus`
- `RaiseSendPacketEvent`
- `RaisePacketSentEvent`
- `RaiseReceivePacketEvent`
- `RaisePacketReceivedEvent`
- `RaiseReceiveGetreportEvent`
- `RaiseGetreportReceivedEvent`
- `UsbThread_DoWork`

That strongly suggests a raw HID packet/get-report transport, not just feature reports and not COM.

## Limits

- `EMC.dll` and `EMCHid.dll` are at least partially obfuscated.
- Method bodies do not fully decompile cleanly.
- The command/property surface and the real visible UI are recoverable, but not every implementation detail.

## Implication For Our App

If we want to follow EMC Utility Lite closely, the important parts are:
- HID transport, not serial
- a compact single-window workflow
- a small fixed control set
- force settings centered on softlock/damper/friction/inertia
- wheel setup items like CPR and H-bridge mode
- pedal calibration and clutch/handbrake toggles

Our current app is not laid out or scoped the same way. Matching EMC more literally would require transport and feature changes, not just visual changes.
