# EMC FFB Tester – UI Mockup

A **self-contained, single-file HTML preview** of the full EMC FFB Tester desktop app UI.

## Viewing on a Chromebook (or any browser)

### Option A — GitHub Pages live link *(easiest)*

Once the workflow runs, the mockup is served at:

**https://cyberbrainiac1.github.io/FFBWheelCustomFirmware/**

Open that URL in Chrome on any Chromebook — no downloads, no server, no install needed.

> **Note:** GitHub Pages must be enabled in the repo Settings → Pages → Source → GitHub Actions.  
> After the first push, the workflow auto-deploys every time `ui-mockup-tester/` changes.

### Option B — Download and open locally

1. Open the file on GitHub and click **Raw**, then **Save page as…** (or right-click the Raw button → Save link as…).
2. Save it as `index.html` anywhere on your Chromebook.
3. Open Chrome → `Ctrl+O` → select the saved file.

No internet connection required once downloaded.

---

## How to open locally

Double-click **`index.html`** in any modern browser — no server or build step required.

---

## EMC Lite firmware compatibility

This mockup is designed to match **EMC Lite 0.9.32** (`EMCLite0932.hex`), the default EMC Lite
firmware in this repository.

### Transport

EMC Lite communicates via **USB HID** (raw HID packets), not serial/COM port.
The mockup reflects this — the device bar shows HID FFB devices, and the settings panel
uses EMC Lite's actual parameter names.

### Settings mapping

| Mockup label | EMC Lite command | Range |
|---|---|---|
| Spring | `SetSpring` | 0–100 % |
| Damper | `SetDamper` | 0–100 % |
| Friction | `SetFriction` | 0–100 % |
| Inertia | `SetInertia` | 0–100 % |
| Soft Lock | `SetSoftlock` | 0–900 ° |
| Enc. CPR | `SetCpr` | 600–4096 |
| Inv. Motor | H-bridge direction (`HbridgeMode`) | on/off |
| Inv. Encoder | encoder inversion | on/off |

Centering uses `DoSetCenter` (the **Center** / **Recenter** button), not a slider.

### Default values (Reset Defaults)

| Setting | Default |
|---|---|
| Spring | 15 % |
| Damper | 10 % |
| Friction | 4 % |
| Inertia | 0 % |
| Soft Lock | 900 ° |
| Enc. CPR | 1024 |
| Inv. Motor | ☐ |
| Inv. Encoder | ☐ |

---

## What the mockup shows

The mockup faithfully reproduces the full **860 × 620 px** app layout with all 5 sections:

| Section | Contents |
|---------|----------|
| **Top device bar** | Device dropdown, Refresh / Connect / Disconnect, selected device, connection status, profile dropdown, Load / Save Profile |
| **Live wheel state** | Big live angle display (42 pt), force mode label, live state lines (spring / soft lock / connected), raw HID debug values, horizontal wheel position bar |
| **Manual force control** | Turn Left / Turn Right / Center / Stop Force / Start Test / Stop Test buttons, force strength slider + numeric, stop-on-key-release checkbox |
| **Wheel settings** | Two-column slider rows matching EMC Lite params (Spring / Damper / Friction / Inertia / Soft Lock / Enc. CPR), Inv. Motor / Inv. Encoder checkboxes, Read Current Settings / Apply Settings / Reset Defaults |
| **Bottom bar** | Keyboard shortcut help, status line, Emergency Stop button |

## Interactive features

- **Connect / Disconnect** — toggles device state, enables/disables controls; shows firmware version on connect  
- **Turn Left / Turn Right** — hold to push wheel angle indicator left or right  
- **Center** — animates wheel back to 0°  
- **Stop Force / Emergency Stop** — resets everything instantly  
- **Start / Stop Test Mode** — toggles test mode state and status display  
- **Force Strength slider ↔ numeric** — always in sync  
- **All settings sliders ↔ numeric inputs** — always in sync  
- **Load Profile** — populates settings panel from a named EMC Lite preset  
- **Save Profile** — confirms local profile save  
- **Read Current Settings** — simulates reading EMC Lite settings via HID  
- **Apply Settings** — simulates sending settings via HID  
- **Reset Defaults** — resets all settings to EMC Lite safe defaults  
- **Keyboard shortcuts** (when no input is focused):  
  - `←` / `→` — left / right force  
  - `Space` — stop force  
  - `C` — center  
  - `↑` / `↓` — increase / decrease strength by 5%

## Notes

- This is a **visual mockup only** — it does not communicate with any real device  
- Settings and defaults are aligned to **EMC Lite firmware** (HID transport, not serial)  
- Colours, fonts, layout, and control positions match the C# WinForms app spec exactly  
- The mockup is intentionally a **single file** (`index.html`) for easy sharing and preview  
