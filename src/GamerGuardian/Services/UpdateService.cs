using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace GamerGuardian.Services;

public sealed record UpdateInfo(
    string Version,
    string ReleaseUrl,
    string InstallerUrl,
    long InstallerSize,
    string ReleaseNotes);

public static class UpdateService
{
    private const string ApiUrl =
        "https://api.github.com/repos/carterscode/GamerGuardian/releases/latest";

    public static string CurrentSemver()
    {
        var info = typeof(UpdateService).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        var idx = info.IndexOf('+');
        return idx > 0 ? info[..idx] : info;
    }

    public static async Task<UpdateInfo?> CheckLatestAsync(CancellationToken ct = default)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("GamerGuardian/1.0");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            var json = await http.GetStringAsync(ApiUrl, ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tag = root.GetProperty("tag_name").GetString() ?? "";
            var version = tag.TrimStart('v', 'V');
            var url = root.TryGetProperty("html_url", out var u) ? u.GetString() ?? "" : "";
            var notes = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";

            string? installerUrl = null;
            long installerSize = 0;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var a in assets.EnumerateArray())
                {
                    var name = a.GetProperty("name").GetString() ?? "";
                    if (name.StartsWith("GamerGuardian-Setup-", StringComparison.OrdinalIgnoreCase)
                        && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        installerUrl = a.GetProperty("browser_download_url").GetString();
                        installerSize = a.TryGetProperty("size", out var s) ? s.GetInt64() : 0;
                        break;
                    }
                }
            }
            if (string.IsNullOrEmpty(installerUrl)) return null;
            if (string.IsNullOrEmpty(version)) return null;

            if (!TryParseSemver(version, out var newer)) return null;
            if (!TryParseSemver(CurrentSemver(), out var current)) return null;
            if (newer <= current) return null;

            return new UpdateInfo(version, url, installerUrl!, installerSize, notes);
        }
        catch
        {
            return null;
        }
    }

    public static async Task<string?> DownloadInstallerAsync(
        UpdateInfo info,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            var fileName = Path.GetFileName(new Uri(info.InstallerUrl).LocalPath);
            if (string.IsNullOrEmpty(fileName)) fileName = $"GamerGuardian-Setup-{info.Version}.exe";
            var destPath = Path.Combine(Path.GetTempPath(), fileName);

            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("GamerGuardian/1.0");

            using var response = await http.GetAsync(info.InstallerUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength ?? info.InstallerSize;
            await using var src = await response.Content.ReadAsStreamAsync(ct);
            await using var dst = File.Create(destPath);

            var buf = new byte[81920];
            long received = 0;
            int n;
            while ((n = await src.ReadAsync(buf.AsMemory(0, buf.Length), ct)) > 0)
            {
                await dst.WriteAsync(buf.AsMemory(0, n), ct);
                received += n;
                if (total > 0) progress?.Report((double)received / total);
            }
            return destPath;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryParseSemver(string s, out Version v)
    {
        // Accept "1.2.3" or "1.2" — strip any prerelease suffix like "-rc1".
        var clean = s;
        var dash = clean.IndexOf('-');
        if (dash > 0) clean = clean[..dash];
        var plus = clean.IndexOf('+');
        if (plus > 0) clean = clean[..plus];
        if (Version.TryParse(clean, out var parsed))
        {
            v = parsed;
            return true;
        }
        v = new Version();
        return false;
    }
}
