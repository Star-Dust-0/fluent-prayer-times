$ErrorActionPreference='Stop'
gh release download v1.3.6 --repo "$env:GITHUB_REPOSITORY" --pattern '*installer-win-x64.exe' --dir old
$old=Get-ChildItem old/*.exe | Select-Object -First 1
$p=Start-Process $old.FullName -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART' -PassThru -Wait
if ($p.ExitCode -ne 0) { throw 'Old install failed' }
$legacy="$env:LOCALAPPDATA/Programs/FluentPrayerTimes-SelfContained"
if (-not (Test-Path "$legacy/FluentPrayerTimes.exe")) { throw 'Legacy installation missing' }
$settings="$env:APPDATA/FluentPrayerTimes/settings.json"
'{"Lang":"en","Theme":"dark","AutoUpdate":false,"LocationPending":false,"Location":{"Name":"Saved Cairo","NameAr":"Saved Cairo","Lat":30.0444,"Lon":31.2357,"Cc":"EG","UseCity":false}}' | Set-Content $settings
$running=Start-Process "$legacy/FluentPrayerTimes.exe" -PassThru
Start-Sleep 5
$installer="$PWD/FluentPrayerTimes-v1.3.7-installer-win-x64.exe"
$p=Start-Process $installer -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/TASKS=desktopicon','/LOG=proof/upgrade.log' -PassThru -Wait
if ($p.ExitCode -ne 0) { throw "Upgrade failed $($p.ExitCode)" }
$root="$env:ProgramFiles/FluentPrayerTimes"
if (-not (Test-Path "$root/FluentPrayerTimes.exe")) { throw 'Program Files installation missing' }
if (Test-Path "$legacy/FluentPrayerTimes.exe") { throw 'Legacy app remains installed' }
if ((Get-Content $settings -Raw | ConvertFrom-Json).Location.Name -ne 'Saved Cairo') { throw 'Settings lost' }
if (Get-Process FluentPrayerTimes -ErrorAction SilentlyContinue) { throw 'Previous tray instance not closed' }
$p=Start-Process "$root/FluentPrayerTimes.exe" -PassThru
Start-Sleep 10
if ($p.HasExited) { throw 'Installed app did not launch' }
Stop-Process -Id $p.Id -Force
$p=Start-Process $installer -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/TASKS=desktopicon' -PassThru -Wait
if ($p.ExitCode -ne 0) { throw 'Repeated upgrade failed' }
if ((Get-Content $settings -Raw | ConvertFrom-Json).Location.Name -ne 'Saved Cairo') { throw 'Repeated upgrade lost settings' }
'Upgrade from 1.3.6, close tray app, migrate Program Files, retain settings, installed launch, repeated upgrade: PASS' | Set-Content proof/upgrade-result.txt
