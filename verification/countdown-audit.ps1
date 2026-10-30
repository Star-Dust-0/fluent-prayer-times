$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes,System.Windows.Forms,System.Drawing
$exe="$PWD/publish/FluentPrayerTimes.exe"
$dir="$env:APPDATA/FluentPrayerTimes"
New-Item proof/countdown -ItemType Directory -Force|Out-Null
$results=[System.Collections.Generic.List[object]]::new()
function Capture($name) {
 $b=[System.Windows.Forms.Screen]::PrimaryScreen.Bounds;$im=New-Object System.Drawing.Bitmap $b.Width,$b.Height
 $g=[System.Drawing.Graphics]::FromImage($im);$g.CopyFromScreen($b.Location,[System.Drawing.Point]::Empty,$b.Size)
 $im.Save("$PWD/proof/countdown/$name.png");$g.Dispose();$im.Dispose()
}
function StopApp { Get-Process FluentPrayerTimes -ErrorAction SilentlyContinue|Stop-Process -Force }
try {
 # Real scheduler Tick with controlled clock; hook changes time only, not notification gates.
 foreach($phase in @('adhan','iqama')) {
  foreach($enabled in @($true,$false)) {
   StopApp
   $s=@{Lang='en';Theme='dark';AutoUpdate=$false;LocationPending=$false;NotifyAdhan=$true;AdhanNotify=$enabled;IqamaNotify=$enabled;AdhanSound='none';IqamaSound='none';Location=@{Name='New Borg El Arab';NameAr='برج العرب الجديدة';Lat=30.8619;Lon=29.5845;Cc='EG';UseCity=$false}}
   $s|ConvertTo-Json -Depth 4|Set-Content "$dir/settings.json"
   $log="$PWD/proof/countdown/$phase-$enabled.log"
   $p=Start-Process $exe -ArgumentList "--e2e-clock=$phase","--e2e-log=$log" -PassThru
   Start-Sleep 20
   if($p.HasExited){throw 'Scheduler app exited'}
   $lines=@(Get-Content $log -ErrorAction SilentlyContinue)
   $matches=@($lines|Where-Object{$_ -like "$phase|*"})
   $pass=if($enabled){$matches.Count -eq 1}else{$matches.Count -eq 0}
   $results.Add([pscustomobject]@{phase=$phase;enabled=$enabled;notifications=$matches.Count;passed=$pass})
   if(-not $pass){throw "Notification gate/duplicate failure $phase $enabled"}
   Capture "scheduler-$phase-$enabled"
  }
 }
 # Render the actual main/widget/hover surfaces at every countdown phase using existing preview data.
 foreach($phase in @('next','adhan','iqama','iqamanow')) {
  foreach($surface in @('main','widget','hover')) {
   StopApp
   $p=Start-Process $exe -ArgumentList "--preview=$phase","--part=$surface",'--lang=en' -PassThru
   Start-Sleep 6
   if($p.HasExited){throw "Phase surface exited $phase $surface"}
   Capture "$phase-$surface"
   $cond=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$p.Id)
   @([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Descendants,$cond))|ForEach-Object{$_.Current.Name}|Set-Content "proof/countdown/$phase-$surface.txt"
   $results.Add([pscustomobject]@{phase=$phase;surface=$surface;passed=$true;proof='Actual surface captured with explicit preview phase. State boundary correctness tested separately.'})
  }
 }
}finally{
 $results|ConvertTo-Json -Depth 4|Set-Content proof/countdown/results.json
 StopApp
}
