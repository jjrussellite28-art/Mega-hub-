; Mega Hub + installer (built by GitHub Actions with Inno Setup)
#define AppName "Mega Hub +"
#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

[Setup]
AppId={{6C1F2A52-5B1E-4D77-9C0B-0A1B2C3D4E5F}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Mega Hub +
DefaultDirName={localappdata}\Programs\MegaHubPlus
DefaultGroupName={#AppName}
PrivilegesRequired=lowest
OutputDir=..\installer-out
OutputBaseFilename=MegaHubPlus-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\MegaHubPlus.exe
DisableProgramGroupPage=yes

[Files]
Source: "..\out\MegaHubPlus.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\MegaHubPlus.exe"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\MegaHubPlus.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Run]
Filename: "{app}\MegaHubPlus.exe"; Description: "Start {#AppName}"; Flags: nowait postinstall

[UninstallDelete]
; remove everything Mega Hub + saved, so no trace is left
Type: filesandordirs; Name: "{userappdata}\MegaHubPlus"
