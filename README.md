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
- .NET 8 Desktop Runtime (to run the app) **or** .NET 8 SDK (to build from source)
- Arduino Leonardo running compatible FFB wheel firmware (already flashed)
- USB cable to connect the Arduino Leonardo to your PC

---

## Installation

### Option A – Run a pre-built release

1. **Download** the latest release from the [Releases](../../releases) page.
2. **Install the .NET 8 Desktop Runtime** (if not already installed).
   Open PowerShell and run:
   ```powershell
   winget install Microsoft.DotNet.DesktopRuntime.8
   ```
   Or download it manually from <https://dotnet.microsoft.com/en-us/download/dotnet/8.0> (look for
   ".NET Desktop Runtime 8.x" → Windows x64 installer).
3. **Extract** the downloaded zip and run `FFBWheelConfig.exe`.

### Option B – Build from source

> Requires the **.NET 8 SDK** (the SDK includes the runtime, so a separate runtime install is not needed).

**1. Install the .NET 8 SDK**

```powershell
winget install Microsoft.DotNet.SDK.8
```

Or download the installer from <https://dotnet.microsoft.com/en-us/download/dotnet/8.0>
(look for "SDK 8.x" → Windows x64 installer).

Verify the installation:

```powershell
dotnet --version
```

You should see a version starting with `8.`.

**2. Install Git** (skip if already installed)

```powershell
winget install Git.Git
```

Verify:

```powershell
git --version
```

**3. Clone the repository**

```powershell
git clone https://github.com/CyberBrainiac1/FFBWheelCustomFirmware.git
cd FFBWheelCustomFirmware
```

**4. Restore dependencies**

```powershell
dotnet restore FFBWheelConfig/FFBWheelConfig.csproj
```

**5. Build**

```powershell
dotnet build FFBWheelConfig/FFBWheelConfig.csproj --configuration Release
```

**6. Run**

```powershell
dotnet run --project FFBWheelConfig/FFBWheelConfig.csproj --configuration Release
```

Or run the compiled executable directly:

```powershell
.\FFBWheelConfig\bin\Release\net8.0-windows\FFBWheelConfig.exe
```

---

## Quick-start (after installation)

1. Connect your Arduino Leonardo to the PC via USB.
2. Launch FFBWheelConfig.
3. Select the correct COM port from the dropdown (click **↻** to refresh the list).
4. Click **Connect**. The status bar at the bottom will show "Connected" on success. If it fails, check that the correct COM port is selected and that no other application is using it.
5. Click **Read from Wheel** to load the current settings.
6. Adjust settings and click **Apply** (applies immediately but lost on power cycle) or **Save to Wheel** (persisted to EEPROM and survives power cycles).

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

```powershell
dotnet restore FFBWheelConfig/FFBWheelConfig.csproj
dotnet build   FFBWheelConfig/FFBWheelConfig.csproj --configuration Release
```

Requires .NET 8 SDK (see [Installation → Option B](#option-b--build-from-source) above).
`EnableWindowsTargeting=true` is already set in the `.csproj`, so the build works on any OS with the SDK installed.
