; FFB Wheel Tester - Inno Setup installer script
; Build with: iscc installer\setup.iss
; Requires Inno Setup 6.x  (https://jrsoftware.org/isdl.php)
;
; Before building the installer, publish the app:
;   dotnet publish desktop-app\FFBWheelConfig.csproj -c Release -r win-x64 --self-contained true -o installer\publish
;
; The installer bundles the published output and the firmware hex.

#define AppName      "FFB Wheel Tester"
#define AppVersion   "1.2.2"
#define AppPublisher "DIY FFB Wheel"
#define AppURL       "https://github.com/CyberBrainiac1/FFBWheelCustomFirmware"
#define AppExeName   "FFBWheelConfig.exe"

[Setup]
AppId={{FFB-WHEEL-TESTER-2024}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\FFBWheelTester
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=.
OutputBaseFilename=FFBWheelTester-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Published app binaries
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Firmware hex (bundled next to the exe so FirmwareFlasher.cs can find it)
Source: "..\versions\{#AppVersion}\firmware\leonardo-wheel.ino.hex"; \
    DestDir: "{app}\firmware"; \
    DestName: "leonardo-wheel.ino.hex"; \
    Flags: ignoreversion; \
    Check: HexExists

; Flash helper scripts
Source: "..\scripts\flash_hex.bat"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\scripts\flash_hex.ps1"; DestDir: "{app}"; Flags: ignoreversion

; README
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";    Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}";   Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; \
    Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; \
    Flags: nowait postinstall skipifsilent

[Code]
function HexExists(): Boolean;
begin
  Result := FileExists(ExpandConstant('{src}\..\versions\{#AppVersion}\firmware\leonardo-wheel.ino.hex'));
end;
