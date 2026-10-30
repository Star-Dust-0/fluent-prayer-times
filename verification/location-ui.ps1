$ErrorActionPreference='Stop'
New-Item proof -ItemType Directory -Force | Out-Null
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes,System.Windows.Forms,System.Drawing
$settings="$env:APPDATA/FluentPrayerTimes"
New-Item $settings -ItemType Directory -Force | Out-Null
$exe="$PWD/publish/FluentPrayerTimes.exe"
$p=Start-Process $exe -ArgumentList '--selftest' -PassThru -Wait
Copy-Item "$settings/selftest.log" proof/selftest.log
if ($p.ExitCode -ne 0) { throw 'App selftest failed' }
function Capture($name) {
 $bounds=[System.Windows.Forms.Screen]::PrimaryScreen.Bounds
 $bmp=New-Object System.Drawing.Bitmap $bounds.Width,$bounds.Height
 $g=[System.Drawing.Graphics]::FromImage($bmp)
 $g.CopyFromScreen($bounds.Location,[System.Drawing.Point]::Empty,$bounds.Size)
 $bmp.Save("$PWD/proof/$name.png");$g.Dispose();$bmp.Dispose()
}
function Names($process) {
 $root=[System.Windows.Automation.AutomationElement]::RootElement
 $cond=New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::ProcessIdProperty),$process.Id
 $win=$root.FindFirst([System.Windows.Automation.TreeScope]::Children,$cond)
 if (-not $win) { throw 'App window missing' }
 return @($win.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition) | ForEach-Object { $_.Current.Name })
}
# Test first-run outcomes using existing deterministic app hooks. No preview rendering.
foreach ($mode in @('ok','fail')) {
 '{"Lang":"en","Theme":"dark","AutoUpdate":false,"LocationPending":true}' | Set-Content "$settings/settings.json"
 $p=Start-Process $exe -ArgumentList "--e2e-detect=$mode" -PassThru
 Start-Sleep 10
 if ($p.HasExited) { throw "App exited during first-run $mode" }
 $names=Names $p
 $names | Set-Content "proof/first-run-$mode-names.txt"
 Capture "first-run-$mode"
 if ($mode -eq 'ok') {
  $s=Get-Content "$settings/settings.json" -Raw | ConvertFrom-Json
  if ($s.LocationPending -or $s.Location.Cc -ne 'US' -or $s.Location.Name -ne 'E2E City') { throw 'First-run detected location was not saved' }
 } else {
  if (($names -join '
') -notmatch 'location|Location') { throw 'Failure screen missing location controls' }
 }
 Stop-Process -Id $p.Id -Force
}
# Exercise the real provider path (no location fixture). CI has no GPS; IP fallback is approximate.
'{"Lang":"en","Theme":"light","AutoUpdate":false,"LocationPending":true}' | Set-Content "$settings/settings.json"
$p=Start-Process $exe -PassThru
Start-Sleep 32
if ($p.HasExited) { throw 'Real location launch exited' }
$names=Names $p
$names | Set-Content proof/live-location-names.txt
Capture live-location
if (-not (Test-Path "$settings/location.log")) { throw 'Real location code did not run' }
Copy-Item "$settings/location.log" proof/location.log
$s=Get-Content "$settings/settings.json" -Raw | ConvertFrom-Json
if ($s.LocationPending) { 'Live providers unavailable; deterministic fallback tests passed' | Set-Content proof/live-result.txt }
else { 'Live automatic location obtained and saved' | Set-Content proof/live-result.txt }
Stop-Process -Id $p.Id -Force
# Existing saved locations must not trigger lookup or be overwritten.
'{"Lang":"en","Theme":"dark","AutoUpdate":false,"LocationPending":false,"Location":{"Name":"Saved Cairo","NameAr":"Saved Cairo","Lat":30.0444,"Lon":31.2357,"Cc":"EG","UseCity":false}}' | Set-Content "$settings/settings.json"
$p=Start-Process $exe -ArgumentList '--e2e-detect=ok' -PassThru
Start-Sleep 8
$s=Get-Content "$settings/settings.json" -Raw | ConvertFrom-Json
if ($s.Location.Name -ne 'Saved Cairo' -or $s.Location.Cc -ne 'EG') { throw 'Saved location changed' }
Capture saved-location
Stop-Process -Id $p.Id -Force
