using System;
using System.Text.Json;
namespace FluentPrayerTimes;
internal static class UpdateCatalog
{
	internal static UpdateAsset? Select(JsonElement release, Version installed)
	{
		if (release.GetProperty("draft").GetBoolean() || release.GetProperty("prerelease").GetBoolean())
		{
			return null;
		}
		Version? version = Version.TryParse(release.GetProperty("tag_name").GetString()?.TrimStart('v', 'V'), out var parsed) ? parsed : null;
		if (version == null || version <= installed)
		{
			return null;
		}
		string[] array = { "installer-win-x64.exe" };
		foreach (string text in array)
		{
			foreach (JsonElement item in release.GetProperty("assets").EnumerateArray())
			{
				string text2 = item.GetProperty("name").GetString();
				if (text2 != "FluentPrayerTimes-v" + version.ToString(3) + "-" + text)
				{
					continue;
				}
				string text3 = item.GetProperty("browser_download_url").GetString();
				if (Uri.TryCreate(text3, UriKind.Absolute, out Uri result) && result.Scheme == "https" && result.Host == "github.com" && result.AbsolutePath.StartsWith("/Star-Dust-0/fluent-prayer-times/releases/download/", StringComparison.OrdinalIgnoreCase))
				{
					JsonElement value;
					long size = ((item.TryGetProperty("size", out value) && value.ValueKind == JsonValueKind.Number) ? value.GetInt64() : 0);
					string sha = null;
					if (item.TryGetProperty("digest", out var value2) && value2.ValueKind == JsonValueKind.String && (value2.GetString() ?? "").StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
					{
						sha = value2.GetString().Substring(7);
					}
					if (sha == null || !System.Text.RegularExpressions.Regex.IsMatch(sha, "^[0-9a-fA-F]{64}$")) { Log.Write("update", "Installer checksum unavailable"); return null; }
                    return new UpdateAsset(text3, text2, size, sha);
				}
			}
		}
		Log.Write("update", "Release installer not yet available");
        return null;
	}

}
