using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace FluentPrayerTimes;

internal static class Storage
{
    private static readonly object Gate = new();

    internal static void Write(string path, string text)
    {
        lock (Gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var bytes = Encoding.UTF8.GetBytes(text);
                    stream.Write(bytes);
                    stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }

    internal static T? Read<T>(string path)
    {
        lock (Gate)
        {
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try { return JsonSerializer.Deserialize<T>(File.ReadAllText(candidate)) ?? throw new JsonException("Empty document"); }
                catch (Exception e) when (e is JsonException || e is IOException || e is UnauthorizedAccessException)
                {
                    Log.Error("storage", e);
                    if (candidate == path && e is JsonException)
                    {
                        try { File.Move(path, path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")); }
                        catch (Exception moveError) { Log.Error("storage-recovery", moveError); }
                    }
                }
            }
            return default;
        }
    }
}
