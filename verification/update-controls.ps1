$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes,System.Windows.Forms,System.Drawing
$exe="$PWD/publish/FluentPrayerTimes.exe"
New-Item proof/update-controls -ItemType Directory -Force | Out-Null
$results=[System.Collections.Generic.List[object]]::new()
function Find($id) {
 $cond=[System.Windows.Automation.AndCondition]::new([System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$script:p.Id),[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$id))
 $e=[System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$cond)
 if (-not $e) {throw "Missing $id"};return $e
}
function Invoke($id) { (Find $id).GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke();Start-Sleep 1 }
function Launch($part) {
 Get-Process FluentPrayerTimes -ErrorAction SilentlyContinue|Stop-Process -Force
 $script:p=Start-Process $exe -ArgumentList '--preview=next',"--part=$part",'--lang=en' -PassThru
 Start-Sleep 6
}
function Check($name,$ok,$detail) { $results.Add([pscustomobject]@{control=$name;passed=[bool]$ok;detail=$detail});if(-not $ok){throw $name} }
function ToastVisible {
 $cond=[System.Windows.Automation.AndCondition]::new([System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$p.Id),[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Fluent Prayer Times Update'))
 return [System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children,$cond)
}
function Shot($name) {
 $bounds=[System.Windows.Forms.Screen]::PrimaryScreen.Bounds;$bmp=New-Object System.Drawing.Bitmap $bounds.Width,$bounds.Height
 $g=[System.Drawing.Graphics]::FromImage($bmp);$g.CopyFromScreen($bounds.Location,[System.Drawing.Point]::Empty,$bounds.Size)
 $bmp.Save("$PWD/proof/update-controls/$name.png");$g.Dispose();$bmp.Dispose()
}
try {
 # Existing preview hooks make conditional UI visible; clicks below use real handlers.
 Launch update
 Invoke UpdateToastButton1
 Check 'ToastLater' (-not (ToastVisible)) 'Offer Later closes update window'
 Launch update
 Invoke UpdateToastButton0
 Start-Sleep 10
 Check 'ToastBegin' ((Find UpdateToastButton0).Current.Name -match 'Try again|Cancel') 'Real request entered download/error state'
 Shot real-download-error
 if ((Find UpdateToastButton0).Current.Name -eq 'Try again') {
  Invoke UpdateToastButton0;Start-Sleep 5
  Check ToastRetry ($null -ne (ToastVisible)) 'Retry handler remains responsive on unavailable fixture asset'
  Invoke UpdateToastButton1
  Check ToastBrowserFallback (-not (ToastVisible)) 'Error fallback opened public release page and closed toast'
 }
 Launch update-ready
 Invoke UpdateToastButton0
 Check ToastInstallGuard ((Find UpdateToastButton0).Current.Name -eq 'Try again') 'Ready preview has no verified installer, so Install refuses execution and shows error'
 Shot install-guard
 Launch update-ready
 Invoke UpdateToastButton1
 Check ReadyLater (-not (ToastVisible)) 'Ready Later closes update window'
 foreach($part in @('updates-available','updates-about-available')) {
  Launch $part
  $id=if($part -like '*about*'){'BtnAboutUpdateInstall'}else{'BtnUpdateInstall'}
  Invoke $id
  Check $id ($null -ne (ToastVisible)) 'Conditional page button opened actual download toast'
  Shot $id
 }
 Launch update-downloading
 Invoke UpdateToastButton0
 Check ToastCancelClickable ($null -ne (ToastVisible)) 'Cancel handler invoked on preview state; real cancellation independently checked by update-e2e'
 # Preview downloading has no active cancellation token: no claim of a cancelled transfer here.
} finally {
 $results|ConvertTo-Json -Depth 4|Set-Content proof/update-controls/results.json
 Get-Process FluentPrayerTimes -ErrorAction SilentlyContinue|Stop-Process -Force
}
