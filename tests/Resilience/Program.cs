using FluentPrayerTimes;
using System.Security.Cryptography;
using System.Text.Json;

int checks = 0;
void Check(string name, bool valid) { checks++; Console.WriteLine((valid ? "PASS " : "FAIL ") + name); if (!valid) throw new Exception(name); }
var clock = new Clock();
var retry = new ReloadPolicy(clock);
Check("first load due", retry.Due);
Check("one reload begins", retry.Begin());
Check("overlap blocked", !retry.Begin() && !retry.Due);
retry.Finish(false);
clock.Advance(9); Check("failed retry waits", !retry.Due);
clock.Advance(2); Check("failed retry eventually due", retry.Due);
for (int i = 0; i < 8; i++) { retry.Begin(); retry.Finish(false); clock.Advance(301); Check("bounded backoff " + i, retry.Due); }
// A wall-clock/selected-city change cannot affect a monotonic timestamp.
clock.Wall = DateTimeOffset.UtcNow.AddHours(7); retry.Begin(); retry.Finish(false); Check("east timezone no immediate retry", !retry.Due);
clock.Wall = clock.Wall.AddHours(-14); clock.Advance(301); Check("west timezone cannot suppress retry", retry.Due);

var temp = Path.Combine(Path.GetTempPath(), "fpt-regression-" + Guid.NewGuid()); Directory.CreateDirectory(temp);
try
{
    string file = Path.Combine(temp, "settings.json");
    Storage.Write(file, "{\"Lang\":\"ar\",\"IqamaMin\":[25,0,30,20,15,20]}");
    Storage.Write(file, "{\"Lang\":\"en\"}");
    Check("atomic save replacement", Storage.Read<Settings>(file)?.Lang == "en");
    File.WriteAllText(file, "{broken");
    var recovered = Storage.Read<Settings>(file);
    Check("truncated settings recover backup", recovered?.Lang == "ar" && recovered.IqamaFor(2) == 30);
    Check("corrupt original preserved", Directory.GetFiles(temp, "settings.json.corrupt-*").Length == 1);
    Check("missing settings require first-run detection", Settings.Load().Location != null);
    string installer = Path.Combine(temp, "setup.exe");
    byte[] bytes = new byte[128]; bytes[0] = 77; bytes[1] = 90; File.WriteAllBytes(installer, bytes);
    var asset = new UpdateAsset("https://github.com/Star-Dust-0/fluent-prayer-times/releases/download/v1.3.8/setup.exe", "setup.exe", bytes.Length, Convert.ToHexString(SHA256.HashData(bytes)));
    UpdateInstaller.Verify(installer, asset); Check("valid checksum accepted", true);
    foreach (string? hash in new string?[] { null, "", "xyz", new string('0', 64) })
    {
        bool rejected = false; try { UpdateInstaller.Verify(installer, asset with { Sha256 = hash }); } catch (InvalidDataException) { rejected = true; }
        Check("missing/invalid/mismatched checksum rejected " + hash?.Length, rejected);
    }
    string Release(string assets) => "{\"draft\":false,\"prerelease\":false,\"tag_name\":\"v1.3.8\",\"assets\":" + assets + "}";
    using var empty = JsonDocument.Parse(Release("[]"));
    Check("incomplete release not an exception", UpdateCatalog.Select(empty.RootElement, new Version(1,3,7)) == null);
    string ReleaseAsset(string digest) => "[{\"name\":\"FluentPrayerTimes-v1.3.8-installer-win-x64.exe\",\"size\":128,\"browser_download_url\":\"https://github.com/Star-Dust-0/fluent-prayer-times/releases/download/v1.3.8/setup.exe\"" + digest + "}]";
    using var noDigest = JsonDocument.Parse(Release(ReleaseAsset("")));
    Check("digestless release refused", UpdateCatalog.Select(noDigest.RootElement, new Version(1,3,7)) == null);
    using var good = JsonDocument.Parse(Release(ReleaseAsset(",\"digest\":\"sha256:" + asset.Sha256 + "\"")));
    Check("digest-bearing release selected", UpdateCatalog.Select(good.RootElement, new Version(1,3,7))?.Sha256 == asset.Sha256);
}
finally { Directory.Delete(temp, true); }
foreach (int offset in new[] { -7, -2, 2, 3, 7 })
{
    string zone = "Etc/GMT" + (offset >= 0 ? "-" : "+") + Math.Abs(offset);
    var resolved = Times.ResolveZone(zone)!;
    Check("fixed zone " + zone, resolved.BaseUtcOffset == TimeSpan.FromHours(offset));
    Check("zone resolution cached " + zone, ReferenceEquals(resolved, Times.ResolveZone(zone)));
}
var settings = new Settings { CalculationMethod = 5, Location = new Loc { Tz = "Africa/Cairo" } };
var date = new DateTime(2026, 11, 13);
var local = Times.LocalDay(settings, date, null)!;
Check("offline Egypt has all six times", local.T.Count == 6);
Check("offline preserves accepted formula", local.T.SequenceEqual(SolarCalc.Compute(date, settings.Location.Lat, settings.Location.Lon, 2, 5, null, null, 1, 3)!));
Check("unsupported local method not invented", Times.LocalDay(new Settings { CalculationMethod = 4, Location = settings.Location }, date, null) == null);
Check("unknown timezone not device timezone", Times.LocalDay(new Settings { CalculationMethod = 5, Location = new Loc { Tz = "Missing/Zone" } }, date, null) == null);
settings.CalculationMethod = -1;
Check("auto method retained offline", Times.LocalDay(settings, date, local)?.Method == 5);
var times = new Dictionary<string,string> { ["Fajr"]="04:00", ["Sunrise"]="05:30", ["Dhuhr"]="12:00", ["Asr"]="15:00", ["Maghrib"]="18:00", ["Isha"]="19:00" };
settings.IqamaMin = new[] {20,0,20,20,180,20};
Check("long Maghrib iqama never hides Isha", Times.State(date.AddHours(19), times, times, settings)?.Phase == Times.Phase.Adhan && Times.State(date.AddHours(19), times, times, settings)?.Idx == 5);
var late = new Dictionary<string,string>(times) { ["Isha"]="23:50" };
Check("late Isha iqama survives midnight", Times.State(date.AddDays(1).AddMinutes(2), times, times, settings, late) is { Phase:Times.Phase.Iqama, Idx:5 });
Check("no tomorrow data means no invented next time", Times.Next(date.AddHours(23), times, null) == null);
Console.WriteLine($"{checks} resilience checks passed");

sealed class Clock : TimeProvider
{
    private long seconds;
    internal DateTimeOffset Wall = DateTimeOffset.UtcNow;
    public override long TimestampFrequency => 1;
    public override long GetTimestamp() => seconds;
    public override DateTimeOffset GetUtcNow() => Wall;
    internal void Advance(long value) => seconds += value;
}
