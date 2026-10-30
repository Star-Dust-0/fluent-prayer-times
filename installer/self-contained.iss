[Setup]
AppId=FluentPrayerTimes-SelfContained
AppName=Fluent Prayer Times
AppVersion={#AppVersion}
AppPublisher=Mohamed Elnaggar
AppPublisherURL=https://instinct.com
DefaultDirName={autopf}\FluentPrayerTimes
UsePreviousAppDir=no
DefaultGroupName=Fluent Prayer Times
PrivilegesRequired=admin
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
Filename: "{app}\FluentPrayerTimes.exe"; Description: "Launch Fluent Prayer Times"; Flags: nowait postinstall skipifsilent
[Messages]
WelcomeLabel2=This edition includes .NET and Windows App Runtime. No runtime download is required.%n%nbrought to you by Instinct
[Code]
function OpenEvent(dwDesiredAccess: LongWord; bInheritHandle: Boolean; lpName: String): THandle;
  external 'OpenEventW@kernel32.dll stdcall';
function SetEvent(hEvent: THandle): Boolean;
  external 'SetEvent@kernel32.dll stdcall';
function CloseHandle(hObject: THandle): Boolean;
  external 'CloseHandle@kernel32.dll stdcall';
function PrepareToInstall(var NeedsRestart: Boolean): String;
var QuitEvent: THandle; Code: Integer; LegacyUninstaller: String;
begin
  Result := '';
  QuitEvent := OpenEvent($0002, False, 'FluentPrayerTimes.Quit');
  if QuitEvent <> 0 then begin
    SetEvent(QuitEvent);
    CloseHandle(QuitEvent);
    Sleep(2000);
  end;
  LegacyUninstaller := ExpandConstant('{localappdata}\Programs\FluentPrayerTimes-SelfContained\unins000.exe');
  if FileExists(LegacyUninstaller) then begin
    if not Exec(LegacyUninstaller, '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART', '', SW_HIDE, ewWaitUntilTerminated, Code) or (Code <> 0) then
      Result := 'Could not upgrade the previous installation. Please close Fluent Prayer Times and try again.';
  end;
end;
