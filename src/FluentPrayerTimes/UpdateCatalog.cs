using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FluentPrayerTimes;

internal static class UpdateCatalog
{
    internal static UpdateAsset? Select(JsonElement release, Version installed)
    {
        if (release.ValueKind != JsonValueKind.Object ||
            !release.TryGetProperty("draft", out var draft) || draft.ValueKind != JsonValueKind.False ||
            !release.TryGetProperty("prerelease", out var prerelease) || prerelease.ValueKind != JsonValueKind.False ||
            !release.TryGetProperty("tag_name", out var tag) || tag.ValueKind != JsonValueKind.String ||
            !Version.TryParse(tag.GetString()?.TrimStart('v', 'V'), out var version) || version.Build < 0 || version <= installed ||
            !release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            return null;

        string expectedName = "FluentPrayerTimes-v" + version.ToString(3) + "-installer-win-x64.exe";
        foreach (var asset in assets.EnumerateArray())
        {
            if (asset.ValueKind != JsonValueKind.Object ||
                !asset.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String || name.GetString() != expectedName ||
                !asset.TryGetProperty("browser_download_url", out var url) || url.ValueKind != JsonValueKind.String ||
                !Uri.TryCreate(url.GetString(), UriKind.Absolute, out var source) || source.Scheme != "https" ||
                source.Host != "github.com" ||
                !source.AbsolutePath.StartsWith("/Star-Dust-0/fluent-prayer-times/releases/download/", StringComparison.OrdinalIgnoreCase))
                continue;

            string? checksum = null;
            if (asset.TryGetProperty("digest", out var digest) && digest.ValueKind == JsonValueKind.String &&
                (digest.GetString() ?? "").StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                checksum = digest.GetString()![7..];
            if (checksum == null || !Regex.IsMatch(checksum, "^[0-9a-fA-F]{64}$"))
            {
                Log.Write("update", "Installer checksum unavailable");
                return null;
            }
            long size = asset.TryGetProperty("size", out var length) && length.ValueKind == JsonValueKind.Number && length.TryGetInt64(out var bytes) ? bytes : 0;
            return new UpdateAsset(source.AbsoluteUri, expectedName, size, checksum);
        }
        Log.Write("update", "Release installer not yet available");
        return null;
    }
}
