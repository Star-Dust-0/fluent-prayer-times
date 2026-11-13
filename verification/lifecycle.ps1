$ErrorActionPreference='Stop'
New-Item proof/lifecycle -ItemType Directory -Force | Out-Null
$exe=(Resolve-Path "$PWD/publish/FluentPrayerTimes.exe").Path
$directory="$env:APPDATA/FluentPrayerTimes"
New-Item $directory -ItemType Directory -Force | Out-Null
function SaveSettings($startup) {
 @{ Lang='en'; Theme='dark'; AutoUpdate=$false; LocationPending=$true; RunAtStartup=$startup } | ConvertTo-Json | Set-Content "$directory/settings.json"
}
function StartApp($arguments='--tray') {
 $info=[System.Diagnostics.ProcessStartInfo]::new($exe,$arguments)
 $info.UseShellExecute=$false
 $process=[System.Diagnostics.Process]::Start($info)
 Start-Sleep 3
 if ($process.HasExited) { throw "Launch exited: $($process.ExitCode)" }
 return $process
}
function StopApp {
 try { $quit=[System.Threading.EventWaitHandle]::OpenExisting('FluentPrayerTimes.Quit');$quit.Set()|Out-Null;$quit.Dispose() } catch { }
 for($i=0;$i -lt 10 -and (Get-Process FluentPrayerTimes -ErrorAction SilentlyContinue);$i++){Start-Sleep 1}
 if(Get-Process FluentPrayerTimes -ErrorAction SilentlyContinue){throw 'Graceful exit did not terminate the app'}
}
try {
 Get-Process FluentPrayerTimes -ErrorAction SilentlyContinue | Stop-Process -Force
 SaveSettings $true
 New-Item 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Force | Out-Null
 Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name FluentPrayerTimes -Value '"C:\old-install\FluentPrayerTimes.exe" --tray'
 $first=StartApp
 $actual=(Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run').FluentPrayerTimes
 if($actual -ne "`"$exe`" --tray"){throw "Startup path not repaired"}
 'PASS startup path repaired' | Add-Content proof/lifecycle/results.txt
 $secondInfo=[System.Diagnostics.ProcessStartInfo]::new($exe)
 $secondInfo.UseShellExecute=$false
 $second=[System.Diagnostics.Process]::Start($secondInfo)
 if(-not $second.WaitForExit(10000)){throw 'Second launch did not redirect and exit'}
 if(@(Get-Process FluentPrayerTimes).Count -ne 1 -or $first.HasExited){throw 'Single instance failed'}
 'PASS second launch keeps original instance alive' | Add-Content proof/lifecycle/results.txt
 $restartInfo=[System.Diagnostics.ProcessStartInfo]::new($exe,'--restarted --tray')
 $restartInfo.UseShellExecute=$false
 $restart=[System.Diagnostics.Process]::Start($restartInfo)
 Start-Sleep 3
 if($restart.HasExited){throw 'Restart did not wait for old mutex'}
 $quit=[System.Threading.EventWaitHandle]::OpenExisting('FluentPrayerTimes.Quit');$quit.Set()|Out-Null;$quit.Dispose()
 if(-not $first.WaitForExit(10000)){throw 'Old instance did not exit'}
 Start-Sleep 4
 if($restart.HasExited -or @(Get-Process FluentPrayerTimes).Count -ne 1){throw 'Restart replacement failed'}
 'PASS delayed restart waits for old instance' | Add-Content proof/lifecycle/results.txt
 StopApp
 'PASS graceful shutdown' | Add-Content proof/lifecycle/results.txt
 # Untrusted command-line hooks must do nothing without the explicit test environment.
 $saved=$env:FPT_E2E
 $env:FPT_E2E=''
 SaveSettings $false
 $sentinel="$env:TEMP/fpt-untrusted-hook.log"
 Remove-Item $sentinel -ErrorAction SilentlyContinue
 $normal=StartApp "--tray --preview=adhan --e2e-set=AdhanNotify=false --e2e-log=$sentinel"
 if(Test-Path $sentinel){throw 'Production launch honored test log hook'}
 if((Get-Content "$directory/settings.json" -Raw|ConvertFrom-Json).AdhanNotify -eq $false){throw 'Production launch honored test settings hook'}
 StopApp
 $env:FPT_E2E=$saved
 'PASS normal launch ignores test hooks' | Add-Content proof/lifecycle/results.txt
} finally {
 Get-Process FluentPrayerTimes -ErrorAction SilentlyContinue | Stop-Process -Force
 Remove-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name FluentPrayerTimes -ErrorAction SilentlyContinue
}
