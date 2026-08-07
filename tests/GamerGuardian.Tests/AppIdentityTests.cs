using System;
using System.IO;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

/// <summary>
/// Pins the non-beta identity to the exact literals the code used before
/// <see cref="AppIdentity"/> existed. The test project compiles without the BETA
/// constant, so these assertions are the enforced proof that a stable build's
/// paths, mutex name and startup entry are behaviorally unchanged -- a beta-only
/// value leaking into the default build fails here.
/// </summary>
public class AppIdentityTests
{
    private static string AppData => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    [Fact]
    public void StableBuild_UsesTheOriginalProductFolder()
    {
        Assert.Equal("GamerGuardian", AppIdentity.ProductFolderName);
        Assert.Equal(Path.Combine(AppData, "GamerGuardian"), AppIdentity.ConfigDirectory);
    }

    [Fact]
    public void StableBuild_ConfigAndChangeLogPaths_MatchTheOriginals()
    {
        Assert.Equal(Path.Combine(AppData, "GamerGuardian", "config.json"), AppIdentity.ConfigFile);
        Assert.Equal(Path.Combine(AppData, "GamerGuardian", "changes.log"), AppIdentity.ChangeLogFile);
    }

    [Fact]
    public void StableBuild_DiagnosticPaths_MatchTheOriginals()
    {
        Assert.Equal("gamerguardian", AppIdentity.DiagnosticPrefix);
        Assert.Equal(Path.Combine(Path.GetTempPath(), "gamerguardian_error.log"), AppIdentity.ErrorLogFile);
        Assert.Equal(Path.Combine(Path.GetTempPath(), "gamerguardian_selftest.txt"), AppIdentity.SelfTestFile);
    }

    [Fact]
    public void StableBuild_MutexAndStartupNames_MatchTheOriginals()
    {
        Assert.Equal("GamerGuardian.SingleInstance", AppIdentity.MutexName);
        Assert.Equal("GamerGuardian", AppIdentity.StartupRegistryValueName);
    }

    [Fact]
    public void StableBuild_DisplaySuffix_IsEmpty_SoTitlesAreUnchanged()
    {
        // Callers concatenate this unconditionally; empty is what keeps the stable
        // window title and tray tooltip byte-identical to before.
        Assert.Equal(string.Empty, AppIdentity.DisplaySuffix);
        Assert.Equal("GamerGuardian", "GamerGuardian" + AppIdentity.DisplaySuffix);
        Assert.Equal("GamerGuardian - Settings", "GamerGuardian - Settings" + AppIdentity.DisplaySuffix);
    }

    [Fact]
    public void ConfigStore_ResolvesToTheSamePathsAsAppIdentity()
    {
        // The whole point of the shared source: ConfigStore and ChangeLogger used to
        // build this path independently and could drift apart.
        var store = new ConfigStore();
        Assert.Equal(AppIdentity.ConfigDirectory, store.ConfigDirectory);
        Assert.Equal(AppIdentity.ConfigFile, store.ConfigPath);
    }

    [Fact]
    public void ChangeLogger_WritesIntoTheSameFolderAsTheConfig()
    {
        Assert.Equal(AppIdentity.ChangeLogFile, ChangeLogger.LogPath);
        Assert.Equal(
            Path.GetDirectoryName(AppIdentity.ConfigFile),
            Path.GetDirectoryName(ChangeLogger.LogPath));
    }

    [Fact]
    public void StableConfigDirectory_IsTheStableRoot_EvenInAStableBuild()
    {
        // In a stable build the seed source and the live root are the same folder,
        // which is what makes the beta first-launch copy a no-op here.
        Assert.Equal(Path.Combine(AppData, "GamerGuardian"), AppIdentity.StableConfigDirectory);
        Assert.Equal(AppIdentity.ConfigDirectory, AppIdentity.StableConfigDirectory);
    }
}
