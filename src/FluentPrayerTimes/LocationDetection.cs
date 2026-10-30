using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FluentPrayerTimes;

// No coordinates, IP addresses or response bodies are written to the diagnostic log.
internal static class LocationDetection
{
    internal record Fix(double Lat, double Lon, string City = "", string Country = "", string Cc = "", string Source = "windows");

    internal static bool Valid(double lat, double lon) => double.IsFinite(lat) && double.IsFinite(lon)
        && lat >= -90 && lat <= 90 && lon >= -180 && lon <= 180;

    internal static void Log(string stage, string outcome)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FluentPrayerTimes");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "location.log");
            if (File.Exists(path) && new FileInfo(path).Length > 65536) File.WriteAllText(path, "");
            File.AppendAllText(path, $"{DateTimeOffset.UtcNow:O} {stage}: {outcome}\n");
        }
        catch { }
    }

    internal static Fix? Parse(string body, string provider)
    {
        using var doc = JsonDocument.Parse(body);
        var r = doc.RootElement;
        string S(string key) => r.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
        double lat, lon;
        if (provider == "ipwho")
        {
            if (!r.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True) return null;
            if (!r.TryGetProperty("latitude", out var la) || la.ValueKind != JsonValueKind.Number || !la.TryGetDouble(out lat)
                || !r.TryGetProperty("longitude", out var lo) || lo.ValueKind != JsonValueKind.Number || !lo.TryGetDouble(out lon)) return null;
        }
        else
        {
            var parts = S("loc").Split(',');
            if (parts.Length != 2 || !double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lat)
                || !double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lon)) return null;
        }
        return Valid(lat, lon) ? new Fix(lat, lon, S("city"), S("country"), S(provider == "ipwho" ? "country_code" : "country").ToUpperInvariant(), "ip") : null;
    }

    internal static async Task<Fix?> DetectAsync(Func<CancellationToken, Task<Fix?>> native, HttpClient http,
        Action<string, string>? log = null, TimeSpan? nativeTimeout = null, TimeSpan? httpTimeout = null)
    {
        log ??= Log;
        using (var cancel = new CancellationTokenSource(nativeTimeout ?? TimeSpan.FromSeconds(10)))
        {
            try
            {
                // WaitAsync bounds the operation even when a provider ignores cancellation.
                var fix = await native(cancel.Token).WaitAsync(cancel.Token);
                if (fix != null && Valid(fix.Lat, fix.Lon)) { log("windows", "ok"); return fix; }
                log("windows", "unavailable");
            }
            catch (Exception e) { log("windows", e.GetType().Name); }
        }
        foreach (var provider in new[] { ("ipwho", "https://ipwho.is/"), ("ipinfo", "https://ipinfo.io/json") })
        {
            using var cancel = new CancellationTokenSource(httpTimeout ?? TimeSpan.FromSeconds(5));
            try
            {
                var body = await http.GetStringAsync(provider.Item2, cancel.Token).WaitAsync(cancel.Token);
                var fix = Parse(body, provider.Item1);
                if (fix != null) { log(provider.Item1, "ok-approximate"); return fix; }
                log(provider.Item1, "invalid-response");
            }
            catch (Exception e) { log(provider.Item1, e.GetType().Name); }
        }
        return null;
    }
}
