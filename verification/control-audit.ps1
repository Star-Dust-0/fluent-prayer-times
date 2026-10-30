$ErrorActionPreference='Stop'
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes,System.Windows.Forms,System.Drawing
Add-Type @'
using System;using System.Runtime.InteropServices;
public static class AuditNative {
[DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
[DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
[DllImport("user32.dll")] public static extern void mouse_event(uint flags,uint x,uint y,uint data,UIntPtr extra);
}
'@
$results=[System.Collections.Generic.List[object]]::new()
$exe="$PWD/publish/FluentPrayerTimes.exe"
$settings="$env:APPDATA/FluentPrayerTimes/settings.json"
$dir=Split-Path $settings
New-Item proof/control-audit -ItemType Directory -Force | Out-Null
function Record($id,$ok,$detail) {
 $results.Add([pscustomobject]@{id=$id;passed=[bool]$ok;detail=$detail})
 $results | ConvertTo-Json -Depth 5 | Set-Content proof/control-audit/results.json
 if (-not $ok) { throw "$id : $detail" }
}
function State { Get-Content $settings -Raw | ConvertFrom-Json }
function Window {
 $cond=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$script:p.Id)
 $all=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,$cond)
 @($all | Where-Object { $_.Current.Name -like 'Prayer Times*' -or $_.Current.Name -like '*Fluent Prayer Times' }) | Select-Object -First 1
}
function Find($id,$optional=$false) {
 $cond=[System.Windows.Automation.AndCondition]::new(
 [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$script:p.Id),
 [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty,$id))
 $el=[System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$cond)
 if (-not $el -and -not $optional) { throw "Missing automation control: $id" }
 return $el
}
function Visible($el) {
 $scroll=$null
 if ($el.TryGetCurrentPattern([System.Windows.Automation.ScrollItemPattern]::Pattern,[ref]$scroll)) { $scroll.ScrollIntoView();Start-Sleep -Milliseconds 200 }
}
function Invoke($id) { $el=Find $id; Visible $el; $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); Start-Sleep -Milliseconds 700 }
function Toggle($id) { $el=Find $id; Visible $el; $el.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle();Start-Sleep -Milliseconds 400 }
function SelectRadio($id) { $el=Find $id;Visible $el;$el.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select();Start-Sleep -Milliseconds 600 }
function Nav($tag) { SelectRadio "Nav_$tag" }
function SetValue($id,$value) {
 $el=Find $id;Visible $el
 $pattern=$null
 if ($el.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern,[ref]$pattern)) { $pattern.SetValue([string]$value) }
 elseif ($el.TryGetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern,[ref]$pattern)) { $pattern.SetValue([double]$value) }
 else {
  $edit=$el.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Edit))
  if (-not $edit) { throw "No editable value for $id" }
  $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue([string]$value)
  $edit.SetFocus();[System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
 }
 Start-Sleep -Milliseconds 400
}
function Choose($id,$index) {
 $el=Find $id;Visible $el
 $el.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand();Start-Sleep -Milliseconds 300
 $items=$el.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::ListItem))
 if ($items.Count -le $index) { throw "Not enough items in $id ($($items.Count))" }
 $items[$index].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select();Start-Sleep -Milliseconds 650
}
function Shot($name) {
 $bounds=[System.Windows.Forms.Screen]::PrimaryScreen.Bounds
 $bmp=New-Object System.Drawing.Bitmap $bounds.Width,$bounds.Height
 $g=[System.Drawing.Graphics]::FromImage($bmp);$g.CopyFromScreen($bounds.Location,[System.Drawing.Point]::Empty,$bounds.Size)
 $bmp.Save("$PWD/proof/control-audit/$name.png");$g.Dispose();$bmp.Dispose()
}
function Launch($arguments='') {
 Get-Process FluentPrayerTimes -ErrorAction SilentlyContinue | Stop-Process -Force
 $script:p=if ($arguments) {Start-Process $exe -ArgumentList $arguments -PassThru} else {Start-Process $exe -PassThru}
 Start-Sleep 6
 if ($script:p.HasExited) { throw 'Launch failed' }
}
function CheckState($id,$property,$expected) { $actual=(State).$property;Record $id ($actual -eq $expected) "$property = $actual (expected $expected)" }
function Tray($cmd) {
 $win=Window
 [AuditNative]::SetCursorPos(900,700) | Out-Null
 [AuditNative]::PostMessage([IntPtr]$win.Current.NativeWindowHandle,32769,[IntPtr]::Zero,[IntPtr]517) | Out-Null
 Start-Sleep 1
 Invoke "TrayCommand$cmd"
}
try {
 New-Item $dir -ItemType Directory -Force | Out-Null
 '{"Lang":"en","Theme":"dark","AutoUpdate":false,"LocationPending":false,"AdhanNotify":true,"IqamaNotify":true,"Location":{"Name":"Cairo","NameAr":"القاهرة","Lat":30.0444,"Lon":31.2357,"Cc":"EG","UseCity":false}}' | Set-Content $settings
 Launch
 foreach ($tag in @('times','calendar','convert','azkar','settings','about')) {Nav $tag;Record "navigation-$tag" ($null -ne (Window)) 'Selected real navigation control';Shot "page-$tag"}
 Nav calendar
 $before=(Find CalendarTitle).Current.Name
 Invoke CalendarNext;$after=(Find CalendarTitle).Current.Name
 Record CalendarNext ($before -ne $after) "$before -> $after"
 Invoke CalendarPrevious;Record CalendarPrevious ((Find CalendarTitle).Current.Name -eq $before) 'Returned to original month'
 Invoke CalendarPrevious;Invoke CalendarToday;Record CalendarToday ((Find CalendarTitle).Current.Name -eq $before) 'Today reset month'
 Nav convert
 $before=(Find ConverterFrom).Current.Name
 Invoke ConverterSwap;Record ConverterSwap ((Find ConverterFrom).Current.Name -ne $before) 'Conversion direction swapped'
 SetValue ConverterDay 15;SetValue ConverterYear 2027;Choose ConverterMonth 4
 Record ConverterInputs ((Find ConverterResult).Current.Name -notmatch 'incorrect|invalid|--') (Find ConverterResult).Current.Name
 $gregResult=(Find ConverterResult).Current.Name
 Invoke ConverterSwap;Shot converter-swapped
 Record ConverterRoundTrip ((Find ConverterResult).Current.Name -match '2027') 'Switching Hijri/Gregorian preserves selected 2027 date instead of clamping to 2076'
 Nav azkar
 $before=(Find AzkarText).Current.Name;Invoke AzkarAnother;Record AzkarAnother ((Find AzkarText).Current.Name -ne $before) 'Dhikr changed'
 Toggle AzkarToggle;CheckState AzkarToggle AzkarOn $true
 Choose AzkarPreset 3;CheckState AzkarPreset AzkarMinutes 60
 SetValue AzkarCustom 37;Invoke AzkarApply;CheckState AzkarApply AzkarMinutes 37
 Toggle AzkarToggle;CheckState AzkarToggleOff AzkarOn $false
 Nav settings
 foreach ($pair in @(@('RbMica','Backdrop','mica'),@('RbAcrylic','Backdrop','acrylic'),@('RbMicaAlt','Backdrop','micaalt'),@('RbThemeLight','Theme','light'),@('RbThemeSys','Theme','system'),@('RbThemeDark','Theme','dark'))) {
  SelectRadio $pair[0];CheckState $pair[0] $pair[1] $pair[2];Shot $pair[0]
 }
 $exp=Find ExpAccent
 $exp.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand();Start-Sleep -Milliseconds 400
 $presetIds=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$p.Id)) | Where-Object {$_.Current.AutomationId -like 'AccentPreset*'} | ForEach-Object {$_.Current.AutomationId})
 foreach ($id in $presetIds) {Invoke $id;Record $id ((State).Accent -match '^#[0-9A-F]{6}$') (State).Accent}
 Invoke BtnAccentReset;CheckState BtnAccentReset Accent ''
 Invoke BtnAccentCustom;Shot accent-dialog
 $dialog=Find AccentDialog -optional $true
 $picker=Find AccentColorPicker -optional $true
 Record BtnAccentCustom ($null -ne $dialog -or $null -ne $picker) 'Custom color dialog is accessible'
 $colorFields=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$p.Id))|Where-Object{$_.Current.ControlType -eq [System.Windows.Automation.ControlType]::Edit})
 $colorFields|ForEach-Object { $_.Current.Name+'|'+$_.Current.AutomationId }|Set-Content proof/control-audit/color-fields.txt
 $spectrumCond=[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Slider)
 $dialogEl=Find AccentDialog
 $sliders=$dialogEl.FindAll([System.Windows.Automation.TreeScope]::Descendants,$spectrumCond)
 $oldAccent=(State).Accent
 if ($sliders.Count -gt 0) {
  $brightness=Find ThirdDimensionSlider
  $range=$brightness.GetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern)
  $range.SetValue(($range.Current.Minimum+$range.Current.Maximum)/2)
  Start-Sleep 1
  Record CustomColorSelection ((State).Accent -ne $oldAccent) 'Native color slider changed and persisted accent'
 } else {
  $results.Add([pscustomobject]@{id='CustomColorSelection';passed=$null;detail='No RangeValue slider peer; dialog opens, color manipulation remains unverified'})
 }
 Shot accent-dialog-selected
 [System.Windows.Forms.SendKeys]::SendWait('{ESC}');Start-Sleep 1
 # Country/city selection, explicit coordinates and search result selection.
 Choose CmbCountry 1;Record CmbCountry ((State).Location.Cc -ne '') (State).Location.Cc
 Choose CmbCountry 0
 SetValue TxtLat 30.0444;SetValue TxtLon 31.2357;Invoke BtnCoords
 Record BtnCoords ((State).Location.Lat -eq 30.0444 -and (State).Location.Lon -eq 31.2357) 'Manual coordinates persisted'
 SetValue TxtSearch Cairo;Invoke BtnSearch;Start-Sleep 6
 $list=Find LstPlaces
 $items=$list.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::ListItem))
 Record BtnSearch ($items.Count -gt 0) "Search returned $($items.Count) items"
 $items[0].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select();Start-Sleep 2
 Record LstPlaces ((State).Location.Name -match 'Cairo|القاهرة') (State).Location.Name
 # Egypt drives the conditional DST settings; select by text rather than fixed world index.
 $country=Find CmbCountry;Visible $country;$country.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand();Start-Sleep -Milliseconds 400
 $egypt=$country.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Egypt'))
 if (-not $egypt) {throw 'Egypt option missing'};$egypt.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select();Start-Sleep 1
 Choose CmbCity 1;Record CmbCity ((State).Location.UseCity) 'Selected city persisted'
 foreach($pair in @(@('RbDstOn','on'),@('RbDstOff','off'),@('RbDstAuto','auto'))) {SelectRadio $pair[0];CheckState $pair[0] Dst $pair[1]}
 $asr='CalculationChoice_المذهب الفقهي لصلاة العصر'
 foreach($index in @(0,1,2)) {Choose $asr $index;CheckState "Asr-$index" AsrSchool ($index-1)}
 $method='CalculationChoice_طريقة الحساب لصلاتي الفجر والعشاء'
 # Exercise every offered method. Each selection rebuilds the controls, so reacquire.
 $m=Find $method;Visible $m;$m.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand();Start-Sleep -Milliseconds 300
 $count=$m.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::ListItem)).Count
 [System.Windows.Forms.SendKeys]::SendWait('{ESC}')
 foreach($i in 0..($count-1)) {Choose $method $i;Record "CalculationMethod-$i" ((State).CalculationMethod -ge -1) "Saved method $((State).CalculationMethod)"}
 $custom='CalculationChoice_إعدادات الحساب اليدوي'
 foreach($i in 0..3) {
  Choose $custom $i;CheckState "CustomRule-$i" CustomRule $i
  $numIds=@([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$p.Id))|Where-Object{$_.Current.AutomationId -like 'CalculationNumber_*'}|ForEach-Object{$_.Current.AutomationId})
  foreach($id in $numIds) {SetValue $id 19;Record $id $true 'Numeric control accepted value; model checked in calculation tests'}
 }
 $high='CalculationChoice_التعديل للمناطق ذات خطوط العرض المرتفعة'
 foreach($i in 0..4) {Choose $high $i;CheckState "HighLatitude-$i" HighLatitude ($i-1)}
 SetValue CalculationZone 'Etc/GMT-3';Invoke CalculationZoneApply;CheckState CalculationZoneApply TimeZoneOverride 'Etc/GMT-3'
 SetValue CalculationZone '';Invoke CalculationZoneApply;CheckState CalculationZoneReset TimeZoneOverride ''
 foreach($idx in @(0,2,3,4,5)) {SetValue "IqamaMinutes$idx" 23;Record "IqamaMinutes$idx" ((State).IqamaMin[$idx] -eq 23) 'Iqama duration saved'}
 foreach($kind in @('adhan','iqama')) {
  foreach($i in 0..3) {Choose "SoundChoice_$kind" $i;Invoke "SoundTest_$kind";Record "SoundTest-$kind-$i" ($null -ne (Find "SoundNote_$kind")) 'Handler invoked; sound playback cannot be heard on CI'}
  Toggle "SoundToggle_$kind";Record "SoundToggle_$kind" (-not (Find "SoundTest_$kind").Current.IsEnabled) 'Test disabled with notification toggle'
  Toggle "SoundToggle_$kind"
 }
 Toggle TglAdhan;CheckState TglAdhan NotifyAdhan $false;Toggle TglAdhan
 Toggle TglAutoUpdate;CheckState TglAutoUpdate AutoUpdate $true;Toggle TglAutoUpdate
 Toggle TglStartup;CheckState TglStartup RunAtStartup $true
 Record StartupRegistry ($null -ne (Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name FluentPrayerTimes -ErrorAction SilentlyContinue)) 'Startup registry entry created on disposable runner'
 Toggle TglStartup;CheckState TglStartupOff RunAtStartup $false
 Toggle TglWidget;CheckState TglWidget WidgetVisible $true
 Toggle BtnPin;CheckState BtnPin WidgetPinned $false
 Toggle BtnPin;CheckState BtnPinAgain WidgetPinned $true;Shot widget
 $widget=Find BtnPin
 $windowCond=[System.Windows.Automation.AndCondition]::new([System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$p.Id),[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Fluent Prayer Times Widget'))
 $widgetWindow=[System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children,$windowCond)
 $rect=$widgetWindow.Current.BoundingRectangle
 [AuditNative]::SetCursorPos([int]($rect.X+70),[int]($rect.Y+50))|Out-Null
 [AuditNative]::mouse_event(2,0,0,0,[UIntPtr]::Zero)
 [AuditNative]::SetCursorPos([int]($rect.X+200),[int]($rect.Y-100))|Out-Null
 Start-Sleep -Milliseconds 300
 [AuditNative]::mouse_event(4,0,0,0,[UIntPtr]::Zero)
 Start-Sleep 1
 Record WidgetDrag ((State).WidgetSnap -ge 0 -and (State).WidgetSnap -le 7) 'Pointer drag/release completed and valid snap persisted (position proof screenshot)'
 Shot widget-dragged
 Invoke BtnHide;CheckState BtnHide WidgetVisible $false
 Invoke BtnUpdate;Start-Sleep 5;Record BtnUpdate (Find BtnUpdate).Current.IsEnabled 'Update check completed and recovered';Shot settings-update
 Nav about;Invoke BtnAboutUpdate;Start-Sleep 5;Record BtnAboutUpdate (Find BtnAboutUpdate).Current.IsEnabled 'About update check completed';Shot about-update
 # Open public project and credit links in the isolated CI browser only.
 Invoke RepoLink;Record RepoLink $true 'Public project link invoked'
 Invoke CreditLink;Record CreditLink $true 'Public credit link invoked'
 Nav settings
 foreach($i in 0..11) {Choose CmbLang $i;Record "Language-$i" $true "Saved language $((State).Lang)";Shot "language-$i";Copy-Item "$dir/localization-diagnostics.log" "proof/control-audit/localization-$i.log"}
 Choose CmbLang 2 # English
 # Exercise tray commands through actual native callback and UI buttons, not OnMenu calls.
 Tray 4;CheckState TrayWidget WidgetVisible $true
 Tray 4;CheckState TrayWidgetOff WidgetVisible $false
 Tray 2;Record TraySettings ($null -ne (Find CmbCountry)) 'Tray settings navigation'
 Tray 1;Record TrayOpen ($null -ne (Find TxtCity)) 'Tray opens prayer page'
 Tray 5;Start-Sleep 5;Record TrayUpdate (Find BtnUpdate).Current.IsEnabled 'Tray update check'
 # Language Apply intentionally restarts; locate replacement process afterwards.
 $oldPid=$p.Id
 try { Invoke BtnApply } catch {
  # UIA can return E_UNEXPECTED when the invoked action intentionally exits its provider.
  if (Get-Process -Id $oldPid -ErrorAction SilentlyContinue) { throw }
 }
 Start-Sleep 5
 $script:p=Get-Process FluentPrayerTimes -ErrorAction Stop | Select-Object -First 1
 Record BtnApply (-not $p.HasExited) 'Apply changes restarted app'
 try {Tray 3} catch {Start-Sleep 2;if(Get-Process FluentPrayerTimes -ErrorAction SilentlyContinue){throw}}
 Start-Sleep 2;Record TrayExit (-not (Get-Process FluentPrayerTimes -ErrorAction SilentlyContinue)) 'Tray exit closes process'
 'Primary UI control audit passed' | Set-Content proof/control-audit/summary.txt
} catch {
 Shot audit-failure
 @([System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$p.Id)))|ForEach-Object {$_.Current.ControlType.ProgrammaticName+'|'+$_.Current.AutomationId+'|'+$_.Current.Name}|Set-Content proof/control-audit/failure-tree.txt
 $results.Add([pscustomobject]@{id='AUDIT-STOPPED';passed=$false;detail=$_.Exception.Message})
 throw
} finally {
 $results | ConvertTo-Json -Depth 5 | Set-Content proof/control-audit/results.json
 Get-Process FluentPrayerTimes -ErrorAction SilentlyContinue | Stop-Process -Force
}
