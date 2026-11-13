using System;
using System.IO;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace FluentPrayerTimes;

internal static class Sounds
{
    private static readonly object Gate = new();
    private static MediaPlayer? _player;
    private static readonly string[] Extensions = { ".mp3", ".wav", ".m4a", ".wma", ".aac" };
    internal static bool Playing { get { lock (Gate) return _player != null; } }
    public static string Dir => Path.Combine(AppContext.BaseDirectory, "Audio");
    private static string? FindFile(string name)
    {
        foreach (string extension in Extensions)
        {
            string path = Path.Combine(Dir, name + extension);
            if (File.Exists(path)) return path;
        }
        return null;
    }
    public static (string path, bool trim)? Find(string kind, string mode)
    {
        string? path = mode == "full" ? FindFile(kind + "-full") : mode == "short" ? FindFile(kind + "-short") ?? FindFile(kind + "-full") : null;
        return path == null ? null : (path, false);
    }
    public static bool Available(string kind, string mode) => Find(kind, mode).HasValue;
    public static bool Play(string kind, string mode)
    {
        var source = Find(kind, mode);
        if (!source.HasValue) return false;
        try
        {
            lock (Gate)
            {
                Stop();
                var player = new MediaPlayer { Source = MediaSource.CreateFromUri(new Uri(source.Value.path)) };
                _player = player;
                player.CommandManager.IsEnabled = false;
                player.MediaEnded += (_, _) => Release(player);
                player.MediaFailed += (_, _) => Release(player);
                player.Play();
            }
            return true;
        }
        catch (Exception e) { Log.Error("sound", e); Stop(); return false; }
    }
    private static void Release(MediaPlayer player)
    {
        lock (Gate)
        {
            if (!ReferenceEquals(_player, player)) return;
            _player = null;
            try { player.Source = null; player.Dispose(); }
            catch (Exception e) { Log.Error("sound-release", e); }
        }
    }
    public static void Stop()
    {
        lock (Gate) { if (_player != null) Release(_player); }
    }
}
