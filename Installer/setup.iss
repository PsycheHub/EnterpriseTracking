; ────────────────────────────────────────────────────────────────────────────
; Enterprise Tracking Activity Agent — Inno Setup installer script
; Requires Inno Setup 6.x  (https://jrsoftware.org/isinfo.php)
; ────────────────────────────────────────────────────────────────────────────

#define AppName      "Enterprise Tracking Activity Agent"
#define AppPublisher "EnterpriseTracking"
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#define AppExeName   "EnterpriseTrackingActivityAgent.exe"
#define AppId        "{{E4A2B3C1-9F3D-4E2A-B1C5-7A8D9E0F1234}"
#define PublishDir   "publish"
#define IconFile     "..\EnterpriseTrackingActivityAgent\Resources\apps.ico"

; ── Setup metadata ──────────────────────────────────────────────────────────
[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://enterprisetracking.onrender.com
AppSupportURL=https://enterprisetracking.onrender.com
AppUpdatesURL=https://enterprisetracking.onrender.com

; Install to Program Files\EnterpriseTracking\ActivityAgent
DefaultDirName={autopf}\{#AppPublisher}\ActivityAgent
DefaultGroupName={#AppPublisher}
DisableProgramGroupPage=yes

; Output
OutputDir=output
OutputBaseFilename=EnterpriseTrackingActivityAgent-Setup-{#AppVersion}

; Icon and visual style
SetupIconFile={#IconFile}
WizardStyle=modern
WizardSizePercent=120
DisableWelcomePage=no
LicenseFile=

; Architecture — x64 only
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible

; Compression
Compression=lzma2/ultra64
SolidCompression=yes
InternalCompressLevel=ultra64

; Privileges — install to Program Files requires admin
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

; Do not use %TEMP% for setup loader (avoids ASR/policy blocks on temp execution)
UseSetupLdr=no

; Uninstaller
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
CreateUninstallRegKey=yes

; Prevent downgrade
VersionInfoVersion={#AppVersion}
VersionInfoProductName={#AppName}
VersionInfoCompany={#AppPublisher}

; ── Languages ────────────────────────────────────────────────────────────────
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

; ── Tasks (optional user selections) ────────────────────────────────────────
[Tasks]
Name: "desktopicon";    Description: "Create a &desktop shortcut";          GroupDescription: "Additional icons:";  Flags: unchecked
Name: "autostart";      Description: "Start automatically when &Windows starts (all users)"; GroupDescription: "Startup:"; Flags: unchecked

; ── Files ───────────────────────────────────────────────────────────────────
[Files]
; Main executable (self-contained — no .NET runtime needed on target machine)
Source: "{#PublishDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; ── Icons (shortcuts) ───────────────────────────────────────────────────────
[Icons]
; Start Menu
Name: "{group}\{#AppName}";           Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"

; Desktop shortcut (optional)
Name: "{autodesktop}\{#AppName}";     Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"; Tasks: desktopicon

; ── Registry ────────────────────────────────────────────────────────────────
[Registry]
; Auto-start for all users (HKLM Run key) — optional task
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "{#AppName}"; \
  ValueData: """{app}\{#AppExeName}"""; \
  Flags: uninsdeletevalue; Tasks: autostart

; Register application in Apps & Features (Add/Remove Programs)
Root: HKLM; Subkey: "Software\{#AppPublisher}\{#AppName}"; \
  ValueType: string; ValueName: "InstallDir"; \
  ValueData: "{app}"; Flags: uninsdeletekey

; ── Run after install ────────────────────────────────────────────────────────
[Run]
Filename: "{app}\{#AppExeName}"; \
  Description: "Launch {#AppName} now"; \
  Flags: nowait postinstall skipifsilent shellexec; \
  WorkingDir: "{app}"

; ── Uninstall: kill running instance first ───────────────────────────────────
[UninstallRun]
Filename: "taskkill.exe"; \
  Parameters: "/f /im {#AppExeName}"; \
  Flags: runhidden waituntilterminated; \
  RunOnceId: "KillAgent"

; ── Code ────────────────────────────────────────────────────────────────────
[Code]
// Stop any running instance before upgrading/installing
procedure StopRunningInstance();
var
  ResultCode: Integer;
begin
  Exec('taskkill.exe', '/f /im {#AppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

function InitializeSetup(): Boolean;
begin
  StopRunningInstance();
  Result := True;
end;

// Remove user-data AppData folder on full uninstall (ask first)
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
  Ans: Integer;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{localappdata}\EnterpriseTrackingAgent');
    if DirExists(DataDir) then
    begin
      Ans := MsgBox(
        'Do you want to remove locally stored session data?' + #13#10 +
        '(' + DataDir + ')',
        mbConfirmation, MB_YESNO);
      if Ans = IDYES then
        DelTree(DataDir, True, True, True);
    end;
  end;
end;
