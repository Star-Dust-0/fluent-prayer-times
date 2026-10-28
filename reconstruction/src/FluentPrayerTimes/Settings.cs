using System;
using System.IO;
using System.Text.Json;

namespace FluentPrayerTimes;

public class Settings
{
	public Loc Location { get; set; } = new Loc();

	public bool RunAtStartup { get; set; }

	public string Backdrop { get; set; } = "micaalt";

	public string Theme { get; set; } = "system";

	public bool WidgetVisible { get; set; }

	public bool WidgetPinned { get; set; } = true;

	public int WidgetSnap { get; set; } = 2;

	public int WidgetX { get; set; } = int.MinValue;

	public int WidgetY { get; set; } = int.MinValue;

	public bool NotifyAdhan { get; set; } = true;

	public string Accent { get; set; } = "";

	public bool AutoUpdate { get; set; } = true;

	public string Lang { get; set; } = "";

	public string UpdateSeen { get; set; } = "";

	public bool AzkarOn { get; set; }

	public int AzkarMinutes { get; set; } = 30;

	public int CalculationMethod { get; set; } = -1;

	public int AsrSchool { get; set; } = -1;

	public int HighLatitude { get; set; } = -1;

	public double FajrAngle { get; set; } = 18.0;

	public double IshaAngle { get; set; } = 17.0;

	public int CustomRule { get; set; }

	public double FajrMinutes { get; set; } = 90.0;

	public double IshaMinutes { get; set; } = 90.0;

	public string TimeZoneOverride { get; set; } = "";

	public string Dst { get; set; } = "auto";

	public int[] IqamaMin { get; set; } = new int[6] { 20, 0, 20, 20, 15, 20 };

	public bool AdhanNotify { get; set; } = true;

	public bool IqamaNotify { get; set; } = true;

	public bool AdhanOn
	{
		get
		{
			if (NotifyAdhan)
			{
				return AdhanNotify;
			}
			return false;
		}
	}

	public bool IqamaOn
	{
		get
		{
			if (NotifyAdhan)
			{
				return IqamaNotify;
			}
			return false;
		}
	}

	public bool LocationPending { get; set; }

	public string AdhanSound { get; set; } = "default";

	public string IqamaSound { get; set; } = "default";

	private static string Dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FluentPrayerTimes");

	private static string F => Path.Combine(Dir, "settings.json");

	public string? TzParam
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(TimeZoneOverride))
			{
				return TimeZoneOverride;
			}
			string result;
			if (!DstApplies())
			{
				result = null;
			}
			else
			{
				string dst = Dst;
				string text = ((dst == "off") ? "Etc/GMT-2" : ((!(dst == "on")) ? null : "Etc/GMT-3"));
				result = text;
			}
			return result;
		}
	}

	public int IqamaFor(int i)
	{
		int[] iqamaMin = IqamaMin;
		int num = ((iqamaMin != null && i >= 0 && i < iqamaMin.Length) ? iqamaMin[i] : 0);
		if (num <= 0)
		{
			num = ((i == 4) ? 15 : 20);
		}
		return Math.Clamp(num, 2, 180);
	}

	public static Settings Load()
	{
		try
		{
			if (!File.Exists(F))
			{
				return new Settings
				{
					LocationPending = true
				};
			}
			return JsonSerializer.Deserialize<Settings>(File.ReadAllText(F)) ?? new Settings();
		}
		catch
		{
			return new Settings();
		}
	}

	public void Save()
	{
		try
		{
			Directory.CreateDirectory(Dir);
			File.WriteAllText(F, JsonSerializer.Serialize(this));
		}
		catch
		{
		}
	}

	public bool DstApplies()
	{
		return Location.Cc == "EG";
	}
}
