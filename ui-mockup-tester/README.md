# EMC FFB Tester – UI Mockup

A **self-contained, single-file HTML preview** of the full EMC FFB Tester desktop app UI.

## How to open

Double-click **`index.html`** in any modern browser — no server or build step required.

## What it shows

The mockup faithfully reproduces the full **860 × 620 px** app layout with all 5 sections:

| Section | Contents |
|---------|----------|
| **Top device bar** | Device dropdown, Refresh / Connect / Disconnect, selected device, connection status, profile dropdown, Load / Save Profile |
| **Live wheel state** | Big live angle display (42 pt), force mode label, live state lines (strength / range / connected), raw debug values, horizontal wheel position bar |
| **Manual force control** | Turn Left / Turn Right / Center / Stop Force / Start Test / Stop Test buttons, force strength slider + numeric, stop-on-key-release checkbox |
| **Wheel settings** | Two-column slider rows (Overall / Spring / Damper / Friction / Constant Force / Steering Range / Center Offset), Invert FFB / Invert Axis checkboxes, Read Current Settings / Apply Settings / Reset Defaults |
| **Bottom bar** | Keyboard shortcut help, status line, Emergency Stop button |

## Interactive features

- **Connect / Disconnect** — toggles device state, enables/disables controls  
- **Turn Left / Turn Right** — hold to push wheel angle indicator left or right  
- **Center** — animates wheel back to 0°  
- **Stop Force / Emergency Stop** — resets everything instantly  
- **Start / Stop Test Mode** — toggles test mode state and status display  
- **Force Strength slider ↔ numeric** — always in sync  
- **All settings sliders ↔ numeric inputs** — always in sync  
- **Load Profile** — populates settings panel from a named preset  
- **Save Profile** — confirms local profile save  
- **Read Current Settings** — simulates reading from device  
- **Apply Settings** — simulates sending settings  
- **Reset Defaults** — resets all settings to safe defaults  
- **Keyboard shortcuts** (when no input is focused):  
  - `←` / `→` — left / right force  
  - `Space` — stop force  
  - `C` — center  
  - `↑` / `↓` — increase / decrease strength by 5%

## Notes

- This is a **visual mockup only** — it does not communicate with any real device  
- Colours, fonts, layout, and control positions match the C# WinForms `wheel-ffb-tester` app spec exactly  
- The mockup is intentionally a **single file** (`index.html`) for easy sharing and preview  
