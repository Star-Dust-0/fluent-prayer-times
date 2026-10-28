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
$results=@()
foreach($variant in @('original','rebuilt')) {
 $dir=if($variant -eq 'original'){$original}else{$rebuilt}
 foreach($language in @('ar','en')) {
  foreach($part in @('main','calendar','settings','calculation','sounds','about','widget','hover')) {
   Get-Process FluentPrayerTimes -ErrorAction SilentlyContinue | Stop-Process -Force
   New-Item -ItemType Directory -Force $settings | Out-Null
   '{"AutoUpdate":false,"Theme":"light","LocationPending":false}' | Set-Content "$settings\settings.json" -Encoding utf8
   $process=Start-Process "$dir\FluentPrayerTimes.exe" -WorkingDirectory $dir -ArgumentList @('--preview=iqama',"--part=$part","--lang=$language") -PassThru
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
   $results += [pscustomobject]@{variant=$variant;language=$language;part=$part;processExited=$process.HasExited;visibleCaptures=$count}
   if(Test-Path "$settings\crash.log"){Copy-Item "$settings\crash.log" "$out\$variant-$language-$part-crash.log"}
   Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
  }
 }
}
$results|ConvertTo-Json|Set-Content "$out\launch-results.json"
if(@($results|Where-Object {$_.visibleCaptures -eq 0}).Count -gt 0){throw 'Some native screens had no visible capture'}
