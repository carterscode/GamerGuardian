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
    // List ALL releases, not /releases/latest. GitHub's "latest" pointer is the
    // release flagged latest *by publish date*, which can lag or point at an
    // older line if releases ever publish out of order (a backported hotfix, a
    // re-run workflow, a manually edited release). That produced the version-
    // skipping a user reported -- updating 1.38 -> 1.39 only to be prompted for
    // 1.40 immediately after. Enumerating every release and picking the highest
    // *stable* semver guarantees we always jump straight to the newest version.
    private const string ApiUrl =
        "https://api.github.com/repos/carterscode/GamerGuardian/releases?per_page=100";

    /// <summary>One GitHub release, reduced to the fields the updater cares about.</summary>
    public sealed record ReleaseCandidate(
        string Version,
        bool IsPrerelease,
        bool IsDraft,
        string ReleaseUrl,
        string? InstallerUrl,
        long InstallerSize,
        string ReleaseNotes);

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
            return SelectBestUpdate(ParseReleases(json), CurrentSemver());
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses the GitHub <c>/releases</c> array JSON into candidates. The array
    /// form is a list of release objects (the same shape as <c>/releases/latest</c>,
    /// just many of them). Tolerant of missing fields -- a release without an
    /// installer asset yields a candidate with a null <see cref="ReleaseCandidate.InstallerUrl"/>,
    /// which <see cref="SelectBestUpdate"/> then skips.
    /// </summary>
    public static IReadOnlyList<ReleaseCandidate> ParseReleases(string json)
    {
        var list = new List<ReleaseCandidate>();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Array) return list;

        foreach (var rel in root.EnumerateArray())
        {
            var tag = rel.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
            var version = tag.TrimStart('v', 'V');
            var url = rel.TryGetProperty("html_url", out var u) ? u.GetString() ?? "" : "";
            var notes = rel.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
            var prerelease = rel.TryGetProperty("prerelease", out var p) && p.ValueKind == JsonValueKind.True;
            var draft = rel.TryGetProperty("draft", out var d) && d.ValueKind == JsonValueKind.True;

            string? installerUrl = null;
            long installerSize = 0;
            if (rel.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in assets.EnumerateArray())
                {
                    var name = a.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    if (name.StartsWith("GamerGuardian-Setup-", StringComparison.OrdinalIgnoreCase)
                        && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        installerUrl = a.TryGetProperty("browser_download_url", out var du) ? du.GetString() : null;
                        installerSize = a.TryGetProperty("size", out var s) ? s.GetInt64() : 0;
                        break;
                    }
                }
            }

            list.Add(new ReleaseCandidate(version, prerelease, draft, url, installerUrl, installerSize, notes));
        }
        return list;
    }

    /// <summary>
    /// Picks the single newest installable update strictly above <paramref name="currentVersion"/>.
    /// Skips drafts, prereleases (matching the repo's stable-only update channel),
    /// releases without an installer asset, and anything not newer than what's
    /// installed. Returns null when nothing qualifies. Pure and deterministic --
    /// the unit-testable core of the "always upgrade to the newest" guarantee.
    /// </summary>
    public static UpdateInfo? SelectBestUpdate(IEnumerable<ReleaseCandidate> candidates, string currentVersion)
    {
        if (candidates is null) return null;
        if (!TryParseSemver(currentVersion, out var current)) return null;

        UpdateInfo? best = null;
        var bestVersion = current;
        foreach (var c in candidates)
        {
            if (c.IsDraft || c.IsPrerelease) continue;
            if (string.IsNullOrEmpty(c.InstallerUrl)) continue;
            if (string.IsNullOrEmpty(c.Version)) continue;
            if (!TryParseSemver(c.Version, out var v)) continue;
            if (v <= bestVersion) continue;

            bestVersion = v;
            best = new UpdateInfo(c.Version, c.ReleaseUrl, c.InstallerUrl!, c.InstallerSize, c.ReleaseNotes);
        }
        return best;
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
