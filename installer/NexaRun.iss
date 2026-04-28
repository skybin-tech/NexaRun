; NexaRun Inno Setup Script
; Build with: build.ps1 or iscc.exe /DAppVersion=1.0.0 /DPubDir=.\publish /DOutDir=.\output NexaRun.iss

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef PubDir
  #define PubDir ".\publish"
#endif
#ifndef OutDir
  #define OutDir ".\output"
#endif

#define AppName      "NexaRun"
#define AppPublisher "Skybin Technology Private Limited"
#define AppCopyright "Copyright (C) Skybin Technology Private Limited"
#define AppURL       "https://github.com/skybin-tech/NexaRun"
#define ServiceName  "NexaRunDaemon"
#define ServiceExe   "NexaRun.Daemon.exe"
#define TrayExe      "NexaRun.exe"
#define CliExe       "NexaRun.Cli.exe"

[Setup]
AppId={{E2A4F1B3-7C5D-4E8A-9F2B-1D3C6A8E0F47}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppCopyright={#AppCopyright}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir={#OutDir}
OutputBaseFilename=NexaRun-{#AppVersion}-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#TrayExe}
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startwitwindows"; Description: "Start NexaRun Tray at Windows login"; GroupDescription: "Startup:"; Flags: unchecked
Name: "desktopicon";     Description: "Create a &desktop shortcut for NexaRun Tray"; GroupDescription: "Additional icons:"

[Files]
; Daemon (Windows Service)
Source: "{#PubDir}\daemon\{#ServiceExe}"; DestDir: "{app}"; Flags: ignoreversion

; CLI — also copied to bin\ as "nexarun.exe" so it works from any terminal
Source: "{#PubDir}\cli\{#CliExe}"; DestDir: "{app}";     Flags: ignoreversion
Source: "{#PubDir}\cli\{#CliExe}"; DestDir: "{app}\bin"; Flags: ignoreversion; DestName: "nexarun.exe"

; Tray app
Source: "{#PubDir}\tray\{#TrayExe}";      DestDir: "{app}";      Flags: ignoreversion

[Icons]
; Start Menu
Name: "{group}\NexaRun Tray";        Filename: "{app}\{#TrayExe}";   Comment: "NexaRun system tray app"
Name: "{group}\Uninstall NexaRun";   Filename: "{uninstallexe}"

; Desktop shortcut (optional task)
Name: "{autodesktop}\NexaRun";       Filename: "{app}\{#TrayExe}";   Tasks: desktopicon

; Startup (optional task) — run tray at login
Name: "{userstartup}\NexaRun Tray";  Filename: "{app}\{#TrayExe}";   Tasks: startwitwindows

[Registry]
; Add {app}\bin to system PATH so "nexarun" works in any terminal
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"; \
  ValueType: expandsz; ValueName: "Path"; \
  ValueData: "{olddata};{app}\bin"; \
  Check: NeedsAddPath(ExpandConstant('{app}\bin'))

[Run]
; Install and start the Windows Service silently after install
Filename: "sc.exe"; Parameters: "create ""{#ServiceName}"" binPath= ""{app}\{#ServiceExe}"" start= auto DisplayName= ""NexaRun Daemon"""; \
  Flags: runhidden waituntilterminated; StatusMsg: "Installing NexaRun Daemon service..."
Filename: "sc.exe"; Parameters: "description ""{#ServiceName}"" ""NexaRun background process manager daemon"""; \
  Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "start ""{#ServiceName}"""; \
  Flags: runhidden waituntilterminated; StatusMsg: "Starting NexaRun Daemon..."

; Launch tray app after install (user-visible)
Filename: "{app}\{#TrayExe}"; \
  Description: "Launch NexaRun Tray"; \
  Flags: nowait postinstall skipifsilent

[UninstallRun]
; Stop and remove the Windows Service on uninstall
Filename: "sc.exe"; Parameters: "stop ""{#ServiceName}""";   Flags: runhidden waituntilterminated
Filename: "sc.exe"; Parameters: "delete ""{#ServiceName}"""; Flags: runhidden waituntilterminated

[UninstallDelete]
; Leave ~/.nexarun (user data) — only remove install dir
Type: filesandordirs; Name: "{app}"

[Code]
// Checks whether a given path is already in the system PATH variable.
function NeedsAddPath(Param: string): boolean;
var
  OrigPath: string;
begin
  if not RegQueryStringValue(
    HKEY_LOCAL_MACHINE,
    'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
    'Path', OrigPath)
  then begin
    Result := True;
    exit;
  end;
  Result := Pos(';' + Param + ';', ';' + OrigPath + ';') = 0;
end;

// Remove our bin dir from PATH on uninstall.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  OrigPath, BinPath, NewPath: string;
  P: Integer;
begin
  if CurUninstallStep <> usPostUninstall then exit;

  BinPath := ExpandConstant('{app}\bin');
  if not RegQueryStringValue(
    HKEY_LOCAL_MACHINE,
    'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
    'Path', OrigPath)
  then exit;

  NewPath := OrigPath;
  P := Pos(';' + BinPath, NewPath);
  if P > 0 then
    Delete(NewPath, P, Length(';' + BinPath));

  if NewPath <> OrigPath then
    RegWriteExpandStringValue(
      HKEY_LOCAL_MACHINE,
      'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
      'Path', NewPath);
end;
