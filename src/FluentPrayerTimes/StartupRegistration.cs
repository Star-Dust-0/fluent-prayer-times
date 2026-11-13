using System;
using Microsoft.Win32;
namespace FluentPrayerTimes;

internal static class StartupRegistration
{
    private const string Run = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string Approved = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string Name = "FluentPrayerTimes";
    internal static bool Enabled
    {
        get
        {
            try
            {
                using var run = Registry.CurrentUser.OpenSubKey(Run);
                using var approved = Registry.CurrentUser.OpenSubKey(Approved);
                var bytes = approved?.GetValue(Name) as byte[];
                return run?.GetValue(Name) is string && (bytes == null || bytes.Length == 0 || (bytes[0] & 1) == 0);
            }
            catch (Exception e) { Log.Error("startup-read", e); return false; }
        }
    }
    internal static void Refresh(Settings settings)
    {
        if (!settings.RunAtStartup) return;
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(Run, true);
            string expected = "\"" + Environment.ProcessPath + "\" --tray";
            if (key.GetValue(Name) as string != expected) key.SetValue(Name, expected);
        }
        catch (Exception e) { Log.Error("startup-repair", e); }
    }
    internal static void Set(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(Run, true);
            if (enabled) key.SetValue(Name, "\"" + Environment.ProcessPath + "\" --tray");
            else key.DeleteValue(Name, false);
            using var approved = Registry.CurrentUser.OpenSubKey(Approved, true);
            approved?.DeleteValue(Name, false);
        }
        catch (Exception e) { Log.Error("startup-write", e); }
    }
}
