[Setup]
AppId=FluentPrayerTimes-SelfContained
AppName=Fluent Prayer Times
AppVersion={#AppVersion}
AppPublisher=Mohamed Elnaggar
AppPublisherURL=https://instinct.com
DefaultDirName={localappdata}\Programs\FluentPrayerTimes-SelfContained
DefaultGroupName=Fluent Prayer Times
PrivilegesRequired=lowest
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
[Icons]
Name: "{autoprograms}\Fluent Prayer Times"; Filename: "{app}\FluentPrayerTimes.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Fluent Prayer Times"; Filename: "{app}\FluentPrayerTimes.exe"; WorkingDir: "{app}"
[Run]
Filename: "{app}\FluentPrayerTimes.exe"; Description: "Launch Fluent Prayer Times"; Flags: nowait postinstall skipifsilent
[Messages]
WelcomeLabel2=This edition includes .NET and Windows App Runtime. No runtime download or administrator rights are required.%n%nbrought to you by Instinct
[Code]
function OpenEvent(dwDesiredAccess: LongWord; bInheritHandle: Boolean; lpName: String): THandle;
  external 'OpenEventW@kernel32.dll stdcall';
function SetEvent(hEvent: THandle): Boolean;
  external 'SetEvent@kernel32.dll stdcall';
function CloseHandle(hObject: THandle): Boolean;
  external 'CloseHandle@kernel32.dll stdcall';
function PrepareToInstall(var NeedsRestart: Boolean): String;
var QuitEvent: THandle; Code: Integer;
begin
  Result := '';
  QuitEvent := OpenEvent($0002, False, 'FluentPrayerTimes.Quit');
  if QuitEvent <> 0 then begin
    SetEvent(QuitEvent);
    CloseHandle(QuitEvent);
    Sleep(2000);
  end;
end;
