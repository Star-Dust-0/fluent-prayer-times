[Setup]
AppId=FluentPrayerTimes
AppName=Fluent Prayer Times
UninstallDisplayName=Fluent Prayer Times
AppVersion={#AppVersion}
AppPublisher=Mohamed Elnaggar
AppPublisherURL=https://instinct.com
DefaultDirName={autopf}\FluentPrayerTimes
UsePreviousAppDir=no
DefaultGroupName=Fluent Prayer Times
DisableProgramGroupPage=yes
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
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"
[Icons]
Name: "{autoprograms}\Fluent Prayer Times"; Filename: "{app}\FluentPrayerTimes.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Fluent Prayer Times"; Filename: "{app}\FluentPrayerTimes.exe"; WorkingDir: "{app}"; Tasks: desktopicon
[Run]
Filename: "{app}\FluentPrayerTimes.exe"; Description: "Launch Fluent Prayer Times"; Flags: nowait postinstall skipifsilent
[Messages]
WelcomeLabel2=This edition includes .NET and Windows App Runtime. Administrator rights are required to install in Program Files.%n%nbrought to you by Instinct
[Code]
function OpenEvent(dwDesiredAccess: LongWord; bInheritHandle: Boolean; lpName: String): THandle;
  external 'OpenEventW@kernel32.dll stdcall';
function SetEvent(hEvent: THandle): Boolean;
  external 'SetEvent@kernel32.dll stdcall';
function CloseHandle(hObject: THandle): Boolean;
  external 'CloseHandle@kernel32.dll stdcall';
function PrepareToInstall(var NeedsRestart: Boolean): String;
var QuitEvent: THandle; Code: Integer; OldDir, OldUninstall: String;
begin
  Result := '';
  QuitEvent := OpenEvent($0002, False, 'FluentPrayerTimes.Quit');
  if QuitEvent <> 0 then begin
    SetEvent(QuitEvent);
    CloseHandle(QuitEvent);
    Sleep(2000);
  end;
  OldDir := ExpandConstant('{localappdata}\Programs\FluentPrayerTimes-SelfContained');
  OldUninstall := OldDir + '\unins000.exe';
  if FileExists(OldUninstall) then begin
    if not Exec(OldUninstall, '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART', '', SW_HIDE, ewWaitUntilTerminated, Code) or (Code <> 0) then begin
      Result := 'Could not remove the previous installation. Close Fluent Prayer Times and try again.';
      exit;
    end;
  end;
  if DirExists(OldDir) then begin
    if not DelTree(OldDir, True, True, True) then begin
      Result := 'Could not clean the previous installation folder. Close Fluent Prayer Times and try again.';
      exit;
    end;
  end;
  DeleteFile(ExpandConstant('{userdesktop}\Fluent Prayer Times (Self-contained).lnk'));
  DeleteFile(ExpandConstant('{userprograms}\Fluent Prayer Times (Self-contained).lnk'));
  RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\FluentPrayerTimes-SelfContained_is1');
end;
