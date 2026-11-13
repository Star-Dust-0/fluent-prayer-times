[Setup]
AppId=FluentPrayerTimes-SelfContained
AppName=Fluent Prayer Times
AppVersion={#AppVersion}
AppPublisher=Mohamed Elnaggar
AppPublisherURL=https://github.com/Star-Dust-0/fluent-prayer-times
DefaultDirName={autopf}\FluentPrayerTimes
UsePreviousAppDir=no
DefaultGroupName=Fluent Prayer Times
PrivilegesRequired=admin
MinVersion=10.0.17763
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=out
OutputBaseFilename=FluentPrayerTimes-v{#AppVersion}-installer-win-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=publish\app.ico
CloseApplications=yes
RestartApplications=no
UninstallDisplayIcon={app}\FluentPrayerTimes.exe
[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: checkedonce
[Icons]
Name: "{autoprograms}\Fluent Prayer Times"; Filename: "{app}\FluentPrayerTimes.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Fluent Prayer Times"; Filename: "{app}\FluentPrayerTimes.exe"; WorkingDir: "{app}"; Tasks: desktopicon
[Run]
Filename: "{app}\FluentPrayerTimes.exe"; Description: "Launch Fluent Prayer Times"; Flags: nowait postinstall skipifsilent runasoriginaluser
[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "FluentPrayerTimes"; Flags: uninsdeletevalue
[Messages]
WelcomeLabel2=This edition includes .NET and Windows App Runtime. No runtime download is required.%n%nbrought to you by Instinct
[Code]
function OpenEvent(dwDesiredAccess: LongWord; bInheritHandle: Boolean; lpName: String): THandle;
  external 'OpenEventW@kernel32.dll stdcall';
function SetEvent(hEvent: THandle): Boolean;
  external 'SetEvent@kernel32.dll stdcall';
function CloseHandle(hObject: THandle): Boolean;
  external 'CloseHandle@kernel32.dll stdcall';
function OpenMutex(dwDesiredAccess: LongWord; bInheritHandle: Boolean; lpName: String): THandle;
  external 'OpenMutexW@kernel32.dll stdcall';
function WaitForSingleObject(hHandle: THandle; dwMilliseconds: LongWord): LongWord;
  external 'WaitForSingleObject@kernel32.dll stdcall';
function ReleaseMutex(hMutex: THandle): Boolean;
  external 'ReleaseMutex@kernel32.dll stdcall';
function PrepareToInstall(var NeedsRestart: Boolean): String;
var QuitEvent, AppMutex: THandle; WaitResult: LongWord; Code: Integer; LegacyUninstaller: String;
begin
  Result := '';
  QuitEvent := OpenEvent($0002, False, 'FluentPrayerTimes.Quit');
  if QuitEvent <> 0 then begin
    SetEvent(QuitEvent);
    CloseHandle(QuitEvent);
    AppMutex := OpenMutex($00100001, False, 'FluentPrayerTimes.SingleInstance');
    if AppMutex <> 0 then begin
      WaitResult := WaitForSingleObject(AppMutex, 15000);
      if (WaitResult = 0) or (WaitResult = $80) then ReleaseMutex(AppMutex);
      CloseHandle(AppMutex);
      if (WaitResult <> 0) and (WaitResult <> $80) then begin
        Result := 'Please close Fluent Prayer Times and try again.';
        exit;
      end;
    end;
  end;
  LegacyUninstaller := ExpandConstant('{localappdata}\Programs\FluentPrayerTimes-SelfContained\unins000.exe');
  if FileExists(LegacyUninstaller) then begin
    if not Exec(LegacyUninstaller, '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART', '', SW_HIDE, ewWaitUntilTerminated, Code) or (Code <> 0) then
      Result := 'Could not upgrade the previous installation. Please close Fluent Prayer Times and try again.';
  end;
end;
