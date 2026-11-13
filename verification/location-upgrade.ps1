$ErrorActionPreference='Stop'
function RunInstaller($path, $arguments) {
 $info=[System.Diagnostics.ProcessStartInfo]::new($path,$arguments)
 $info.UseShellExecute=$false
 $process=[System.Diagnostics.Process]::Start($info)
 if(-not $process.WaitForExit(120000)){throw 'Installer timed out'}
 $process.Refresh()
 if($process.ExitCode -ne 0){throw "Installer failed: $($process.ExitCode)"}
}
gh release download v1.3.6 --repo "$env:GITHUB_REPOSITORY" --pattern '*installer-win-x64.exe' --dir old
$old=Get-ChildItem old/*.exe | Select-Object -First 1
RunInstaller $old.FullName '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART'
$legacy="$env:LOCALAPPDATA/Programs/FluentPrayerTimes-SelfContained"
if (-not (Test-Path "$legacy/FluentPrayerTimes.exe")) { throw 'Legacy installation missing' }
$settings="$env:APPDATA/FluentPrayerTimes/settings.json"
'{"Lang":"en","Theme":"dark","AutoUpdate":false,"LocationPending":false,"Location":{"Name":"Saved Cairo","NameAr":"Saved Cairo","Lat":30.0444,"Lon":31.2357,"Cc":"EG","UseCity":false}}' | Set-Content $settings
$running=Start-Process "$legacy/FluentPrayerTimes.exe" -PassThru
Start-Sleep 5
$installer="$PWD/FluentPrayerTimes-v$env:APP_VERSION-installer-win-x64.exe"
RunInstaller $installer '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /TASKS=desktopicon /LOG=proof/upgrade.log'
$root="$env:ProgramFiles/FluentPrayerTimes"
if (-not (Test-Path "$root/FluentPrayerTimes.exe")) { throw 'Program Files installation missing' }
if (Test-Path "$legacy/FluentPrayerTimes.exe") { throw 'Legacy app remains installed' }
if ((Get-Content $settings -Raw | ConvertFrom-Json).Location.Name -ne 'Saved Cairo') { throw 'Settings lost' }
if (Get-Process FluentPrayerTimes -ErrorAction SilentlyContinue) { throw 'Previous tray instance not closed' }
$p=Start-Process "$root/FluentPrayerTimes.exe" -PassThru
Start-Sleep 10
if ($p.HasExited) { throw 'Installed app did not launch' }
Stop-Process -Id $p.Id -Force
RunInstaller $installer '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /TASKS=desktopicon'
if ((Get-Content $settings -Raw | ConvertFrom-Json).Location.Name -ne 'Saved Cairo') { throw 'Repeated upgrade lost settings' }
'Upgrade from 1.3.6, close tray app, migrate Program Files, retain settings, installed launch, repeated upgrade: PASS' | Set-Content proof/upgrade-result.txt
