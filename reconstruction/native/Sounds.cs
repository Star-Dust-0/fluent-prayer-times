using System;
using System.IO;
using System.Threading;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace FluentPrayerTimes;

internal static class Sounds
{
	public const int ShortSeconds = 16;

	private static MediaPlayer? _p;

	private static Timer? _cut;

	private static readonly string[] Ext = new string[5] { ".mp3", ".wav", ".m4a", ".wma", ".aac" };

	public static string Dir => Path.Combine(AppContext.BaseDirectory, "Audio");

	private static string? File_(string name)
	{
		string[] ext = Ext;
		foreach (string text in ext)
		{
			string text2 = Path.Combine(Dir, name + text);
			if (File.Exists(text2))
			{
				return text2;
			}
		}
		return null;
	}

	public static (string path, bool trim)? Find(string kind, string mode)
	{
		if (mode == "full")
		{
			string text = File_(kind + "-full");
			if (text != null)
			{
				return (text, false);
			}
			return null;
		}
		if (mode == "short")
		{
			string text2 = File_(kind + "-short");
			if (text2 != null)
			{
				return (text2, false);
			}
			string text3 = File_(kind + "-full");
			if (text3 != null)
			{
				return (text3, false);
			}
		}
		return null;
	}

	public static bool Available(string kind, string mode)
	{
		return Find(kind, mode).HasValue;
	}

	public static bool Play(string kind, string mode)
	{
		(string, bool)? tuple = Find(kind, mode);
		if (!tuple.HasValue)
		{
			return false;
		}
		try
		{
			Stop();
			_p = new MediaPlayer
			{
				Source = MediaSource.CreateFromUri(new Uri(tuple.Value.Item1))
			};
			_p.CommandManager.IsEnabled = false;
			_p.Play();
			if (tuple.Value.Item2)
			{
				_cut = new Timer(delegate
				{
					Stop();
				}, null, 16000, -1);
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static void Stop()
	{
		try
		{
			_cut?.Dispose();
		}
		catch
		{
		}
		_cut = null;
		try
		{
			_p?.Dispose();
		}
		catch
		{
		}
		_p = null;
	}
}
