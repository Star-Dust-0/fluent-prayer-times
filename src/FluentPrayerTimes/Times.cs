using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FluentPrayerTimes;

public static class Times
{
	public record Result(Day? Today, Day? Tomorrow, bool Offline);

	public enum Phase
	{
		Next,
		Adhan,
		Iqama,
		IqamaNow
	}

	public record Disp(Phase Phase, int Idx, DateTime Time, bool Tomorrow, TimeSpan Left);

	public record Place(string Label, double Lat, double Lon, string Name = "", string Country = "", string Cc = "");

	public static readonly string[] Keys = new string[6] { "Fajr", "Sunrise", "Dhuhr", "Asr", "Maghrib", "Isha" };

	private static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FluentPrayerTimes");

	public static readonly HttpClient Http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(15.0)
	};

	private static readonly Regex Diac = new Regex("[\u064b-\u0652\u0670]");

	public static string[] Names => new string[6]
	{
		L.T("الفجر"),
		L.T("الشروق"),
		L.T("الظهر"),
		L.T("العصر"),
		L.T("المغرب"),
		L.T("العشاء")
	};

	private static string CacheFile => Path.Combine(Dir, "cache3.json");

	public static string CalculationQuery(Settings s)
	{
		string text = "";
		if (s.CalculationMethod >= 0)
		{
			text = text + "&method=" + s.CalculationMethod;
		}
		if (s.AsrSchool >= 0)
		{
			text = text + "&school=" + s.AsrSchool;
		}
		if (s.HighLatitude >= 0)
		{
			text = text + "&latitudeAdjustmentMethod=" + s.HighLatitude;
		}
		if (s.CalculationMethod == 99)
		{
			text = text + "&methodSettings=" + s.FajrAngle.ToString(CultureInfo.InvariantCulture) + ",null," + s.IshaAngle.ToString(CultureInfo.InvariantCulture);
		}
		if (s.TzParam != null)
		{
			text = text + "&timezonestring=" + Uri.EscapeDataString(s.TzParam);
		}
		return text;
	}

	public static string CacheKey(Settings s, DateTime d)
	{
		return $"{s.Location.Key}|{s.Location.Cc}|{s.Location.Lat.ToString(CultureInfo.InvariantCulture)}|{s.Location.Lon.ToString(CultureInfo.InvariantCulture)}|{CalculationQuery(s)}|{s.CustomRule}|{s.FajrMinutes.ToString(CultureInfo.InvariantCulture)}|{s.IshaMinutes.ToString(CultureInfo.InvariantCulture)}|local1|{d:yyyy-MM-dd}";
	}

	private static string CK(Settings s, DateTime d)
	{
		return CacheKey(s, d);
	}

	private static Dictionary<string, Day> Read()
	{
		try
		{
			return JsonSerializer.Deserialize<Dictionary<string, Day>>(File.ReadAllText(CacheFile)) ?? new Dictionary<string, Day>();
		}
		catch
		{
			return new Dictionary<string, Day>();
		}
	}

	private static async Task<Day> Fetch(Settings s, DateTime d)
	{
		Loc l = s.Location;
		CultureInfo inv = CultureInfo.InvariantCulture;
		string requestUri = $"https://api.aladhan.com/v1/timings/{d:dd-MM-yyyy}?latitude={l.Lat.ToString(inv)}&longitude={l.Lon.ToString(inv)}{CalculationQuery(s)}";
		using JsonDocument jsonDocument = JsonDocument.Parse(await Http.GetStringAsync(requestUri));
		JsonElement property = jsonDocument.RootElement.GetProperty("data");
		JsonElement property2 = property.GetProperty("timings");
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		string[] keys = Keys;
		foreach (string text in keys)
		{
			string text2 = property2.GetProperty(text).GetString() ?? "";
			if (text2.Length < 5 || !DateTime.TryParseExact(text2.Substring(0, 5), "HH:mm", inv, DateTimeStyles.None, out var _))
			{
				throw new InvalidOperationException("Prayer time is unavailable for the selected calculation settings: " + text);
			}
			dictionary[text] = text2.Substring(0, 5);
		}
		int i;
		if (s.CalculationMethod == 99)
		{
			i = s.CustomRule;
			if ((uint)(i - 2) <= 1u)
			{
				dictionary["Fajr"] = At(d, dictionary["Sunrise"]).AddMinutes(0.0 - s.FajrMinutes).ToString("HH:mm", inv);
			}
			i = s.CustomRule;
			if ((uint)(i - 1) <= 1u)
			{
				dictionary["Isha"] = At(d, dictionary["Maghrib"]).AddMinutes(s.IshaMinutes).ToString("HH:mm", inv);
			}
		}
		JsonElement property3 = property.GetProperty("date").GetProperty("hijri");
		string hijri = $"{property3.GetProperty("day").GetString().TrimStart('0')} {Diac.Replace(property3.GetProperty("month").GetProperty("ar").GetString(), "")} {property3.GetProperty("year").GetString()} هـ";
		string text3 = null;
		try
		{
			text3 = property.GetProperty("meta").GetProperty("timezone").GetString();
		}
		catch
		{
		}
		JsonElement property4 = property.GetProperty("meta");
		JsonElement value;
		int num = (property4.GetProperty("method").TryGetProperty("id", out value) ? value.GetInt32() : s.CalculationMethod);
		JsonElement value2;
		int num2 = ((property4.TryGetProperty("school", out value2) && value2.GetString() == "HANAFI") ? 1 : 0);
		i = ((!property4.TryGetProperty("latitudeAdjustmentMethod", out var value3)) ? 3 : (value3.GetString() switch
		{
			"NONE" => 0, 
			"MIDDLE_OF_THE_NIGHT" => 1, 
			"ONE_SEVENTH" => 2, 
			_ => 3, 
		}));
		int num3 = i;
		if (num != 99 || s.CustomRule == 0)
		{
			double? num4 = OffsetHours(s, text3, d);
			double? customFajr = ((num == 99) ? new double?(s.FajrAngle) : ((double?)null));
			double? customIsha = ((num == 99) ? new double?(s.IshaAngle) : ((double?)null));
			Dictionary<string, string> dictionary2 = ((!num4.HasValue) ? null : SolarCalc.Compute(d, l.Lat, l.Lon, num4.Value, num, customFajr, customIsha, (num2 != 1) ? 1 : 2, num3));
			if (dictionary2 != null)
			{
				dictionary = dictionary2;
			}
		}
		return new Day(dictionary, hijri, text3, num, num2, num3);
	}

	internal static double? OffsetHours(Settings s, string? zone, DateTime d)
	{
		string text = s.TzParam ?? zone;
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		Match match = Regex.Match(text, "^Etc/GMT([+-])(\\d{1,2})$");
		if (match.Success)
		{
			int num = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
			return (match.Groups[1].Value == "-") ? num : (-num);
		}
		try
		{
			return TimeZoneInfo.FindSystemTimeZoneById(text).GetUtcOffset(new DateTime(d.Year, d.Month, d.Day, 12, 0, 0)).TotalHours;
		}
		catch
		{
			return null;
		}
	}

	private static async Task<Day> Get(Settings s, DateTime d, Dictionary<string, Day> cache)
	{
		string k = CK(s, d);
		if (cache.TryGetValue(k, out Day value))
		{
			return value;
		}
		return cache[k] = await Fetch(s, d);
	}

	public static async Task<Result> Load(Settings s)
	{
		Dictionary<string, Day> cache = Read();
		DateTime now = Now(s.Location.Tz);
		DateTime tom = now.Date.AddDays(1.0);
		Day today = null;
		Day value = null;
		bool off = false;
		try
		{
			today = await Get(s, now, cache);
		}
		catch
		{
			off = true;
		}
		if (today?.Tz != null && today.Tz != s.Location.Tz)
		{
			s.Location.Tz = today.Tz;
			now = Now(s.Location.Tz);
			tom = now.Date.AddDays(1.0);
			try
			{
				today = await Get(s, now, cache);
			}
			catch
			{
				off = true;
			}
		}
		try
		{
			value = await Get(s, tom, cache);
		}
		catch
		{
			cache.TryGetValue(CK(s, tom), out value);
		}
		Dictionary<string, Day> value2 = cache.Where<KeyValuePair<string, Day>>((KeyValuePair<string, Day> kv) => kv.Key == CK(s, now) || kv.Key == CK(s, tom)).ToDictionary((KeyValuePair<string, Day> kv) => kv.Key, (KeyValuePair<string, Day> kv) => kv.Value);
		try
		{
			Directory.CreateDirectory(Dir);
			File.WriteAllText(CacheFile, JsonSerializer.Serialize(value2));
		}
		catch
		{
		}
		return new Result(today, value, off);
	}

	public static DateTime Now(string? tz)
	{
		if (string.IsNullOrEmpty(tz))
		{
			return DateTime.Now;
		}
		try
		{
			return TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(tz));
		}
		catch
		{
			return DateTime.Now;
		}
	}

	public static DateTime At(DateTime day, string hm, int add = 0)
	{
		string[] array = hm.Split(':');
		return day.Date.AddDays(add).AddHours(int.Parse(array[0])).AddMinutes(int.Parse(array[1]));
	}

	public static string F12(string hm)
	{
		return At(DateTime.Today, hm).ToString("h:mm tt", CultureInfo.InvariantCulture);
	}

	public static (int idx, DateTime time, bool tomorrow)? Next(DateTime now, Dictionary<string, string> today, Dictionary<string, string>? tomorrow)
	{
		for (int i = 0; i < Keys.Length; i++)
		{
			DateTime dateTime = At(now, today[Keys[i]]);
			if (dateTime > now)
			{
				return (i, dateTime, false);
			}
		}
		if (tomorrow != null)
		{
			return (0, At(now, tomorrow["Fajr"], 1), true);
		}
		return null;
	}

	public static (string num, string unit) FmtDyn(TimeSpan s)
	{
		if (s < TimeSpan.Zero)
		{
			s = TimeSpan.Zero;
		}
		if (s.TotalHours >= 1.0)
		{
			return (num: $"{(int)s.TotalHours:00}:{s.Minutes:00}:{s.Seconds:00}", unit: L.T("ساعة"));
		}
		if (s.TotalMinutes >= 1.0)
		{
			return (num: $"{s.Minutes:00}:{s.Seconds:00}", unit: L.T("دقيقة"));
		}
		return (num: $"{s.Seconds:00}", unit: L.T("ثانية"));
	}

	public static Disp? State(DateTime now, Dictionary<string, string> today, Dictionary<string, string>? tomorrow, Settings s)
	{
		TimeSpan timeSpan = TimeSpan.FromMinutes(1.0);
		for (int i = 0; i < Keys.Length; i++)
		{
			if (i == 1)
			{
				continue;
			}
			DateTime dateTime = At(now, today[Keys[i]]);
			TimeSpan timeSpan2 = now - dateTime;
			if (!(timeSpan2 < TimeSpan.Zero))
			{
				if (timeSpan2 < timeSpan)
				{
					return new Disp(Phase.Adhan, i, dateTime, Tomorrow: false, timeSpan - timeSpan2);
				}
				DateTime dateTime2 = dateTime.AddMinutes(s.IqamaFor(i));
				if (now < dateTime2)
				{
					return new Disp(Phase.Iqama, i, dateTime2, Tomorrow: false, dateTime2 - now);
				}
				if (now - dateTime2 < timeSpan)
				{
					return new Disp(Phase.IqamaNow, i, dateTime2, Tomorrow: false, TimeSpan.Zero);
				}
			}
		}
		(int, DateTime, bool)? tuple = Next(now, today, tomorrow);
		if (tuple.HasValue)
		{
			return new Disp(Phase.Next, tuple.Value.Item1, tuple.Value.Item2, tuple.Value.Item3, tuple.Value.Item2 - now);
		}
		return null;
	}

	public static string FmtHM(TimeSpan s)
	{
		if (s < TimeSpan.Zero)
		{
			s = TimeSpan.Zero;
		}
		int num = (int)Math.Ceiling(s.TotalMinutes);
		return $"{num / 60:00}:{num % 60:00}";
	}

	public static string FmtMinutes(TimeSpan s)
	{
		return Math.Max(0, (int)Math.Ceiling(s.TotalMinutes)).ToString(CultureInfo.InvariantCulture);
	}

	public static string FmtMS(TimeSpan s)
	{
		if (s < TimeSpan.Zero)
		{
			s = TimeSpan.Zero;
		}
		int num = (int)Math.Ceiling(s.TotalSeconds);
		return $"{num / 60:00}:{num % 60:00}";
	}

	public static string Fmt(TimeSpan s)
	{
		if (s < TimeSpan.Zero)
		{
			s = TimeSpan.Zero;
		}
		return $"{(int)s.TotalHours:00}:{s.Minutes:00}:{s.Seconds:00}";
	}

	public static async Task<List<Place>> Search(string q, string? cc = null)
	{
		string requestUri = "https://geocoding-api.open-meteo.com/v1/search?name=" + Uri.EscapeDataString(q) + "&count=15&language=ar&format=json" + (string.IsNullOrEmpty(cc) ? "" : ("&countryCode=" + cc));
		using JsonDocument jsonDocument = JsonDocument.Parse(await Http.GetStringAsync(requestUri));
		List<Place> list = new List<Place>();
		if (jsonDocument.RootElement.TryGetProperty("results", out var value))
		{
			foreach (JsonElement item in value.EnumerateArray())
			{
				string text = item.GetProperty("name").GetString() ?? "";
				JsonElement value2;
				string text2 = (item.TryGetProperty("admin1", out value2) ? (value2.GetString() ?? "") : "");
				JsonElement value3;
				string text3 = (item.TryGetProperty("country", out value3) ? (value3.GetString() ?? "") : "");
				JsonElement value4;
				string text4 = (item.TryGetProperty("country_code", out value4) ? (value4.GetString() ?? "") : "");
				string label = text + ((text2 == "" || text2 == text) ? "" : (" - " + text2)) + ((text3 == "") ? "" : (" - " + text3));
				list.Add(new Place(label, item.GetProperty("latitude").GetDouble(), item.GetProperty("longitude").GetDouble(), text, text3, text4.ToUpperInvariant()));
			}
		}
		return list;
	}

	public static async Task<(string city, string country, string cc)?> Reverse(double lat, double lon)
	{
		try
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			string requestUri = $"https://api.bigdatacloud.net/data/reverse-geocode-client?latitude={lat.ToString(invariantCulture)}&longitude={lon.ToString(invariantCulture)}&localityLanguage=ar";
			using JsonDocument jsonDocument = JsonDocument.Parse(await Http.GetStringAsync(requestUri));
			JsonElement r = jsonDocument.RootElement;
			string text = Get("city");
			if (text == "")
			{
				text = Get("locality");
			}
			if (text == "")
			{
				text = Get("principalSubdivision");
			}
			string text2 = Get("countryName");
			string item = Get("countryCode").ToUpperInvariant();
			if (text == "" && text2 == "")
			{
				return null;
			}
			return (text, text2, item);
			string Get(string k)
			{
				if (!r.TryGetProperty(k, out var value) || value.ValueKind != JsonValueKind.String)
				{
					return "";
				}
				return value.GetString() ?? "";
			}
		}
		catch
		{
			return null;
		}
	}

    private static async Task<LocationDetection.Fix?> NativeLocation(System.Threading.CancellationToken cancel)
    {
        // Called from the foreground UI thread so Windows can show its native permission prompt.
        var access = await Windows.Devices.Geolocation.Geolocator.RequestAccessAsync().AsTask(cancel);
        if (access != Windows.Devices.Geolocation.GeolocationAccessStatus.Allowed) return null;
        var locator = new Windows.Devices.Geolocation.Geolocator
        {
            DesiredAccuracy = Windows.Devices.Geolocation.PositionAccuracy.Default
        };
        var position = await locator.GetGeopositionAsync(TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(8)).AsTask(cancel);
        var point = position.Coordinate.Point.Position;
        return new LocationDetection.Fix(point.Latitude, point.Longitude);
    }

    public static async Task<(Place? p, string how)> Detect()
    {
        var fix = await LocationDetection.DetectAsync(NativeLocation, Http);
        if (fix == null) return (null, "");
        var how = L.T(fix.Source == "windows" ? "نظام الموقع في ويندوز" : "عنوان IP (تقريبي)");
        // IP responses already include a city/country. Reverse lookup is optional, never a
        // prerequisite for using valid coordinates, and must not block detection indefinitely.
        var city = fix.City;
        var country = fix.Country;
        var cc = fix.Cc;
        if (city == "" || cc == "")
        {
            try
            {
                var name = await Reverse(fix.Lat, fix.Lon).WaitAsync(TimeSpan.FromSeconds(4));
                if (name.HasValue) (city, country, cc) = name.Value;
            }
            catch (Exception e) { LocationDetection.Log("reverse", e.GetType().Name); }
        }
        if (city == "") city = L.T("موقعك الحالي");
        return (new Place(city, fix.Lat, fix.Lon, city, country, cc), how);
    }
}
