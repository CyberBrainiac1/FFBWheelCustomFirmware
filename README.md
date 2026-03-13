# FFB Wheel Config

A compact, dark Windows desktop utility for configuring a DIY force-feedback steering wheel controller based on an Arduino Leonardo.

---

## What it does

- Connects to the wheel controller over a USB serial (COM) port
- Reads current settings from the wheel (force levels, steering range, encoder centre, etc.)
- Allows editing those settings in a clean, single-window UI
- Applies settings temporarily (`Apply`) or persists them to EEPROM (`Save to Wheel`)
- Resets settings to firmware defaults (`Reset Defaults`)
- Displays a **live steering angle** that mirrors the same processed steering value the firmware reports to the PC / game
- Shows raw encoder counts as secondary information

---

## What it does NOT do

- Flash or update firmware
- Upload pedal calibration data
- Show telemetry dashboards or graphs
- Provide a game launcher
- Replace the Arduino firmware

---

## Requirements

- Windows 10 / 11
- .NET 8 Desktop Runtime
- Arduino Leonardo running compatible FFB wheel firmware (already flashed)

---

## Serial Protocol

Baud rate: **115 200 8N1**

### Commands (app → wheel)

| Command | Description |
|---|---|
| `GET_SETTINGS` | Request all wheel settings |
| `GET_LIVE_STATE` | Request current live angle and raw counts |
| `SET FORCE <0-100>` | Overall force percentage |
| `SET MIN_FORCE <0-100>` | Minimum force |
| `SET DAMPING <0-100>` | Damping level |
| `SET FRICTION <0-100>` | Friction level |
| `SET SPRING <0-100>` | Spring level |
| `SET RANGE <90-1800>` | Steering range in degrees |
| `SET INV_ENCODER <0\|1>` | Invert encoder direction |
| `SET INV_MOTOR <0\|1>` | Invert motor direction |
| `APPLY` | Apply all pending settings |
| `SAVE` | Persist current settings to EEPROM |
| `LOAD_DEFAULTS` | Reset to firmware defaults (then re-reads settings) |
| `SET_CENTER` | Set current wheel position as the steering centre |

### Settings block (wheel → app)

```
BEGIN_SETTINGS
FORCE=60
MIN_FORCE=5
DAMPING=10
FRICTION=4
SPRING=15
RANGE=900
CENTER=12345
INV_ENCODER=0
INV_MOTOR=0
FW_VERSION=1.0.0
END_SETTINGS
```

### Live state (wheel → app)

Either as a structured block:

```
BEGIN_LIVE
LIVE_ANGLE=42
RAW_COUNTS=12054
END_LIVE
```

Or as standalone lines (both formats are accepted):

```
LIVE_ANGLE=42
RAW_COUNTS=12054
```

### Live angle note

`LIVE_ANGLE` must represent the **same processed steering position** the firmware reports to the PC/game over USB HID.  
The app polls `GET_LIVE_STATE` every 100 ms and displays the result in the large central readout.  
Raw encoder counts are displayed in small secondary text only.

---

## Architecture

```
FFBWheelConfig/
├── Models/
│   ├── WheelSettings.cs        – settings data model
│   └── WheelLiveState.cs       – live-state data model
├── Services/
│   ├── WheelProtocolParser.cs  – parses serial protocol messages
│   ├── SerialWheelClient.cs    – low-level serial port wrapper
│   └── WheelControllerService.cs – coordinates client, parser, polling timer
├── Forms/
│   └── MainForm.cs             – single-window UI (no Designer file; fully code-first)
└── Program.cs
```

---

## Building from source

```
dotnet build --configuration Release
```

Requires .NET 8 SDK with `EnableWindowsTargeting=true` (already set in the `.csproj`).
