[Setup]
AppId=FluentPrayerTimes-SelfContained
AppName=Fluent Prayer Times
AppVersion=${{ steps.version.outputs.value }}
AppPublisher=Mohamed Elnaggar
AppPublisherURL=https://instinct.com
DefaultDirName={localappdata}\Programs\FluentPrayerTimes-SelfContained
DefaultGroupName=Fluent Prayer Times (Self-contained)
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=out
OutputBaseFilename=FluentPrayerTimes-v${{ steps.version.outputs.value }}-self-contained-installer-win-x64
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
Name: "{autoprograms}\Fluent Prayer Times (Self-contained)"; Filename: "{app}\FluentPrayerTimes.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Fluent Prayer Times (Self-contained)"; Filename: "{app}\FluentPrayerTimes.exe"; WorkingDir: "{app}"
[Run]
Filename: "{app}\FluentPrayerTimes.exe"; Description: "Launch Fluent Prayer Times"; Flags: nowait postinstall skipifsilent
[Messages]
WelcomeLabel2=This edition includes .NET and Windows App Runtime. No runtime download or administrator rights are required.%n%nbrought to you by Instinct
