$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$out = Join-Path $repo 'verification-output'
$original = Join-Path $repo 'reference\FluentPrayerTimes-SelfContained'
$rebuilt = Join-Path $out 'native'
New-Item -ItemType Directory -Force "$out\screenshots" | Out-Null
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
Add-Type @'
using System;using System.Runtime.InteropServices;using System.Collections.Generic;using System.Text;
public class CaptureNative {
 public delegate bool EnumProc(IntPtr h,IntPtr l);
 [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc p,IntPtr l);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint p);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out RECT r);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [StructLayout(LayoutKind.Sequential)]public struct RECT {public int l,t,r,b;}
 public static List<IntPtr> Windows(uint pid){var a=new List<IntPtr>();EnumWindows((h,l)=>{uint p;GetWindowThreadProcessId(h,out p);if(p==pid&&IsWindowVisible(h))a.Add(h);return true;},IntPtr.Zero);return a;}
}
'@
$settings=Join-Path $env:APPDATA 'FluentPrayerTimes'
$env:FPT_XAML_DIAGNOSTICS='0'
$env:FPT_COMPONENT_PROBE='1'
$results=@()
$started=Get-Date
foreach($variant in @('rebuilt','native-original-pri','recovered')) {
 $dir=if($variant -eq 'original'){$original}elseif($variant -eq 'native-original-pri'){Join-Path $out 'native-original-pri'}elseif($variant -eq 'recovered'){Join-Path $out 'recovered'}else{$rebuilt}
 foreach($language in @('ar')) {
  foreach($part in @('main')) {
   Get-Process FluentPrayerTimes -ErrorAction SilentlyContinue | Stop-Process -Force
   New-Item -ItemType Directory -Force $settings | Out-Null
   '{"AutoUpdate":false,"Theme":"light","LocationPending":false}' | Set-Content "$settings\settings.json" -Encoding utf8
   $process=Start-Process "$dir\FluentPrayerTimes.exe" -WorkingDirectory $dir -ArgumentList @('--preview=iqama',"--part=$part","--lang=$language") -PassThru -RedirectStandardOutput "$out\$variant-$language-$part-stdout.log" -RedirectStandardError "$out\$variant-$language-$part-stderr.log"
   Start-Sleep -Seconds 9
   $windows=@([CaptureNative]::Windows([uint32]$process.Id))
   $count=0
   foreach($hwnd in $windows){
    $r=New-Object CaptureNative+RECT
    [CaptureNative]::GetWindowRect($hwnd,[ref]$r)|Out-Null
    $w=$r.r-$r.l;$h=$r.b-$r.t
    if($w -lt 60 -or $h -lt 40 -or $r.l -lt -1000){continue}
    [CaptureNative]::SetForegroundWindow($hwnd)|Out-Null
    Start-Sleep -Milliseconds 400
    $bmp=New-Object System.Drawing.Bitmap $w,$h
    $g=[System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($r.l,$r.t,0,0,(New-Object System.Drawing.Size $w,$h))
    $bmp.Save("$out\screenshots\$variant-$language-$part-$count.png")
    $g.Dispose();$bmp.Dispose();$count++
   }
   $results += [pscustomobject]@{variant=$variant;language=$language;part=$part;processExited=$process.HasExited;exitCode=if($process.HasExited){$process.ExitCode}else{$null};visibleCaptures=$count}
   if(Test-Path "$settings\crash.log"){Copy-Item "$settings\crash.log" "$out\$variant-$language-$part-crash.log"}
   if(Test-Path "$settings\xaml-diagnostics.log"){Copy-Item "$settings\xaml-diagnostics.log" "$out\$variant-$language-$part-xaml.log"}
   if(Test-Path "$settings\component-probe.log"){Copy-Item "$settings\component-probe.log" "$out\$variant-components.log"}
   Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
  }
 }
}
Get-WinEvent -FilterHashtable @{LogName="Application";StartTime=$started} -ErrorAction SilentlyContinue | Where-Object {$_.ProviderName -in @(".NET Runtime","Application Error","Windows Error Reporting")} | Format-List * | Out-File "$out\event-errors.log"
$results|ConvertTo-Json|Set-Content "$out\launch-results.json"
if(@($results|Where-Object {$_.visibleCaptures -eq 0}).Count -gt 0){throw 'Some native screens had no visible capture'}
