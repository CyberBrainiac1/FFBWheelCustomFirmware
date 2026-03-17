# FFB Wheel - DIY Force Feedback Steering Wheel

Windows test utility and firmware for a DIY USB force-feedback steering wheel.

| Folder                             | What it is                                                  |
| ---------------------------------- | ----------------------------------------------------------- |
| **desktop-app/**                   | Windows test utility (C# / WinForms / .NET 8)               |
| **firmware/leonardo-wheel/**       | Arduino Leonardo firmware (HID FFB wheel + serial config)    |
| **firmware/third_party/**          | Vendored Arduino libraries required by the wheel firmware    |
| **firmware/secondary-controller/** | Optional second controller template (pedals / buttons)      |
| **installer/**                     | Inno Setup installer configuration                          |
| **scripts/**                       | Flash helper scripts (flash_hex.bat, flash_hex.ps1)         |
| **versions/**                      | Versioned app + firmware release packages                   |
| **ui-preview/**                    | Browser preview of the desktop UI (works on Chromebook)     |

---

## SUPER SIMPLE SETUP

This section explains how to install and use the wheel tester from scratch,
in plain language.

### 1 — Install the app

**Option A (easiest): Use the installer**

1. Download `FFBWheelTester-Setup.exe` from the [Releases](https://github.com/CyberBrainiac1/FFBWheelCustomFirmware/releases) page.
2. Double-click the installer and follow the prompts.
3. A shortcut appears on the Start Menu (and optionally the Desktop).

**Option B: Run from source**

```
winget install Microsoft.DotNet.SDK.8
git clone https://github.com/CyberBrainiac1/FFBWheelCustomFirmware.git
cd FFBWheelCustomFirmware
dotnet run --project desktop-app/FFBWheelConfig.csproj
```

### 2 — Plug in the wheel controller

Connect your Arduino Leonardo to the PC via USB.
Windows will assign it a COM port (e.g. COM4).
Open Device Manager → Ports (COM & LPT) to find the port number.

### 3 — Flash the EMC-compatible hex (first time only)

The firmware hex is included in `versions/<latest>/firmware/leonardo-wheel.ino.hex`.

**Using the app:**

1. Open **FFB Wheel Tester**.
2. Select the COM port in the dropdown.
3. Click **Flash EMC Hex**.
4. Confirm the dialog — the board will reset into bootloader mode and be flashed.
5. Unplug and replug the wheel when prompted.

**Using the command line:**

```
scripts\flash_hex.bat COM4
```

or with PowerShell:

```
.\scripts\flash_hex.ps1 -Port COM4
```

Exact avrdude command (replace COM4 with your port):

```
avrdude -p atmega32u4 -c avr109 -P COM4 -b 57600 -D -U flash:w:versions\1.2.2\firmware\leonardo-wheel.ino.hex:i
```

### 4 — Open the app

Launch **FFB Wheel Tester** from the Start Menu or by running
`desktop-app\bin\Release\net8.0-windows\FFBWheelConfig.exe`.

### 5 — Select the correct COM port and connect

1. Click **Refresh** to scan for COM ports.
2. Select your wheel's COM port from the dropdown.
3. Click **Connect**. The status shows **Connected**.

### 6 — Start with low force

The force strength defaults to **20 %** for safety.
Keep it low until you know the wheel behaves correctly.
Increase it gradually with the slider or the **Up** arrow key.

### 7 — Use keyboard controls to test left/right force

While the app is focused:

| Key         | Action                        |
| ----------- | ----------------------------- |
| Left arrow  | Apply left force (hold)       |
| Right arrow | Apply right force (hold)      |
| Space       | Stop force immediately        |
| C           | Center / spring-to-center     |
| Up arrow    | Increase force strength +5 %  |
| Down arrow  | Decrease force strength -5 %  |

Or click the on-screen buttons: **Turn Left**, **Turn Right**, **Center**, **Stop Force**.

> **Safety:** Keep hands clear while testing. The wheel can move suddenly.
> Use the red **EMERGENCY STOP** button at any time.

---

## What you need

### Hardware

- Arduino Leonardo
- BTS7960 motor driver
- 600 PPR quadrature encoder
- DC motor (for force feedback)
- USB cable (micro-USB to PC)
- Optional: second microcontroller for pedals (e.g. Seeed XIAO RP2040)

### Software

- Windows 10 or 11 (for the desktop app)
- .NET 8 SDK **or** .NET 8 Desktop Runtime
- arduino-cli (for compiling and flashing firmware)
- Git (to download this repository)
- avrdude (for command-line flashing; bundled with arduino-cli)

---

## Safety

> **Warning: Force feedback wheels can move suddenly and with significant force.**

Follow these precautions at all times:

- **Keep hands clear** of the wheel rim and motor while testing.
- **Start with low force** (20 % or less). Increase only after verifying safe behaviour.
- Use the **EMERGENCY STOP** button in the app to cut force instantly.
- Force is automatically stopped when you disconnect or close the app.
- Force is automatically stopped after flashing firmware.
- If communication fails, force output stops automatically.
- **Never leave the wheel running unattended.**

---

## Quick start (developer / source build)

### Before cloning — install prerequisites

> **Already have the repo cloned?** Skip ahead to [After cloning](#after-cloning--build-and-flash).

**Windows** (run each line in Command Prompt or PowerShell):

```
winget install Git.Git
winget install ArduinoSA.CLI
winget install Microsoft.DotNet.SDK.8
```

Close and reopen your terminal so the new tools are on your PATH, then:

```
arduino-cli core update-index
arduino-cli core install arduino:avr
```

Then clone the repo:

```
git clone https://github.com/CyberBrainiac1/FFBWheelCustomFirmware.git
cd FFBWheelCustomFirmware
```

---

### After cloning — build and flash

#### Step 1 — Compile the firmware

**Windows:**

```
arduino-cli compile --fqbn arduino:avr:leonardo --library firmware\third_party\ArduinoJoystickWithFFBLibrary --output-dir firmware\leonardo-wheel\build firmware\leonardo-wheel
```

**Linux / macOS:**

```
arduino-cli compile --fqbn arduino:avr:leonardo --library firmware/third_party/ArduinoJoystickWithFFBLibrary --output-dir firmware/leonardo-wheel/build firmware/leonardo-wheel
```

This produces `firmware/leonardo-wheel/build/leonardo-wheel.ino.hex`.

#### Step 2 — Flash the firmware

Plug in your Arduino Leonardo via USB. Find its port:

```
arduino-cli board list
```

Then flash (replace `COM4` / `/dev/ttyACM0` with your actual port):

**Windows:**

```
arduino-cli upload -p COM4 --fqbn arduino:avr:leonardo --input-dir firmware\leonardo-wheel\build
```

**Linux / macOS:**

```
arduino-cli upload -p /dev/ttyACM0 --fqbn arduino:avr:leonardo --input-dir firmware/leonardo-wheel/build
```

Or use the bundled scripts:

```
scripts\flash_hex.bat COM4
```

Or avrdude directly:

```
avrdude -p atmega32u4 -c avr109 -P COM4 -b 57600 -D -U flash:w:firmware\leonardo-wheel\build\leonardo-wheel.ino.hex:i
```

> **Tip:** If the upload fails, double-press the reset button on the Leonardo to enter the bootloader, then immediately re-run the upload command.

#### Step 3 — Build and run the desktop app

```
dotnet restore desktop-app/FFBWheelConfig.csproj
dotnet build desktop-app/FFBWheelConfig.csproj --configuration Release
```

**Windows:**

```
desktop-app\bin\Release\net8.0-windows\FFBWheelConfig.exe
```

#### Step 3A — Create a versioned release package

```
build_versioned_release.bat
```

This will:
- increment the next `1.2.x` version
- stamp that version into the firmware response string
- publish the desktop app as a Windows `.exe`
- build the Leonardo `.hex`
- create a new folder under `versions/<version>/`

#### Step 4 — Connect and test

1. Plug in the Arduino Leonardo via USB.
2. Open the desktop app.
3. Select the COM port from the dropdown (click **Refresh** to scan).
4. Click **Connect**.
5. Set force strength to 20 % or lower to start safely.
6. Use keyboard arrows or buttons to test left/right force.
7. Press **Space** or **EMERGENCY STOP** to stop force at any time.

---

## Keyboard shortcuts

| Key         | Action                        |
| ----------- | ----------------------------- |
| Left arrow  | Apply left force (hold)       |
| Right arrow | Apply right force (hold)      |
| Space       | Stop force immediately        |
| C           | Center / spring-to-center     |
| Up arrow    | Increase force strength +5 %  |
| Down arrow  | Decrease force strength -5 %  |

"Stop force on key release" checkbox: when enabled (default), releasing the
left/right key stops force automatically.

---

## Test presets

| Button       | Action                                        |
| ------------ | --------------------------------------------- |
| Pulse Left   | Rapid left/stop pulses (300 ms cycle)          |
| Pulse Right  | Rapid right/stop pulses (300 ms cycle)         |
| Oscillate    | Alternates left/right (400 ms cycle)           |
| Stop (preset)| Stops all preset timers and force output       |

---

## Building the installer

Requires [Inno Setup 6](https://jrsoftware.org/isdl.php).

```
installer\build_installer.bat
```

This publishes the app as a self-contained Windows exe and builds
`installer\FFBWheelTester-Setup.exe`.

---

## Project structure

```
FFBWheelCustomFirmware/
├── desktop-app/                     Windows test utility
│   ├── FFBWheelConfig.csproj
│   ├── Program.cs
│   ├── Forms/MainForm.cs            Single-window dark UI
│   ├── Models/
│   │   ├── ForceDirection.cs        Force direction enum
│   │   ├── ForceCommand.cs          Force command model
│   │   ├── WheelSettings.cs         Settings data model
│   │   └── WheelLiveState.cs        Live state data model
│   └── Services/
│       ├── SerialWheelClient.cs     Low-level serial port wrapper
│       ├── WheelControllerService.cs Service coordinator + polling + test commands
│       ├── WheelProtocolParser.cs   Protocol parser
│       ├── FirmwareFlasher.cs       avrdude flash helper
│       └── AppSettings.cs           Persisted user preferences
├── firmware/
│   ├── leonardo-wheel/              Arduino Leonardo firmware
│   │   ├── leonardo-wheel.ino        Main sketch
│   │   ├── EncoderReader.h/.cpp     Interrupt-driven quadrature encoder
│   │   ├── MotorDriver.h/.cpp       BTS7960 PWM motor control
│   │   ├── WheelSettings.h/.cpp     Settings struct + defaults
│   │   ├── EepromStorage.h/.cpp     EEPROM persistence
│   │   ├── SerialProtocol.h/.cpp    Serial command interface (incl. TEST_FORCE)
│   │   ├── BuildVersion.h           Release-stamped firmware version
│   │   └── WheelMath.h/.cpp         Angle math + FFB calculation
│   └── secondary-controller/        Optional pedal/button controller
├── installer/
│   ├── setup.iss                    Inno Setup configuration
│   └── build_installer.bat          Publish + build installer helper
├── scripts/
│   ├── flash_hex.bat                Flash firmware (Windows CMD)
│   └── flash_hex.ps1                Flash firmware (PowerShell)
├── versions/                        Versioned app + firmware packages
├── ui-preview/                      Browser-based UI preview
├── build_versioned_release.bat      Windows versioned release build
├── build_versioned_release.ps1      Creates next versioned package
├── FFBWheelConfig.slnx              Visual Studio solution file
└── README.md                        <- you are here
```

---

## Serial protocol

The desktop app and firmware communicate over USB serial at **115 200 baud, 8N1**.

All successful commands reply with `OK`. Unknown commands reply with `ERROR INVALID_COMMAND`.

### Configuration commands (app -> firmware)

| Command             | What it does                                                  |
| ------------------- | ------------------------------------------------------------- |
| `GET_SETTINGS`      | Request all settings (returns BEGIN_SETTINGS block)           |
| `GET_LIVE_STATE`    | Request current angle + raw counts (returns BEGIN_LIVE block) |
| `SET FORCE 60`      | Set overall force (0-100)                                     |
| `SET MIN_FORCE 5`   | Set minimum force (0-100)                                     |
| `SET DAMPING 10`    | Set damping (0-100)                                           |
| `SET FRICTION 4`    | Set friction (0-100)                                          |
| `SET SPRING 15`     | Set spring (0-100)                                            |
| `SET RANGE 900`     | Set steering range (90-1800 deg)                              |
| `SET CENTER 12345`  | Set center to a specific encoder value                        |
| `SET INV_ENCODER 0` | Invert encoder (0 or 1)                                       |
| `SET INV_MOTOR 0`   | Invert motor (0 or 1)                                         |
| `SET_CENTER_NOW`    | Set current encoder position as centre                        |
| `APPLY`             | Apply pending settings                                        |
| `SAVE`              | Persist active settings to EEPROM                             |
| `LOAD_DEFAULTS`     | Reset to compiled defaults                                    |

### Test-force commands (app -> firmware)

These commands drive the motor directly for bench testing.

| Command                 | What it does                                                  |
| ----------------------- | ------------------------------------------------------------- |
| `TEST_FORCE LEFT [0-100]`  | Constant left force at given strength %                    |
| `TEST_FORCE RIGHT [0-100]` | Constant right force at given strength %                   |
| `TEST_FORCE CENTER`        | Spring-to-center (uses current spring and force settings)  |
| `TEST_FORCE STOP`          | Stop all test forces, resume normal FFB                    |

Test force overrides the HID FFB path while active. `TEST_FORCE STOP` returns
to normal HID-controlled FFB operation.

### Settings response (firmware -> app)

```
BEGIN_SETTINGS
FORCE=60
MIN_FORCE=5
DAMPING=10
FRICTION=4
SPRING=15
RANGE=900
CENTER=0
INV_ENCODER=0
INV_MOTOR=0
FW_VERSION=1.2.2
END_SETTINGS
```

### Live state response (firmware -> app)

```
BEGIN_LIVE
LIVE_ANGLE=42
RAW_COUNTS=12054
END_LIVE
```

---

## Default settings

| Setting        | Default |
| -------------- | ------- |
| Overall force  | 60 %    |
| Minimum force  | 5 %     |
| Damping        | 10 %    |
| Friction       | 4 %     |
| Spring         | 15 %    |
| Steering range | 900 deg |
| Invert encoder | Off     |
| Invert motor   | Off     |
| Test force %   | 20 %    |

---

## Troubleshooting

| Problem                              | Solution                                                                                                                                                              |
| ------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Wheel not detected / no COM port     | Unplug and re-plug the Leonardo. Click Refresh in the app. On Windows, check Device Manager -> Ports.                                                                 |
| Wrong COM port                       | Open Device Manager -> Ports (COM & LPT). Look for "Arduino Leonardo" or "USB Serial Device".                                                                         |
| avrdude fails                        | The bootloader port only appears for a few seconds after reset. Try double-pressing the reset button, then immediately re-run the flash command.                       |
| Can't find the COM port              | Unplug and re-plug the Leonardo. Click Refresh in the app.                                                                                                            |
| Build fails with "core not found"    | Run `arduino-cli core install arduino:avr`                                                                                                                            |
| `dotnet` command not found           | Install the .NET 8 SDK: `winget install Microsoft.DotNet.SDK.8`                                                                                                       |
| App shows "Disconnected" immediately | Check that no other program (Arduino IDE Serial Monitor, etc.) has the COM port open.                                                                                 |
| No force response after connecting   | Click Refresh, reconnect, check the overall force % is above 0. Try pressing Left or Right arrow key.                                                                 |
| Wheel moves wrong direction          | Check the "Invert Motor" setting in firmware. Use SET INV_MOTOR 1 and APPLY, or adjust via the firmware defaults.                                                    |
| Emergency stop use                   | Click the red EMERGENCY STOP button at any time. Force stops immediately and stays off until you send another command.                                                |
| Flash fails - hex not found          | Build the firmware first: `arduino-cli compile --fqbn arduino:avr:leonardo ...` or run `build_versioned_release.bat`                                                  |
| App closes without stopping force    | The app sends TEST_FORCE STOP automatically on close and disconnect. If communication was already lost, power-cycle the wheel.                                         |

---

## License

This project is provided as-is for educational and personal use.
