using System;
using System.Collections.Generic;

namespace FluentPrayerTimes;

public static class SolarCalc
{
	private static readonly Dictionary<int, (double Fajr, double Isha)> Angles = new Dictionary<int, (double, double)>
	{
		[1] = (18.0, 18.0),
		[2] = (15.0, 15.0),
		[3] = (18.0, 17.0),
		[5] = (19.5, 17.5),
		[9] = (18.0, 17.5)
	};

	public static bool Supports(int method)
	{
		return Angles.ContainsKey(method);
	}

	private static double Rad(double d)
	{
		return d * Math.PI / 180.0;
	}

	private static double Deg(double r)
	{
		return r * 180.0 / Math.PI;
	}

	private static double Fix(double a)
	{
		a %= 360.0;
		if (!(a < 0.0))
		{
			return a;
		}
		return a + 360.0;
	}

	public static Dictionary<string, string>? Compute(DateTime date, double lat, double lon, double utcOffsetHours, int method, double? customFajr, double? customIsha, int asrFactor, int highLat = 0)
	{
		double ang;
		double ang2;
		if (method == 99)
		{
			if (!customFajr.HasValue || !customIsha.HasValue)
			{
				return null;
			}
			ang = customFajr.Value;
			ang2 = customIsha.Value;
		}
		else
		{
			if (!Angles.TryGetValue(method, out (double, double) value))
			{
				return null;
			}
			(ang, ang2) = value;
		}
		if (asrFactor < 1 || asrFactor > 2)
		{
			return null;
		}
		int year = date.Year;
		int month = date.Month;
		int day = date.Day;
		double num = (double)year * 367.0 - Math.Floor(((double)year + Math.Floor(((double)month + 9.0) / 12.0)) * 7.0 / 4.0) + Math.Floor((double)month * 275.0 / 9.0) + (double)day - 730531.5;
		double num2 = Fix(0.98560028 * num + 357.529);
		double num3 = Fix(0.98564736 * num + 280.459);
		double d = Fix(Math.Sin(Rad(num2)) * 1.915 + num3 + Math.Sin(Rad(2.0 * num2)) * 0.02);
		double d2 = 23.439 - 3.6E-07 * num;
		double num4 = Fix(Deg(Math.Atan2(Math.Cos(Rad(d2)) * Math.Sin(Rad(d)), Math.Cos(Rad(d))))) / 15.0;
		double decl = Deg(Math.Asin(Math.Sin(Rad(d2)) * Math.Sin(Rad(d))));
		double num5 = num3 / 15.0 - num4;
		double num6 = (utcOffsetHours * 15.0 - lon) / 15.0 + 12.0 - num5;
		double? num7 = Below(0.833);
		double? num8 = Above(Deg(Math.Atan(1.0 / (Math.Tan(Rad(Math.Abs(lat - decl))) + (double)asrFactor))));
		if (!num7.HasValue || !num8.HasValue)
		{
			return null;
		}
		double? num9 = Below(ang);
		double? num10 = Below(ang2);
		double num11 = num6 - num7.Value / 15.0;
		double num12 = num6 + num7.Value / 15.0;
		double num13 = 24.0 - (num12 - num11);
		double h;
		if (num9.HasValue)
		{
			h = num6 - num9.Value / 15.0;
		}
		else
		{
			if (highLat <= 0)
			{
				return null;
			}
			h = num11 - num13 * Portion(ang);
		}
		double h2;
		if (num10.HasValue)
		{
			h2 = num6 + num10.Value / 15.0;
		}
		else
		{
			if (highLat <= 0)
			{
				return null;
			}
			h2 = num12 + num13 * Portion(ang2);
		}
		return new Dictionary<string, string>
		{
			["Fajr"] = Near(Secs(h)),
			["Sunrise"] = Down(Secs(num11)),
			["Dhuhr"] = Near(Secs(num6)),
			["Asr"] = Near(Secs(num8.Value / 15.0 + num6)),
			["Maghrib"] = Near(Secs(num12)),
			["Isha"] = Near(Secs(h2))
		};
		double? Above(double alt)
		{
			double num14 = (Math.Sin(Rad(alt)) - Math.Sin(Rad(lat)) * Math.Sin(Rad(decl))) / (Math.Cos(Rad(lat)) * Math.Cos(Rad(decl)));
			if (!(num14 < -1.0) && !(num14 > 1.0))
			{
				return Deg(Math.Acos(num14));
			}
			return null;
		}
		double? Below(double d3)
		{
			double num14 = (0.0 - Math.Sin(Rad(d3)) - Math.Sin(Rad(lat)) * Math.Sin(Rad(decl))) / (Math.Cos(Rad(lat)) * Math.Cos(Rad(decl)));
			if (!(num14 < -1.0) && !(num14 > 1.0))
			{
				return Deg(Math.Acos(num14));
			}
			return null;
		}
		static string Down(int secs)
		{
			return Hm((int)Math.Floor((double)secs / 60.0));
		}
		static string Hm(int totalMinutes)
		{
			int num14 = (totalMinutes % 1440 + 1440) % 1440;
			return $"{num14 / 60:00}:{num14 % 60:00}";
		}
		static string Near(int secs)
		{
			return Hm((int)Math.Floor((double)(secs + 30) / 60.0));
		}
		double Portion(double num14)
		{
			if (highLat != 1)
			{
				if (highLat != 2)
				{
					return num14 / 60.0;
				}
				return 1.0 / 7.0;
			}
			return 0.5;
		}
		static int Secs(double num15)
		{
			int num14 = (int)Math.Floor(num15);
			double num16 = (num15 - (double)num14) * 60.0;
			int num17 = (int)Math.Floor(num16);
			int num18 = (int)Math.Floor((num16 - (double)num17) * 60.0);
			return num14 * 3600 + num17 * 60 + num18;
		}
	}
}
