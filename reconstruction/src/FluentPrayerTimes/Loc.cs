using System.Text.RegularExpressions;

namespace FluentPrayerTimes;

public class Loc
{
	public string Name { get; set; } = "Alexandria";

	public string NameAr { get; set; } = "الإسكندرية";

	public double Lat { get; set; } = 31.2001;

	public double Lon { get; set; } = 29.9187;

	public bool UseCity { get; set; } = true;

	public string CountryAr { get; set; } = "مصر";

	public string Cc { get; set; } = "EG";

	public string? Tz { get; set; }

	public string Display
	{
		get
		{
			string text = L.T(NameAr);
			if (!L.IsArabic && !UseCity && Regex.IsMatch(text, "[\u0600-ۿ]"))
			{
				text = "";
			}
			string text2 = (string.IsNullOrEmpty(Cc) ? L.T(CountryAr) : L.CountryName(Cc, CountryAr));
			if (!(text == ""))
			{
				if (!string.IsNullOrEmpty(text2))
				{
					return text + L.T("، ") + text2;
				}
				return text;
			}
			return text2;
		}
	}

	public string Key
	{
		get
		{
			if (!UseCity)
			{
				return $"g:{Lat:F3},{Lon:F3}";
			}
			return "c:" + Name;
		}
	}
}
