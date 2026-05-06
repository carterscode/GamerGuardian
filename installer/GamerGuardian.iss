; GamerGuardian Inno Setup script
; Build with: ISCC.exe installer\GamerGuardian.iss
; Override version at the command line: ISCC.exe /DAppVersion=1.2.3 installer\GamerGuardian.iss

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

#define AppName       "GamerGuardian"
#define AppPublisher  "GamerGuardian Contributors"
#define AppURL        "https://github.com/GamerGuardian/GamerGuardian"
#define AppExeName    "GamerGuardian.exe"
#define PublishDir    "..\publish"

[Setup]
AppId={{B6C2D7E1-9F1B-4F32-9A8C-3D6F0A7E6B11}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
DefaultDirName={userpf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableDirPage=auto
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=Output
OutputBaseFilename=GamerGuardian-Setup-{#AppVersion}
SetupIconFile=..\src\GamerGuardian\Assets\AppIcon.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#AppName}
CloseApplications=yes
RestartApplications=no
ShowLanguageDialog=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-NoProfile -Command ""Get-Process -Name GamerGuardian -ErrorAction SilentlyContinue | Stop-Process -Force"""; Flags: runhidden; RunOnceId: "StopGamerGuardian"

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\GamerGuardian"

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  RootKey: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    RootKey := HKEY_CURRENT_USER;
    RegDeleteValue(RootKey, 'Software\Microsoft\Windows\CurrentVersion\Run', 'GamerGuardian');
  end;
end;
