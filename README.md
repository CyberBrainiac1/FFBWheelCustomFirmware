# EMC FFB Tester

A Windows desktop utility for testing force feedback on an already-flashed DIY steering wheel.

Connect to the wheel, press keys, and send test force commands — left, right, center, stop.

> **This app does not flash firmware.** The wheel must already be running EMC-compatible firmware before use.

---

## Overview

**EMC FFB Tester** is a compact, dark-themed engineering utility for bench-testing and tuning a DIY USB force-feedback steering wheel. It communicates with a wheel that is already flashed and running via DirectInput (HID FFB), and lets you send simple test force commands without writing any code.

---

## Features

- Connect to any DirectInput force-feedback device
- Turn Left / Turn Right / Center / Stop Force buttons
- Keyboard-driven force testing (hold Left/Right arrows, Space to stop)
- Adjustable force strength (0–100 %, default 20 %)
- Test presets: Constant Left, Constant Right, Center Spring, Pulse Left, Pulse Right, Oscillate
- Stop on key release (configurable)
- Emergency Stop button — always visible
- Dark, flat UI — no clutter
- Settings saved across sessions

---

## SUPER SIMPLE SETUP

### Step 1 — Install the app

**Option A (easiest): Use the installer**

1. Download `EMCFFBTester-Setup.exe` from the [Releases](https://github.com/CyberBrainiac1/FFBWheelCustomFirmware/releases) page.
2. Double-click the installer and follow the on-screen prompts.
3. A shortcut appears in the Start Menu (and optionally on the Desktop).

**Option B: Run from source**

```
winget install Microsoft.DotNet.SDK.8
git clone https://github.com/CyberBrainiac1/FFBWheelCustomFirmware.git
cd FFBWheelCustomFirmware
dotnet run --project wheel-ffb-tester/FFBWheelTester.csproj
```

### Step 2 — Plug in the wheel controller

Connect your wheel/controller to the PC via USB. Windows will recognise it as a HID force-feedback joystick device. No drivers are needed for most setups.

### Step 3 — Open the app

Launch **EMC FFB Tester** from the Start Menu shortcut, or run `FFBWheelTester.exe` directly.

### Step 4 — Choose the device

1. Click **Refresh** to scan for connected force-feedback devices.
2. Select your wheel from the **Device** dropdown.
   - The selected device name appears in the **Selected:** label below the dropdown.

### Step 5 — Connect

Click **Connect**.

The status label changes to **Status: Connected** when the connection is successful.

If connection fails, check that no other application (a game, another utility) has the device open.

### Step 6 — Keep force low at first

The force strength defaults to **20 %** when you first launch the app.
This is intentional — it keeps the wheel safe while you check everything is working.

Increase force gradually using:
- the **Force Strength** slider
- the numeric box next to the slider
- **Up** / **Down** arrow keys (+5 % / −5 % per press)

### Step 7 — Test left/right with keyboard

With the app window focused:

| Key         | Action                        |
| ----------- | ----------------------------- |
| Left arrow  | Apply left force (hold)       |
| Right arrow | Apply right force (hold)      |
| Space       | Stop force immediately        |
| C           | Center / spring-to-center     |
| Up arrow    | Increase force strength +5 %  |
| Down arrow  | Decrease force strength -5 %  |

The large **STATUS** label in the middle of the app shows the current force state in real time.

---

## What each button does

### Device bar (top)

| Control       | What it does                                              |
| ------------- | --------------------------------------------------------- |
| Refresh       | Scans for connected force-feedback devices                |
| Connect       | Opens the selected device and enables force output        |
| Disconnect    | Stops force and releases the device                       |

### Manual Force Controls

| Button       | What it does                                              |
| ------------ | --------------------------------------------------------- |
| Turn Left    | Applies constant left force while held (mouse down)       |
| Turn Right   | Applies constant right force while held (mouse down)      |
| Center       | Activates spring-to-center effect                         |
| Stop Force   | Stops all active force immediately                        |

### Test Presets

| Button        | What it does                                              |
| ------------- | --------------------------------------------------------- |
| Constant Left | Applies steady left force until stopped                   |
| Constant Right| Applies steady right force until stopped                  |
| Center Spring | Activates spring-to-center effect                         |
| Pulse Left    | Alternates left force / no force (600 ms full cycle, 300 ms per state) |
| Pulse Right   | Alternates right force / no force (600 ms full cycle, 300 ms per state)|
| Oscillate     | Alternates left / right force (800 ms full cycle, 400 ms per state)    |
| Stop          | Stops all preset timers and force output                  |

### Bottom bar

| Control         | What it does                                            |
| --------------- | ------------------------------------------------------- |
| EMERGENCY STOP  | Stops all force immediately (red button, always visible) |

---

## Keyboard Controls

| Key         | Action                        |
| ----------- | ----------------------------- |
| Left arrow  | Apply left force (hold)       |
| Right arrow | Apply right force (hold)      |
| Space       | Stop force immediately        |
| C           | Center / spring-to-center     |
| Up arrow    | Increase force strength +5 %  |
| Down arrow  | Decrease force strength -5 %  |

**Stop on key release** (checkbox in the force panel):
When checked (default), releasing the Left or Right arrow key stops force automatically.
Uncheck it if you want force to stay on after releasing the key.

---

## Safety

> **Warning: Force feedback wheels can move suddenly and with significant force.**

- **Keep hands clear** of the wheel rim and motor during testing.
- **Start with low force** (20 % or less). Increase only after confirming safe behaviour.
- Use the red **EMERGENCY STOP** button at any time to cut force instantly.
- Force is automatically stopped when you disconnect or close the app.
- If a force command fails, force output stops automatically.
- Never leave the wheel running unattended.

---

## Troubleshooting

| Problem                              | Solution                                                                                                                                             |
| ------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| No device in the dropdown            | Make sure the wheel is plugged in before clicking Refresh. The device must appear as a HID force-feedback joystick in Windows.                       |
| Wrong device selected                | Click Refresh, select the correct device from the dropdown, and click Connect.                                                                       |
| Connect fails                        | Close any game, simulator, or other utility that might have the device open. Only one app can use a DirectInput device at a time.                    |
| No force output                      | Check that force strength is above 0 %. Make sure the device is connected (Status: Connected). Try pressing a preset button to send a fresh command. |
| Wheel moves wrong direction          | The direction is set in the firmware. This app sends force in the direction you choose; if the wheel turns the wrong way, the firmware motor direction needs adjusting. |
| Disconnect during testing            | The app stops force automatically on disconnect. Reconnect and resume testing.                                                                       |
| App closes / crashes                 | Force is stopped automatically when the app closes. If the wheel keeps moving, power-cycle the controller.                                           |
| Status shows "Not Connected"         | Click Refresh to re-scan devices, select your wheel, and click Connect.                                                                              |

---

## Project structure

```
FFBWheelCustomFirmware/
├── wheel-ffb-tester/            EMC FFB Tester app (this app)
│   ├── FFBWheelTester.csproj
│   ├── Program.cs
│   ├── Forms/MainForm.cs        Single-window dark UI
│   ├── Models/
│   │   ├── ForceDirection.cs    Force direction enum
│   │   └── ForceCommand.cs      Force command model
│   └── Services/
│       ├── WheelTesterService.cs  High-level wheel controller
│       ├── FfbWheelDevice.cs      DirectInput FFB device wrapper
│       ├── DeviceManager.cs       Device discovery
│       └── AppSettings.cs         Persisted user preferences
├── desktop-app/                 FFB Wheel Config app (settings/tuning)
├── firmware/leonardo-wheel/     Arduino Leonardo firmware
├── installer/                   Inno Setup installer script
├── versions/                    Versioned release packages
└── README.md                    <- you are here
```

---

## Building the installer

Requires [Inno Setup 6](https://jrsoftware.org/isdl.php).

```
installer\build_installer.bat
```

This publishes the app as a self-contained Windows executable and produces
`installer\EMCFFBTester-Setup.exe`.

---

## License

This project is provided as-is for educational and personal use.
