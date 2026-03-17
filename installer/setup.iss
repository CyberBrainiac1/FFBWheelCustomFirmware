; EMC FFB Tester - Inno Setup installer script
; Build with: iscc installer\setup.iss
; Requires Inno Setup 6.x  (https://jrsoftware.org/isdl.php)
;
; Before building the installer, publish the tester app:
;   dotnet publish wheel-ffb-tester\FFBWheelTester.csproj -c Release -r win-x64 --self-contained true -o installer\publish
;
; This app is a runtime force-feedback test utility only.
; It does NOT flash firmware. The wheel must already be flashed before use.

#define AppName      "EMC FFB Tester"
#define AppVersion   "1.0.0"
#define AppPublisher "DIY FFB Wheel"
#define AppURL       "https://github.com/CyberBrainiac1/FFBWheelCustomFirmware"
#define AppExeName   "FFBWheelTester.exe"

[Setup]
AppId={{FFB-WHEEL-TESTER-2024}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\EMCFFBTester
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=.
OutputBaseFilename=EMCFFBTester-Setup
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
