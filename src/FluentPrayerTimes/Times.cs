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
	public record Result(Day? Today, Day? Tomorrow, bool Offline, Day? Yesterday = null);

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

	public static readonly HttpClient Http = Network.Http;

	private static readonly Regex Diac = new Regex("[\u064b-\u0652\u0670]");

    private static string? _namesLanguage;
    private static string[] _names = Array.Empty<string>();
    public static string[] Names
    {
        get
        {
            if (_namesLanguage != L.Lang)
            {
                _names = new[] { L.T("الفجر"), L.T("الشروق"), L.T("الظهر"), L.T("العصر"), L.T("المغرب"), L.T("العشاء") };
                _namesLanguage = L.Lang;
            }
            return _names;
        }
    }

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

    private static Dictionary<string, Day> Read() => Storage.Read<Dictionary<string, Day>>(CacheFile) ?? new();

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

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, TimeZoneInfo> Zones = new();
    internal static TimeZoneInfo? ResolveZone(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (Zones.TryGetValue(name, out var cached)) return cached;
        try
        {
            var fixedOffset = Regex.Match(name, "^Etc/GMT([+-])(\\d{1,2})$");
            TimeZoneInfo zone;
            if (fixedOffset.Success)
            {
                int hours = int.Parse(fixedOffset.Groups[2].Value, CultureInfo.InvariantCulture);
                if (hours > 14) return null;
                if (fixedOffset.Groups[1].Value == "+") hours = -hours;
                zone = TimeZoneInfo.CreateCustomTimeZone(name, TimeSpan.FromHours(hours), name, name);
            }
            else zone = TimeZoneInfo.FindSystemTimeZoneById(name);
            return Zones.GetOrAdd(name, zone);
        }
        catch (TimeZoneNotFoundException) { Log.Write("timezone", "Unknown zone"); return null; }
        catch (InvalidTimeZoneException e) { Log.Error("timezone", e); return null; }
    }

    internal static double? OffsetHours(Settings s, string? zone, DateTime d) =>
        ResolveZone(s.TzParam ?? zone)?.GetUtcOffset(new DateTime(d.Year, d.Month, d.Day, 12, 0, 0)).TotalHours;

    internal static Day? LocalDay(Settings s, DateTime date, Day? lastKnown)
    {
        int method = s.CalculationMethod >= 0 ? s.CalculationMethod : lastKnown?.Method ?? (s.Location.Cc == "EG" ? 5 : -1);
        var zone = s.TzParam ?? s.Location.Tz ?? lastKnown?.Tz;
        double? offset = OffsetHours(s, zone, date);
        if (method < 0 || !offset.HasValue) return null;
        int school = s.AsrSchool >= 0 ? s.AsrSchool : lastKnown?.School ?? 0;
        int highLatitude = s.HighLatitude >= 0 ? s.HighLatitude : lastKnown?.HighLatitude ?? 3;
        var times = SolarCalc.Compute(date, s.Location.Lat, s.Location.Lon, offset.Value, method,
            method == 99 ? s.FajrAngle : null, method == 99 ? s.IshaAngle : null, school == 1 ? 2 : 1, highLatitude);
        if (times == null) return null;
        if (method == 99)
        {
            if (s.CustomRule == 2 || s.CustomRule == 3) times["Fajr"] = At(date, times["Sunrise"]).AddMinutes(-s.FajrMinutes).ToString("HH:mm", CultureInfo.InvariantCulture);
            if (s.CustomRule == 1 || s.CustomRule == 2) times["Isha"] = At(date, times["Maghrib"]).AddMinutes(s.IshaMinutes).ToString("HH:mm", CultureInfo.InvariantCulture);
        }
        var calendar = new UmAlQuraCalendar();
        string[] months = { "محرم", "صفر", "ربيع الأول", "ربيع الآخر", "جمادى الأولى", "جمادى الآخرة", "رجب", "شعبان", "رمضان", "شوال", "ذو القعدة", "ذو الحجة" };
        string hijri = calendar.GetDayOfMonth(date) + " " + months[calendar.GetMonth(date) - 1] + " " + calendar.GetYear(date) + " هـ";
        return new Day(times, hijri, zone, method, school, highLatitude, Local: true);
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
        var cache = Read();
        DateTime date = Now(s.TzParam ?? s.Location.Tz).Date;
        string prefix = CK(s, date)[..^10];
        Day? lastKnown = cache.Where(item => item.Key.StartsWith(prefix, StringComparison.Ordinal)).OrderByDescending(item => item.Key).Select(item => item.Value).FirstOrDefault();
        bool offline = false;
        async Task<Day?> DayFor(DateTime requested)
        {
            try { var loaded = await Get(s, requested, cache); offline |= loaded.Local; return loaded; }
            catch (Exception e)
            {
                offline = true;
                Log.Write("times", "Network lookup failed: " + e.GetType().Name);
                var local = LocalDay(s, requested, lastKnown);
                if (local != null) cache[CK(s, requested)] = local;
                return local;
            }
        }
        Day? today = await DayFor(date);
        if (today?.Tz != null && today.Tz != s.Location.Tz)
        {
            s.Location.Tz = today.Tz;
            DateTime resolvedDate = Now(s.TzParam ?? s.Location.Tz).Date;
            if (date != resolvedDate) { date = resolvedDate; today = await DayFor(date); }
        }
        lastKnown = today ?? lastKnown;
        Day? tomorrow = await DayFor(date.AddDays(1));
        cache.TryGetValue(CK(s, date.AddDays(-1)), out var yesterday);
        yesterday ??= LocalDay(s, date.AddDays(-1), lastKnown);
        // Keep a week ahead for local methods without issuing extra network requests.
        for (int ahead = 2; ahead <= 7; ahead++)
        {
            DateTime future = date.AddDays(ahead);
            if (!cache.ContainsKey(CK(s, future)) && LocalDay(s, future, lastKnown) is Day computed) cache[CK(s, future)] = computed;
        }
        // Retain a week of history and a week ahead, including yesterday's late iqama.
        var retained = cache.Where(item => item.Key.StartsWith(prefix, StringComparison.Ordinal) &&
            string.CompareOrdinal(item.Key[^10..], date.AddDays(-7).ToString("yyyy-MM-dd")) >= 0 &&
            string.CompareOrdinal(item.Key[^10..], date.AddDays(7).ToString("yyyy-MM-dd")) <= 0).ToDictionary(item => item.Key, item => item.Value);
        try { Storage.Write(CacheFile, JsonSerializer.Serialize(retained)); }
        catch (Exception e) { Log.Error("cache-save", e); }
        return new Result(today, tomorrow, offline, yesterday);
    }

    public static DateTime Now(string? tz) => ResolveZone(tz) is TimeZoneInfo zone ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone) : DateTime.Now;

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, TimeSpan> ParsedTimes = new();
    public static DateTime At(DateTime day, string hm, int add = 0)
    {
        var time = ParsedTimes.GetOrAdd(hm, value => TimeSpan.ParseExact(value, @"hh\:mm", CultureInfo.InvariantCulture));
        return day.Date.AddDays(add).Add(time);
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

    internal static DateTime FollowingPrayer(DateTime date, Dictionary<string, string> today, Dictionary<string, string>? tomorrow, int index)
    {
        if (index < 5) return At(date, today[Keys[index == 0 ? 2 : index + 1]]);
        return tomorrow != null ? At(date.AddDays(1), tomorrow[Keys[0]]) : DateTime.MaxValue;
    }

    public static Disp? State(DateTime now, Dictionary<string, string> today, Dictionary<string, string>? tomorrow, Settings s, Dictionary<string, string>? yesterday = null)
    {
        // Newer prayer takes priority over any unusually long preceding iqama.
        foreach (var source in new[] { (day: now.Date, times: today), (day: now.Date.AddDays(-1), times: yesterday) })
        {
            if (source.times == null) continue;
            for (int i = Keys.Length - 1; i >= 0; i--)
            {
                if (i == 1) continue;
                DateTime adhan = At(source.day, source.times[Keys[i]]);
                if (now < adhan) continue;
                DateTime nextPrayer = DateTime.MaxValue;
                if (i < 5) nextPrayer = At(source.day, source.times[Keys[i == 0 ? 2 : i + 1]]);
                else if (source.day < now.Date) nextPrayer = At(now, today[Keys[0]]);
                else if (tomorrow != null) nextPrayer = At(now.AddDays(1), tomorrow[Keys[0]]);
                if (now >= nextPrayer) break;
                if (now < adhan.AddMinutes(1)) return new Disp(Phase.Adhan, i, adhan, false, adhan.AddMinutes(1) - now);
                DateTime iqama = adhan.AddMinutes(s.IqamaFor(i));
                if (iqama >= nextPrayer) iqama = nextPrayer.AddTicks(-1);
                if (now < iqama) return new Disp(Phase.Iqama, i, iqama, false, iqama - now);
                if (now < iqama.AddMinutes(1)) return new Disp(Phase.IqamaNow, i, iqama, false, TimeSpan.Zero);
                break;
            }
        }
        var next = Next(now, today, tomorrow);
        return next.HasValue ? new Disp(Phase.Next, next.Value.idx, next.Value.time, next.Value.tomorrow, next.Value.time - now) : null;
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
		string requestUri = "https://geocoding-api.open-meteo.com/v1/search?name=" + Uri.EscapeDataString(q) + "&count=15&language=" + Uri.EscapeDataString(L.Lang) + "&format=json" + (string.IsNullOrEmpty(cc) ? "" : ("&countryCode=" + cc));
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
			string requestUri = $"https://api.bigdatacloud.net/data/reverse-geocode-client?latitude={lat.ToString(invariantCulture)}&longitude={lon.ToString(invariantCulture)}&localityLanguage={L.Lang}";
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
