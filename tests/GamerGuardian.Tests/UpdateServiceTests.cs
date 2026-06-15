using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

/// <summary>
/// Update-selection tests. The HTTP call itself isn't tested (it needs the live
/// GitHub API); instead the pure selection/parse core is exercised directly.
/// These guard the "always upgrade to the newest" invariant -- the fix for the
/// version-skipping bug where /releases/latest could hand back an older line.
/// </summary>
public class UpdateServiceTests
{
    private static UpdateService.ReleaseCandidate Rel(
        string version, bool prerelease = false, bool draft = false, bool hasInstaller = true) =>
        new(version, prerelease, draft,
            ReleaseUrl: $"https://example/{version}",
            InstallerUrl: hasInstaller ? $"https://example/GamerGuardian-Setup-{version}.exe" : null,
            InstallerSize: 100,
            ReleaseNotes: $"notes {version}");

    [Fact]
    public void SelectBestUpdate_PicksHighestSemver_NotMostRecentlyListed()
    {
        // GitHub may list releases out of version order. We must jump to 1.40,
        // never settle for the 1.39 in the middle of the list.
        var candidates = new[] { Rel("1.39.0"), Rel("1.40.0"), Rel("1.38.0") };

        var result = UpdateService.SelectBestUpdate(candidates, "1.38.0");

        Assert.NotNull(result);
        Assert.Equal("1.40.0", result!.Version);
    }

    [Fact]
    public void SelectBestUpdate_SkipsIntermediateVersions_FromOldCurrent()
    {
        // The reported bug: on 1.38, do NOT offer 1.39 when 1.40 exists.
        var candidates = new[] { Rel("1.38.0"), Rel("1.39.0"), Rel("1.40.0") };

        var result = UpdateService.SelectBestUpdate(candidates, "1.38.0");

        Assert.Equal("1.40.0", result!.Version);
    }

    [Fact]
    public void SelectBestUpdate_ReturnsNull_WhenCurrentIsNewest()
    {
        var candidates = new[] { Rel("1.39.0"), Rel("1.40.0") };

        Assert.Null(UpdateService.SelectBestUpdate(candidates, "1.40.0"));
    }

    [Fact]
    public void SelectBestUpdate_ReturnsNull_WhenCurrentIsHigherThanAll()
    {
        var candidates = new[] { Rel("1.39.0"), Rel("1.40.0") };

        Assert.Null(UpdateService.SelectBestUpdate(candidates, "1.41.0"));
    }

    [Fact]
    public void SelectBestUpdate_SkipsPrereleases()
    {
        var candidates = new[] { Rel("1.41.0", prerelease: true), Rel("1.40.0") };

        var result = UpdateService.SelectBestUpdate(candidates, "1.38.0");

        Assert.Equal("1.40.0", result!.Version);
    }

    [Fact]
    public void SelectBestUpdate_SkipsDrafts()
    {
        var candidates = new[] { Rel("1.41.0", draft: true), Rel("1.40.0") };

        var result = UpdateService.SelectBestUpdate(candidates, "1.38.0");

        Assert.Equal("1.40.0", result!.Version);
    }

    [Fact]
    public void SelectBestUpdate_SkipsReleasesWithoutInstaller()
    {
        // Highest version is still building / has no Setup asset yet -> fall back
        // to the highest version that actually has an installer.
        var candidates = new[] { Rel("1.41.0", hasInstaller: false), Rel("1.40.0") };

        var result = UpdateService.SelectBestUpdate(candidates, "1.38.0");

        Assert.Equal("1.40.0", result!.Version);
    }

    [Fact]
    public void SelectBestUpdate_EmptyOrNull_ReturnsNull()
    {
        Assert.Null(UpdateService.SelectBestUpdate(System.Array.Empty<UpdateService.ReleaseCandidate>(), "1.0.0"));
        Assert.Null(UpdateService.SelectBestUpdate(null!, "1.0.0"));
    }

    [Fact]
    public void ParseReleases_ExtractsVersionsAndInstaller()
    {
        const string json = """
        [
          {
            "tag_name": "v1.40.0",
            "html_url": "https://github.com/carterscode/GamerGuardian/releases/tag/v1.40.0",
            "body": "release notes",
            "prerelease": false,
            "draft": false,
            "assets": [
              { "name": "GamerGuardian-Setup-1.40.0.exe", "browser_download_url": "https://example/setup.exe", "size": 12345 }
            ]
          },
          {
            "tag_name": "v1.39.0-dev",
            "prerelease": true,
            "draft": false,
            "assets": []
          }
        ]
        """;

        var list = UpdateService.ParseReleases(json);

        Assert.Equal(2, list.Count);
        Assert.Equal("1.40.0", list[0].Version);
        Assert.False(list[0].IsPrerelease);
        Assert.Equal("https://example/setup.exe", list[0].InstallerUrl);
        Assert.Equal(12345, list[0].InstallerSize);
        Assert.True(list[1].IsPrerelease);
        Assert.Null(list[1].InstallerUrl);
    }

    [Fact]
    public void ParseReleases_ThenSelect_EndToEnd_PicksStableHighest()
    {
        const string json = """
        [
          { "tag_name": "v1.39.0", "prerelease": false, "draft": false,
            "assets": [ { "name": "GamerGuardian-Setup-1.39.0.exe", "browser_download_url": "https://example/39.exe", "size": 1 } ] },
          { "tag_name": "v1.41.0-dev", "prerelease": true, "draft": false,
            "assets": [ { "name": "GamerGuardian-Setup-1.41.0-dev.exe", "browser_download_url": "https://example/41dev.exe", "size": 1 } ] },
          { "tag_name": "v1.40.0", "prerelease": false, "draft": false,
            "assets": [ { "name": "GamerGuardian-Setup-1.40.0.exe", "browser_download_url": "https://example/40.exe", "size": 1 } ] }
        ]
        """;

        var result = UpdateService.SelectBestUpdate(UpdateService.ParseReleases(json), "1.38.0");

        Assert.Equal("1.40.0", result!.Version);
    }

    [Fact]
    public void ParseReleases_NonArrayJson_ReturnsEmpty()
    {
        Assert.Empty(UpdateService.ParseReleases("""{ "message": "Not Found" }"""));
    }
}
