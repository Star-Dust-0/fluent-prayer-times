using System.Collections.Generic;

namespace FluentPrayerTimes;

public record Day(Dictionary<string, string> T, string Hijri, string? Tz = null, int Method = -1, int School = 0, int HighLatitude = 3);
