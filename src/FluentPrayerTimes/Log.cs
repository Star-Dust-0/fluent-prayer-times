using System;
using System.IO;

namespace FluentPrayerTimes;

internal static class Log
{
    private static readonly object Gate = new();
    private const long Limit = 256 * 1024;
    internal static void Error(string area, Exception error) => Write(area, error.GetType().Name + ": " + error.Message);
    internal static void Write(string area, string message)
    {
        try
        {
            lock (Gate)
            {
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FluentPrayerTimes");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "app.log");
                if (File.Exists(path) && new FileInfo(path).Length > Limit) File.Move(path, path + ".1", true);
                // Callers must not pass coordinates, addresses, tokens or response bodies.
                File.AppendAllText(path, DateTimeOffset.UtcNow.ToString("O") + " " + area + " " + message + Environment.NewLine);
            }
        }
        catch { /* Logging must never prevent recovery. */ }
    }
}
