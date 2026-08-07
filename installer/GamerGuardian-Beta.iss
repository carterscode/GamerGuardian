; GamerGuardian BETA Inno Setup script
;
; Build with: ISCC.exe /DAppVersion=1.2.3 installer\GamerGuardian-Beta.iss
; Expects the BETA payload in ..\publish-beta (publish with -p:Beta=true).
;
; Every identity this script declares is deliberately distinct from
; GamerGuardian.iss so a beta install sits alongside a stable one instead of
; upgrading over it: AppId, AppName, install directory, Start Menu group,
; uninstall entry, and output filename. The uninstall cleanup targets the beta
; config root and the beta Run value, which AppIdentity.cs defines under the
; BETA compile constant -- these strings must stay in step with that file.

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

#define AppName       "GamerGuardian Beta"
#define AppPublisher  "GamerGuardian Contributors"
#define AppURL        "https://github.com/GamerGuardian/GamerGuardian"
#define AppExeName    "GamerGuardian.exe"
#define PublishDir    "..\publish-beta"

; Must match AppIdentity.ProductFolderName and AppIdentity.StartupRegistryValueName
; under BETA.
#define BetaConfigDir "GamerGuardian-Beta"
#define BetaRunValue  "GamerGuardian-Beta"

[Setup]
; Distinct from the stable AppId (B6C2D7E1-...). Sharing it would make this
; installer upgrade over -- and then uninstall -- the stable install.
AppId={{9EC25C38-17D8-4AEC-B25B-A914B8C90C98}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
DefaultDirName={userpf}\GamerGuardian Beta
DefaultGroupName=GamerGuardian Beta
DisableProgramGroupPage=yes
DisableDirPage=auto
; Per-user, same as stable -- no elevation to install.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=Output
OutputBaseFilename=GamerGuardian-Beta-Setup-{#AppVersion}
SetupIconFile=..\src\GamerGuardian\Assets\AppIcon.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#AppName}
; Restart Manager works off the files being written under {app}, so this only
; targets a running beta -- it will not close a stable install.
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
; Both flavors ship an executable named GamerGuardian.exe, so stopping by name
; alone would kill a running STABLE install while uninstalling the beta. Filter
; on the image path so only the beta process is stopped.
Filename: "powershell.exe"; Parameters: "-NoProfile -Command ""Get-Process -Name GamerGuardian -ErrorAction SilentlyContinue | Where-Object {{ $_.Path -like '{app}\*' } | Stop-Process -Force"""; Flags: runhidden; RunOnceId: "StopGamerGuardianBeta"

[UninstallDelete]
; The beta's own config root only. The stable root (%APPDATA%\GamerGuardian) is
; never touched -- a beta uninstall must not take the user's real settings with it.
Type: filesandordirs; Name: "{userappdata}\{#BetaConfigDir}"

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  RootKey: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    RootKey := HKEY_CURRENT_USER;
    { Beta's own Run value only; the stable 'GamerGuardian' value is left alone. }
    RegDeleteValue(RootKey, 'Software\Microsoft\Windows\CurrentVersion\Run', '{#BetaRunValue}');
  end;
end;
