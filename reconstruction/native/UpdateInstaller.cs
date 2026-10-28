using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace FluentPrayerTimes;

internal static class UpdateInstaller
{
	internal static string Dir => Path.Combine(Path.GetTempPath(), "FluentPrayerTimes-update");

	internal static bool IsTrustedSource(Uri? uri)
	{
		if (uri != null && uri.Scheme == Uri.UriSchemeHttps)
		{
			if (!(uri.Host == "github.com"))
			{
				return uri.Host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		return false;
	}

	internal static void Verify(string path, UpdateAsset asset)
	{
		long length = new FileInfo(path).Length;
		if ((asset.Size > 0) ? (length != asset.Size) : (length < 1000000))
		{
			throw new InvalidDataException("Installer size does not match the release.");
		}
		using FileStream fileStream = File.OpenRead(path);
		if (fileStream.ReadByte() != 77 || fileStream.ReadByte() != 90)
		{
			throw new InvalidDataException("Downloaded file is not a Windows installer.");
		}
		if (asset.Sha256 != null)
		{
			fileStream.Position = 0L;
			if (!Convert.ToHexString(SHA256.HashData(fileStream)).Equals(asset.Sha256, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidDataException("Installer checksum does not match the release.");
			}
		}
	}

	internal static async Task<string> DownloadAsync(UpdateAsset asset, Action<double> progress, CancellationToken cancel)
	{
		if (asset.Name != Path.GetFileName(asset.Name) || !asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Unexpected installer name.");
		}
		if (!Uri.TryCreate(asset.Url, UriKind.Absolute, out Uri result) || !IsTrustedSource(result))
		{
			throw new InvalidOperationException("Untrusted download source.");
		}
		Directory.CreateDirectory(Dir);
		string[] files = Directory.GetFiles(Dir);
		foreach (string path in files)
		{
			try
			{
				File.Delete(path);
			}
			catch
			{
			}
		}
		string target = Path.Combine(Dir, asset.Name);
		string part = target + ".part";
		try
		{
			using HttpClient http = new HttpClient
			{
				Timeout = Timeout.InfiniteTimeSpan
			};
			http.DefaultRequestHeaders.UserAgent.ParseAdd("FluentPrayerTimes/" + (typeof(UpdateInstaller).Assembly.GetName().Version?.ToString(3) ?? "0.0.0"));
			using HttpResponseMessage response = await http.GetAsync(result, HttpCompletionOption.ResponseHeadersRead, cancel);
			response.EnsureSuccessStatusCode();
			if (!IsTrustedSource(response.RequestMessage?.RequestUri))
			{
				throw new InvalidOperationException("Download was redirected to an untrusted host.");
			}
			long total = response.Content.Headers.ContentLength ?? asset.Size;
			await using (Stream input = await response.Content.ReadAsStreamAsync(cancel))
			{
				await using FileStream output = new FileStream(part, FileMode.Create, FileAccess.Write, FileShare.None);
				byte[] buffer = new byte[81920];
				long done = 0L;
				while (true)
				{
					int num;
					int read = (num = await input.ReadAsync(buffer, cancel));
					if (num <= 0)
					{
						break;
					}
					await output.WriteAsync(buffer.AsMemory(0, read), cancel);
					done += read;
					if (total > 0)
					{
						progress(Math.Min(100.0, (double)done * 100.0 / (double)total));
					}
				}
			}
			await Task.Run(delegate
			{
				Verify(part, asset);
			}, cancel);
			File.Move(part, target, overwrite: true);
			return target;
		}
		catch
		{
			try
			{
				File.Delete(part);
			}
			catch
			{
			}
			throw;
		}
	}
}
