using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FluentPrayerTimes;

internal static class SelfTest
{
	public static void Run()
	{
		List<string> log = new List<string>();
		int fails = 0;
		try
		{
			Settings s = new Settings();
			Dictionary<string, string> t = new Dictionary<string, string>
			{
				["Fajr"] = "04:00",
				["Sunrise"] = "05:30",
				["Dhuhr"] = "12:00",
				["Asr"] = "15:30",
				["Maghrib"] = "18:00",
				["Isha"] = "19:30"
			};
			Dictionary<string, string> tom = new Dictionary<string, string>(t);
			DateTime day = new DateTime(2026, 10, 9);
			Check("defaults Fajr20 Dhuhr20 Asr20 Maghrib15 Isha20", s.IqamaFor(0) == 20 && s.IqamaFor(2) == 20 && s.IqamaFor(3) == 20 && s.IqamaFor(4) == 15 && s.IqamaFor(5) == 20);
			Check("before Dhuhr: next, Dhuhr", D("11:00").Phase == Times.Phase.Next && D("11:00").Idx == 2);
			Check("11:00 -> 01:00", Times.FmtHM(D("11:00").Left) == "01:00");
			Check("11:59:30 -> 00:01 (no seconds, rounds up)", Times.FmtHM(D("11:59", 30).Left) == "00:01");
			Check("12:00:00 adhan", D("12:00").Phase == Times.Phase.Adhan);
			Check("12:00:59 adhan", D("12:00", 59).Phase == Times.Phase.Adhan);
			Check("12:01:00 iqama countdown 19:00", D("12:01").Phase == Times.Phase.Iqama && Times.FmtMS(D("12:01").Left) == "19:00");
			Check("12:19:30 iqama 00:30", Times.FmtMS(D("12:19", 30).Left) == "00:30");
			Check("12:20:00 iqama reached", D("12:20").Phase == Times.Phase.IqamaNow);
			Check("12:20:59 iqama reached", D("12:20", 59).Phase == Times.Phase.IqamaNow);
			Check("12:21:00 back to next (Asr)", D("12:21").Phase == Times.Phase.Next && D("12:21").Idx == 3);
			Check("Maghrib uses 15 min", D("18:15").Phase == Times.Phase.IqamaNow && D("18:14").Phase == Times.Phase.Iqama);
			Check("Sunrise has no adhan/iqama", D("05:30").Phase == Times.Phase.Next);
			Check("after Isha iqama -> tomorrow Fajr", D("20:00").Phase == Times.Phase.Next && D("20:00").Tomorrow && D("20:00").Idx == 0);
			s.IqamaMin = new int[6] { 30, 0, 25, 20, 10, 40 };
			Check("custom iqama per prayer", s.IqamaFor(0) == 30 && s.IqamaFor(2) == 25 && s.IqamaFor(4) == 10 && s.IqamaFor(5) == 40);
			Check("custom Dhuhr 25: 12:24 iqama, 12:25 now", D("12:24").Phase == Times.Phase.Iqama && D("12:25").Phase == Times.Phase.IqamaNow);
			Settings settings = JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(s));
			Check("settings round trip", settings != null && settings.IqamaFor(0) == 30 && settings.AdhanSound == "default");
			Check("old settings.json without new fields", JsonSerializer.Deserialize<Settings>("{\"NotifyAdhan\":true}").IqamaFor(4) == 15);
			Check("minutes only, partial minute rounds up", Times.FmtMinutes(TimeSpan.FromSeconds(30.0)) == "1" && Times.FmtMinutes(TimeSpan.FromMinutes(13.0)) == "13");
			Check("location method starts automatic", s.CalculationMethod == -1 && !Times.CalculationQuery(s).Contains("method="));
			string text = Times.CacheKey(s, day);
			s.CalculationMethod = 5;
			s.AsrSchool = 1;
			s.HighLatitude = 0;
			Check("calculation overrides transmitted", Times.CalculationQuery(s).Contains("method=5") && Times.CalculationQuery(s).Contains("school=1") && Times.CalculationQuery(s).Contains("latitudeAdjustmentMethod=0"));
			Check("method changes invalidate cache", text != Times.CacheKey(s, day));
			s.CalculationMethod = 99;
			s.FajrAngle = 19.5;
			s.IshaAngle = 17.5;
			Check("custom angles invariant", Times.CalculationQuery(s).Contains("methodSettings=19.5,null,17.5"));
			Check("custom settings round trip", JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(s)).FajrAngle == 19.5);
			Check("newer stable release gives installer url", MainWindow.UpdateDownload(Rel(Rj("v9.0.0", draft: false, pre: false, "FluentPrayerTimes-v9.0.0-installer-win-x64.exe")), new Version(1, 1, 1)) != null);
			Check("runtime-dependent installer accepted", MainWindow.UpdateDownload(Rel(Rj("v9.0.0", draft: false, pre: false, "FluentPrayerTimes-v9.0.0-installer-.NET-runtime-dependent-win-x64.exe")), new Version(1, 1, 1)) != null);
			Check("update found when GitHub reports canonical repo casing", MainWindow.UpdateDownload(Rel(Rc("v9.0.0", "FluentPrayerTimes-v9.0.0-installer-win-x64.exe")), new Version(1, 1, 1)) == "https://github.com/MohamedElnaggar00/Fluent-Prayer-Times/releases/download/v9.0.0/FluentPrayerTimes-v9.0.0-installer-win-x64.exe");
			bool ok = false;
			try
			{
				MainWindow.UpdateDownload(Rel("{\"draft\":false,\"prerelease\":false,\"tag_name\":\"v9.0.0\",\"assets\":[{\"name\":\"FluentPrayerTimes-v9.0.0-installer-win-x64.exe\",\"browser_download_url\":\"https://github.com/someone-else/other/releases/download/v9.0.0/FluentPrayerTimes-v9.0.0-installer-win-x64.exe\"}]}"), new Version(1, 1, 1));
			}
			catch (InvalidOperationException)
			{
				ok = true;
			}
			Check("foreign repository download url is still rejected", ok);
			UpdateAsset updateAsset = MainWindow.UpdateAssetFor(Rel("{\"draft\":false,\"prerelease\":false,\"tag_name\":\"v9.0.0\",\"assets\":[{\"name\":\"FluentPrayerTimes-v9.0.0-installer-win-x64.exe\",\"size\":1234,\"digest\":\"sha256:ABCDEF\",\"browser_download_url\":\"https://github.com/MohamedElnaggar00/Fluent-Prayer-Times/releases/download/v9.0.0/FluentPrayerTimes-v9.0.0-installer-win-x64.exe\"}]}"), new Version(1, 1, 1));
			Check("update asset carries size and sha256 digest", updateAsset != null && updateAsset.Size == 1234 && updateAsset.Sha256 == "ABCDEF");
			Check("download host: github.com and its asset hosts only", UpdateInstaller.IsTrustedSource(new Uri("https://github.com/a/b")) && UpdateInstaller.IsTrustedSource(new Uri("https://release-assets.githubusercontent.com/x")) && !UpdateInstaller.IsTrustedSource(new Uri("https://evil.example/x")) && !UpdateInstaller.IsTrustedSource(new Uri("http://github.com/a")) && !UpdateInstaller.IsTrustedSource(new Uri("https://githubusercontent.com.evil.example/x")));
			string tmp = Path.Combine(Path.GetTempPath(), "fpt-verify-" + Guid.NewGuid().ToString("N"));
			byte[] array = new byte[4096];
			array[0] = 77;
			array[1] = 90;
			File.WriteAllBytes(tmp, array);
			string text2 = Convert.ToHexString(SHA256.HashData(array));
			UpdateInstaller.Verify(tmp, new UpdateAsset("u", "n.exe", 4096L, text2.ToLowerInvariant()));
			Check("installer verification accepts matching size and checksum", true);
			Check("installer verification rejects wrong size", Fails(new UpdateAsset("u", "n.exe", 4097L, null)));
			Check("installer verification rejects wrong checksum", Fails(new UpdateAsset("u", "n.exe", 4096L, new string('0', 64))));
			byte[] array2 = (byte[])array.Clone();
			array2[0] = 0;
			File.WriteAllBytes(tmp, array2);
			Check("installer verification rejects non-executable file", Fails(new UpdateAsset("u", "n.exe", 4096L, null)));
			File.Delete(tmp);
			Check("same version gives none", MainWindow.UpdateDownload(Rel(Rj("v1.1.1", draft: false, pre: false, "FluentPrayerTimes-v1.1.1-installer-win-x64.exe")), new Version(1, 1, 1)) == null);
			Check("older gives none", MainWindow.UpdateDownload(Rel(Rj("v1.0.0", draft: false, pre: false, "FluentPrayerTimes-v1.0.0-installer-win-x64.exe")), new Version(1, 1, 1)) == null);
			Check("prerelease ignored", MainWindow.UpdateDownload(Rel(Rj("v9.0.0", draft: false, pre: true, "FluentPrayerTimes-v9.0.0-installer-win-x64.exe")), new Version(1, 1, 1)) == null);
			Check("draft ignored", MainWindow.UpdateDownload(Rel(Rj("v9.0.0", draft: true, pre: false, "FluentPrayerTimes-v9.0.0-installer-win-x64.exe")), new Version(1, 1, 1)) == null);
			Settings settings2 = new Settings();
			Settings settings3 = new Settings
			{
				Location = new Loc
				{
					Cc = "US"
				},
				Dst = "off"
			};
			Check("DST card only for Egypt", settings2.DstApplies() && !settings3.DstApplies() && settings3.TzParam == null);
			Check("local calc 23 Oct matches Wathkkir", Row(new DateTime(2026, 10, 23), 3) == "05:42 07:09 12:46 15:57 18:23 19:41");
			Check("local calc 24 Oct", Row(new DateTime(2026, 10, 24), 3) == "05:42 07:10 12:46 15:57 18:22 19:40");
			Check("local calc 26 Oct", Row(new DateTime(2026, 10, 26), 3) == "05:44 07:11 12:46 15:55 18:20 19:38");
			Settings settings4 = JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(new Settings
			{
				AdhanSound = "none",
				IqamaSound = "none"
			}));
			Check("no-sound choice persists", settings4.AdhanSound == "none" && settings4.IqamaSound == "none");
			Check("no-sound plays no audio file", !Sounds.Available("adhan", "none") && !Sounds.Available("iqama", "none"));
			Check("default sound setting unchanged", new Settings().AdhanSound == "default" && new Settings().IqamaSound == "default");
			Settings settings5 = JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(new Settings
			{
				AdhanNotify = false
			}));
			Check("adhan and iqama notifications are independent", !settings5.AdhanNotify && settings5.IqamaNotify && new Settings().AdhanNotify && new Settings().IqamaNotify);
			Settings settings6 = JsonSerializer.Deserialize<Settings>("{\"NotifyAdhan\":true}");
			Check("old settings.json keeps both notifications on and no location lookup", settings6 != null && settings6.AdhanNotify && settings6.IqamaNotify && !settings6.LocationPending);
			Settings settings7 = new Settings
			{
				AdhanNotify = false
			};
			Settings settings8 = new Settings
			{
				IqamaNotify = false
			};
			Settings settings9 = new Settings
			{
				NotifyAdhan = false
			};
			Settings settings10 = new Settings();
			Check("scheduler gates: adhan off keeps iqama", !settings7.AdhanOn && settings7.IqamaOn);
			Check("scheduler gates: iqama off keeps adhan", settings8.AdhanOn && !settings8.IqamaOn);
			Check("scheduler gates: master off stops both", !settings9.AdhanOn && !settings9.IqamaOn);
			Check("scheduler gates: defaults notify both", settings10.AdhanOn && settings10.IqamaOn);
			Check("new Settings() does not ask for a location lookup by itself", !new Settings().LocationPending);
			Check("local calc unsupported method falls back", Row(new DateTime(2026, 10, 23), 3, 7) == "null");
			Check("local calc Hanafi Asr (Cairo 23 Oct)", RowX(new DateTime(2026, 10, 23), 30.0444, 31.2357, 3, 5, 2, 3) == "05:35 07:02 12:39 16:39 18:17 19:35");
			Check("local calc high latitude angle rule (Oslo 21 Jun)", RowX(new DateTime(2026, 6, 21), 59.9139, 10.7522, 2, 3, 1, 3) == "02:21 03:53 13:19 18:00 22:44 00:12");
			Check("local calc high latitude middle of night", RowX(new DateTime(2026, 6, 21), 59.9139, 10.7522, 2, 3, 1, 1) == "01:19 03:53 13:19 18:00 22:44 01:19");
			Check("local calc high latitude one seventh", RowX(new DateTime(2026, 6, 21), 59.9139, 10.7522, 2, 3, 1, 2) == "03:09 03:53 13:19 18:00 22:44 23:28");
			Check("local calc no high-latitude rule falls back", RowX(new DateTime(2026, 6, 21), 59.9139, 10.7522, 2, 3, 1, 0) == "null");
			Check("local calc polar day falls back", RowX(new DateTime(2026, 6, 21), 69.65, 18.96, 2, 3, 1, 3) == "null");
			Check("local calc mid-latitude matches Aladhan (50N Dec)", RowX(new DateTime(2026, 12, 21), 50.0, 8.0, 1, 3, 1, 3) == "06:24 08:23 12:26 14:12 16:28 18:21");
			Settings s2 = new Settings
			{
				Location = new Loc
				{
					Cc = "US"
				},
				Dst = "off"
			};
			Check("offset Cairo before DST starts (23 Apr)", Times.OffsetHours(s2, "Africa/Cairo", new DateTime(2026, 4, 23)) == 2.0);
			Check("offset Cairo after DST starts (24 Apr)", Times.OffsetHours(s2, "Africa/Cairo", new DateTime(2026, 4, 24)) == 3.0);
			Check("offset Cairo last DST day (29 Oct)", Times.OffsetHours(s2, "Africa/Cairo", new DateTime(2026, 10, 29)) == 3.0);
			Check("offset Cairo after DST ends (30 Oct)", Times.OffsetHours(s2, "Africa/Cairo", new DateTime(2026, 10, 30)) == 2.0);
			Settings settings11 = new Settings
			{
				TimeZoneOverride = "Etc/GMT-3"
			};
			Check("manual UTC+3 overrides zone", Times.OffsetHours(settings11, "Africa/Cairo", new DateTime(2026, 12, 1)) == 3.0);
			settings11.TimeZoneOverride = "Etc/GMT+5";
			Check("manual UTC-5 offset", Times.OffsetHours(settings11, null, new DateTime(2026, 12, 1)) == -5.0);
			Check("DST transition changes local times by an hour", RowX(new DateTime(2026, 10, 30), 30.8619, 29.5845, 2, 5, 1, 3).StartsWith("04:") && RowX(new DateTime(2026, 10, 29), 30.8619, 29.5845, 3, 5, 1, 3).StartsWith("05:"));
			bool ok2 = false;
			try
			{
				MainWindow.UpdateDownload(Rel(Rj("v9.0.0", draft: false, pre: false, "other.zip")), new Version(1, 1, 1));
			}
			catch (InvalidOperationException)
			{
				ok2 = true;
			}
			Check("missing installer asset reports error", ok2);
			Times.Disp D(string hm, int sec = 0)
			{
				return Times.State(day.Date.AddHours(int.Parse(hm.Substring(0, 2))).AddMinutes(int.Parse(hm.Substring(3))).AddSeconds(sec), t, tom, s);
			}
			bool Fails(UpdateAsset a)
			{
				try
				{
					UpdateInstaller.Verify(tmp, a);
					return false;
				}
				catch (InvalidDataException)
				{
					return true;
				}
			}
		}
		catch (Exception ex3)
		{
			log.Add("EXCEPTION " + ex3);
			fails++;
		}
		try
		{
			Directory.CreateDirectory(App.AppData);
			File.WriteAllLines(Path.Combine(App.AppData, "selftest.log"), log);
		}
		catch
		{
		}
		Environment.Exit((fails != 0) ? 1 : 0);
		void Check(string name, bool flag)
		{
			log.Add((flag ? "PASS " : "FAIL ") + name);
			if (!flag)
			{
				fails++;
			}
		}
		static string Rc(string tag, string asset)
		{
			return "{\"draft\":false,\"prerelease\":false,\"tag_name\":\"" + tag + "\",\"assets\":[{\"name\":\"" + asset + "\",\"browser_download_url\":\"https://github.com/MohamedElnaggar00/Fluent-Prayer-Times/releases/download/" + tag + "/" + asset + "\"}]}";
		}
		static JsonElement Rel(string json)
		{
			return JsonDocument.Parse(json).RootElement;
		}
		static string Rj(string tag, bool draft, bool pre, string asset)
		{
			return "{\"draft\":" + (draft ? "true" : "false") + ",\"prerelease\":" + (pre ? "true" : "false") + ",\"tag_name\":\"" + tag + "\",\"assets\":[{\"name\":\"" + asset + "\",\"browser_download_url\":\"https://github.com/MohamedElnaggar00/fluent-prayer-times/releases/download/" + tag + "/" + asset + "\"}]}";
		}
		static string Row(DateTime dd, int off, int method = 5)
		{
			Dictionary<string, string> rr = SolarCalc.Compute(dd, 30.8619, 29.5845, off, method, null, null, 1);
			if (rr != null)
			{
				return string.Join(" ", Times.Keys.Select((string k) => rr[k]));
			}
			return "null";
		}
		static string RowX(DateTime dd, double la, double lo, int off, int method, int asr, int hl)
		{
			Dictionary<string, string> rr = SolarCalc.Compute(dd, la, lo, off, method, null, null, asr, hl);
			if (rr != null)
			{
				return string.Join(" ", Times.Keys.Select((string k) => rr[k]));
			}
			return "null";
		}
	}

	public static void RunUpdateE2E()
	{
		List<string> log = new List<string>();
		int fails = 0;
		try
		{
			Task.Run(async delegate
			{
				using HttpClient http = new HttpClient();
				http.DefaultRequestHeaders.UserAgent.ParseAdd("FluentPrayerTimes-ci");
				JsonDocument jsonDocument = JsonDocument.Parse(await http.GetStringAsync("https://api.github.com/repos/MohamedElnaggar00/fluent-prayer-times/releases/latest"));
				UpdateAsset asset = MainWindow.UpdateAssetFor(jsonDocument.RootElement, new Version(1, 3, 3));
				Check("live release offers an installer to 1.3.3", asset != null && asset.Sha256 != null && asset.Size > 1000000);
				if (asset == null)
				{
					return;
				}
				log.Add("asset " + asset.Name + " " + asset.Size + " bytes");
				List<double> steps = new List<double>();
				string text = await UpdateInstaller.DownloadAsync(asset, delegate(double p)
				{
					steps.Add(p);
				}, CancellationToken.None);
				Check("download finished and file exists", File.Exists(text) && new FileInfo(text).Length == asset.Size);
				int ok;
				if (steps.Count > 5)
				{
					List<double> list = steps;
					if (list[list.Count - 1] >= 99.9)
					{
						ok = (steps.Zip(steps.Skip(1), (double a, double b) => b >= a).All((bool x) => x) ? 1 : 0);
						goto IL_02ee;
					}
				}
				ok = 0;
				goto IL_02ee;
				IL_02ee:
				Check("progress reported and reached 100", (byte)ok != 0);
				Check("downloaded installer passes verification", Verifies(text, asset));
				Check("no partial file left after success", !Directory.GetFiles(UpdateInstaller.Dir, "*.part").Any());
				CancellationTokenSource cts = new CancellationTokenSource();
				try
				{
					bool canceled = false;
					try
					{
						await UpdateInstaller.DownloadAsync(asset, delegate(double p)
						{
							if (p > 3.0)
							{
								cts.Cancel();
							}
						}, cts.Token);
					}
					catch (OperationCanceledException)
					{
						canceled = true;
					}
					Check("cancel stops the download", canceled);
					Check("cancel leaves no files behind", Directory.GetFiles(UpdateInstaller.Dir).Length == 0);
					bool badHash = false;
					try
					{
						await UpdateInstaller.DownloadAsync(asset with
						{
							Sha256 = new string('0', 64)
						}, delegate
						{
						}, CancellationToken.None);
					}
					catch (InvalidDataException)
					{
						badHash = true;
					}
					Check("wrong checksum is rejected", badHash);
					Check("rejected download leaves no installer behind", Directory.GetFiles(UpdateInstaller.Dir).Length == 0);
					bool badHost = false;
					try
					{
						await UpdateInstaller.DownloadAsync(asset with
						{
							Url = "https://example.com/" + asset.Name
						}, delegate
						{
						}, CancellationToken.None);
					}
					catch (InvalidOperationException)
					{
						badHost = true;
					}
					Check("untrusted host is refused before any download", badHost);
				}
				finally
				{
					if (cts != null)
					{
						((IDisposable)cts).Dispose();
					}
				}
			}).GetAwaiter().GetResult();
		}
		catch (Exception ex)
		{
			log.Add("EXCEPTION " + ex);
			fails++;
		}
		try
		{
			Directory.Delete(UpdateInstaller.Dir, recursive: true);
		}
		catch
		{
		}
		try
		{
			Directory.CreateDirectory(App.AppData);
			File.WriteAllLines(Path.Combine(App.AppData, "update-e2e.log"), log);
		}
		catch
		{
		}
		Environment.Exit((fails != 0) ? 1 : 0);
		void Check(string name, bool ok)
		{
			log.Add((ok ? "PASS " : "FAIL ") + name);
			if (!ok)
			{
				fails++;
			}
		}
	}

	private static bool Verifies(string file, UpdateAsset asset)
	{
		try
		{
			UpdateInstaller.Verify(file, asset);
			return true;
		}
		catch
		{
			return false;
		}
	}
}
