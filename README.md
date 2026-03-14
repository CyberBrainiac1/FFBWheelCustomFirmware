# FFB Wheel — DIY Force Feedback Steering Wheel

Everything you need to build and configure a USB force-feedback steering wheel.

| Folder | What it is |
|---|---|
| **desktop-app/** | Windows configuration utility (C# / WinForms / .NET 8) |
| **firmware/leonardo-wheel/** | Arduino Leonardo firmware (encoder + motor + serial config) |
| **firmware/secondary-controller/** | Optional second controller template (pedals / buttons) |
| **ui-preview/** | Browser preview of the desktop UI (works on Chromebook) |

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

---

## Quick start — step by step

**Windows users:** PowerShell scripts are provided for every step so you only need to copy-paste one command at a time. Follow the **Windows (PowerShell)** sections below.

**Linux / macOS users:** Use the **Terminal** sections.

---

### Step 1 — Download this repository

Install Git if you don't have it:

**Windows:**
```
winget install Git.Git
```

Then clone the repo:

```
git clone https://github.com/CyberBrainiac1/FFBWheelCustomFirmware.git
cd FFBWheelCustomFirmware
```

---

### Step 2 — Install prerequisites (Windows only)

Run this **once** from the repo root. It installs arduino-cli, the .NET 8 SDK, and the AVR board core automatically:

```
powershell -ExecutionPolicy Bypass -File setup.ps1
```

> **Linux / macOS** — install prerequisites manually:
> ```
> # arduino-cli
> curl -fsSL https://raw.githubusercontent.com/arduino/arduino-cli/master/install.sh | sh
> sudo mv bin/arduino-cli /usr/local/bin/
> arduino-cli core install arduino:avr
>
> # .NET 8 SDK — https://dotnet.microsoft.com/en-us/download/dotnet/8.0
> ```

---

### Step 3 — Flash the firmware to your Arduino Leonardo

#### 3a. Build the firmware

**Windows (PowerShell):**

```
powershell -ExecutionPolicy Bypass -File firmware\leonardo-wheel\build_firmware.ps1
```

**Linux / macOS:**

```
chmod +x firmware/leonardo-wheel/build_firmware.sh
./firmware/leonardo-wheel/build_firmware.sh
```

You should see `Build succeeded` and a `.hex` file in `firmware/leonardo-wheel/build/`.

#### 3b. Flash the firmware

Plug in your Arduino Leonardo via USB.

**Windows (PowerShell) — auto-detects port:**

```
powershell -ExecutionPolicy Bypass -File firmware\leonardo-wheel\flash_firmware.ps1
```

Or specify the port explicitly:

```
powershell -ExecutionPolicy Bypass -File firmware\leonardo-wheel\flash_firmware.ps1 -Port COM4
```

**Linux / macOS:**

```
./firmware/leonardo-wheel/flash_firmware.sh /dev/ttyACM0
```

On Windows the script auto-detects the port. On Linux/macOS, replace `/dev/ttyACM0` with your actual port (run `arduino-cli board list` to find it).

---

### Step 4 — Run the desktop configuration app

**Windows (PowerShell) — builds and launches in one command:**

```
powershell -ExecutionPolicy Bypass -File desktop-app\run_desktop_app.ps1
```

**Linux / macOS (manual steps):**

```
dotnet restore desktop-app/FFBWheelConfig.csproj
dotnet build   desktop-app/FFBWheelConfig.csproj --configuration Release
dotnet run --project desktop-app/FFBWheelConfig.csproj --configuration Release
```

Or run the exe directly after building:

```
desktop-app\bin\Release\net8.0-windows\FFBWheelConfig.exe
```

---

### Step 5 — Connect and configure

1. Plug in the Arduino Leonardo via USB.
2. Open the desktop app.
3. Select the COM port from the dropdown (click **↻** to refresh).
4. Click **Connect**. The status dot turns green.
5. Click **Read from Wheel** to load current settings.
6. Adjust sliders (force, damping, friction, spring, steering range).
7. Click **Apply** to send settings (active until power cycle).
8. Click **Save to Wheel** to write settings to EEPROM (persists across reboots).

---

### Step 6 (optional) — Preview the UI in a browser

If you want to see what the desktop app looks like without installing .NET (for example on a Chromebook):

```
cd ui-preview
```

Open `index.html` in any browser. Click **Connect** to see a simulated live angle display.

---

## Project structure

```
FFBWheelCustomFirmware/
├── setup.ps1                        One-shot prerequisite installer (Windows)
├── desktop-app/                     Windows configuration utility
│   ├── FFBWheelConfig.csproj
│   ├── Program.cs
│   ├── build_desktop_app.ps1        Build the desktop app (Windows)
│   ├── run_desktop_app.ps1          Build + launch the desktop app (Windows)
│   ├── Forms/MainForm.cs            Single-window dark UI
│   ├── Models/
│   │   ├── WheelSettings.cs         Settings data model
│   │   └── WheelLiveState.cs        Live state data model
│   └── Services/
│       ├── SerialWheelClient.cs     Low-level serial port wrapper
│       ├── WheelControllerService.cs Service coordinator + polling
│       └── WheelProtocolParser.cs   Protocol parser
├── firmware/
│   ├── leonardo-wheel/              Arduino Leonardo firmware
│   │   ├── LeonardoWheel.ino        Main sketch
│   │   ├── EncoderReader.h/.cpp     Quadrature encoder (D2/D3)
│   │   ├── MotorDriver.h/.cpp       BTS7960 motor control
│   │   ├── WheelSettings.h/.cpp     Settings struct + defaults
│   │   ├── EepromStorage.h/.cpp     EEPROM persistence
│   │   ├── SerialProtocol.h/.cpp    Serial command interface
│   │   ├── WheelMath.h/.cpp         Angle math + FFB calculation
│   │   ├── build_firmware.bat/.sh/.ps1  Compile to .hex
│   │   ├── flash_firmware.bat/.sh/.ps1  Flash via avrdude
│   │   └── README.md
│   └── secondary-controller/        Optional pedal/button controller
│       ├── SecondaryController.ino
│       └── README.md
├── ui-preview/                      Browser-based UI preview
│   ├── index.html
│   ├── styles.css
│   └── script.js
├── FFBWheelConfig.slnx              Visual Studio solution file
└── README.md                        ← you are here
```

---

## Serial protocol

The desktop app and firmware talk over USB serial at **115 200 baud, 8N1**.

All successful commands reply with `OK`. Unknown commands reply with `ERROR INVALID_COMMAND`.

### Commands (app → firmware)

| Command | What it does |
|---|---|
| `GET_SETTINGS` | Request all settings (returns BEGIN_SETTINGS block) |
| `GET_LIVE_STATE` | Request current angle + raw counts (returns BEGIN_LIVE block) |
| `SET FORCE 60` | Set overall force (0–100) |
| `SET MIN_FORCE 5` | Set minimum force (0–100) |
| `SET DAMPING 10` | Set damping (0–100) |
| `SET FRICTION 4` | Set friction (0–100) |
| `SET SPRING 15` | Set spring (0–100) |
| `SET RANGE 900` | Set steering range (90–1800°) |
| `SET CENTER 12345` | Set center to a specific encoder value |
| `SET INV_ENCODER 0` | Invert encoder (0 or 1) |
| `SET INV_MOTOR 0` | Invert motor (0 or 1) |
| `SET_CENTER_NOW` | Set current encoder position as centre |
| `APPLY` | Apply pending settings |
| `SAVE` | Persist active settings to EEPROM |
| `LOAD_DEFAULTS` | Reset to compiled defaults |

### Settings response (firmware → app)

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
FW_VERSION=1.1.0
END_SETTINGS
```

### Live state response (firmware → app)

```
BEGIN_LIVE
LIVE_ANGLE=42
RAW_COUNTS=12054
END_LIVE
```

---

## Default settings

| Setting | Default |
|---|---|
| Overall force | 60 % |
| Minimum force | 5 % |
| Damping | 10 % |
| Friction | 4 % |
| Spring | 15 % |
| Steering range | 900° |
| Invert encoder | Off |
| Invert motor | Off |

---

## Troubleshooting

| Problem | Solution |
|---|---|
| Can't find the COM port | Unplug and re-plug the Leonardo. Click ↻ in the app. On Windows, check Device Manager → Ports. |
| Build fails with "core not found" | Run `arduino-cli core install arduino:avr` |
| Flash fails | The Leonardo's bootloader port only appears for a few seconds after reset. Try double-pressing the reset button on the board, then immediately run the flash command. |
| `dotnet` command not found | Install the .NET 8 SDK: `winget install Microsoft.DotNet.SDK.8` |
| App shows "Disconnected" immediately | Check that no other program (Arduino IDE Serial Monitor, etc.) has the COM port open. |

---

## License

This project is provided as-is for educational and personal use.
