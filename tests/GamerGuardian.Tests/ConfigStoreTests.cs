using System;
using System.IO;
using GamerGuardian.Models;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

/// <summary>
/// Covers the two ways a user's settings can silently vanish: ConfigStore.Load
/// swallowing a deserialization problem into a fresh AppConfig, and the beta
/// first-launch seed touching the stable install's config.
/// </summary>
public sealed class ConfigStoreTests : IDisposable
{
    private readonly string _root;

    public ConfigStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "gg-cfgtests-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    private string Dir(string name)
    {
        var d = Path.Combine(_root, name);
        return d;
    }

    private static void WriteConfig(string dir, string json)
    {
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "config.json"), json);
    }

    // ---- Unknown fields must not reset the config ----

    [Fact]
    public void Load_ConfigWithUnknownFields_KeepsKnownValues_DoesNotResetToDefaults()
    {
        // The failure mode this guards: Load() catches everything and returns a
        // fresh AppConfig, so a deserialization change would wipe every setting
        // silently. An older or newer build's extra fields must be ignored, not
        // fatal.
        var dir = Dir("unknown-fields");
        WriteConfig(dir, """
        {
          "launchAtStartup": false,
          "pollIntervalSeconds": 77,
          "aFieldFromANewerBuild": { "nested": [1, 2, 3] },
          "anotherUnknownField": "whatever",
          "global": {
            "gameMode": { "monitor": true, "desiredOn": false, "autoApply": true },
            "someUnknownToggle": { "monitor": true }
          }
        }
        """);

        var cfg = new ConfigStore(dir).Load();

        Assert.False(cfg.LaunchAtStartup);          // non-default, preserved
        Assert.Equal(77, cfg.PollIntervalSeconds);  // non-default, preserved
        Assert.True(cfg.Global.GameMode.Monitor);
        Assert.False(cfg.Global.GameMode.DesiredOn);
        Assert.True(cfg.Global.GameMode.AutoApply);
    }

    [Fact]
    public void Load_ConfigWithUnknownFields_RoundTripsThroughSave()
    {
        // Unknown fields are dropped on save (System.Text.Json does not retain
        // them), but the known settings must survive the round trip intact.
        var dir = Dir("roundtrip");
        WriteConfig(dir, """
        { "pollIntervalSeconds": 45, "futureField": true }
        """);

        var store = new ConfigStore(dir);
        var cfg = store.Load();
        store.Save(cfg);

        Assert.Equal(45, new ConfigStore(dir).Load().PollIntervalSeconds);
    }

    [Fact]
    public void Load_ConfigWithRemovedConsolidateNotifications_LoadsWithoutResetting()
    {
        // consolidateNotifications was persisted for every user before it was
        // removed, so every existing config.json on disk still carries it. It is now
        // an unknown field and must be ignored, not treated as a parse failure --
        // otherwise the removal would silently wipe everyone's settings on upgrade.
        var dir = Dir("removed-field");
        WriteConfig(dir, """
        {
          "launchAtStartup": false,
          "pollIntervalSeconds": 61,
          "consolidateNotifications": true,
          "theme": "Light",
          "global": {
            "gameDvr": { "monitor": true, "desiredOn": false, "autoApply": true }
          }
        }
        """);

        var cfg = new ConfigStore(dir).Load();

        Assert.False(cfg.LaunchAtStartup);
        Assert.Equal(61, cfg.PollIntervalSeconds);
        Assert.Equal(AppThemeChoice.Light, cfg.Theme);
        Assert.True(cfg.Global.GameDvr.Monitor);
        Assert.False(cfg.Global.GameDvr.DesiredOn);
        Assert.True(cfg.Global.GameDvr.AutoApply);
    }

    [Fact]
    public void Save_DropsTheRemovedField_OnNextWrite()
    {
        var dir = Dir("removed-field-save");
        WriteConfig(dir, """
        { "pollIntervalSeconds": 61, "consolidateNotifications": true }
        """);

        var store = new ConfigStore(dir);
        store.Save(store.Load());

        var written = File.ReadAllText(Path.Combine(dir, "config.json"));
        Assert.DoesNotContain("consolidateNotifications", written, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(61, new ConfigStore(dir).Load().PollIntervalSeconds);
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var cfg = new ConfigStore(Dir("nothing-here")).Load();
        Assert.Equal(new AppConfig().PollIntervalSeconds, cfg.PollIntervalSeconds);
    }

    [Fact]
    public void Load_MalformedJson_FallsBackToDefaults_DocumentedBehavior()
    {
        // Pins the known lossy path: genuinely unparseable JSON resets to defaults
        // rather than throwing. Unknown *fields* must never take this branch --
        // that distinction is the point of the tests above.
        var dir = Dir("malformed");
        WriteConfig(dir, "{ this is not json");

        var cfg = new ConfigStore(dir).Load();

        Assert.Equal(new AppConfig().PollIntervalSeconds, cfg.PollIntervalSeconds);
    }

    // ---- Beta first-launch seed ----

    [Fact]
    public void SeedConfigFrom_CopiesStableConfig_WhenTargetDoesNotExist()
    {
        var stable = Dir("stable");
        var beta = Dir("beta");
        WriteConfig(stable, """{ "pollIntervalSeconds": 99 }""");

        Assert.True(ConfigStore.SeedConfigFrom(stable, beta));
        Assert.Equal(99, new ConfigStore(beta).Load().PollIntervalSeconds);
    }

    [Fact]
    public void SeedConfigFrom_NeverWritesToTheSource()
    {
        var stable = Dir("stable-untouched");
        var beta = Dir("beta-untouched");
        WriteConfig(stable, """{ "pollIntervalSeconds": 31 }""");
        var sourceFile = Path.Combine(stable, "config.json");
        var before = File.ReadAllText(sourceFile);
        var writtenBefore = File.GetLastWriteTimeUtc(sourceFile);

        ConfigStore.SeedConfigFrom(stable, beta);

        // Then the beta build changes its own settings.
        var betaStore = new ConfigStore(beta);
        var cfg = betaStore.Load();
        cfg.PollIntervalSeconds = 5;
        betaStore.Save(cfg);

        Assert.Equal(before, File.ReadAllText(sourceFile));
        Assert.Equal(writtenBefore, File.GetLastWriteTimeUtc(sourceFile));
        Assert.Equal(31, new ConfigStore(stable).Load().PollIntervalSeconds);
        Assert.Equal(5, new ConfigStore(beta).Load().PollIntervalSeconds);
    }

    [Fact]
    public void SeedConfigFrom_DoesNothing_WhenTargetAlreadyExists()
    {
        var stable = Dir("s2");
        var beta = Dir("b2");
        WriteConfig(stable, """{ "pollIntervalSeconds": 99 }""");
        WriteConfig(beta, """{ "pollIntervalSeconds": 11 }""");

        Assert.False(ConfigStore.SeedConfigFrom(stable, beta));
        Assert.Equal(11, new ConfigStore(beta).Load().PollIntervalSeconds); // not clobbered
    }

    [Fact]
    public void SeedConfigFrom_DoesNothing_WhenSourceHasNoConfig()
    {
        var stable = Dir("s3-empty");
        Directory.CreateDirectory(stable);
        var beta = Dir("b3");

        Assert.False(ConfigStore.SeedConfigFrom(stable, beta));
        Assert.False(Directory.Exists(beta));
    }

    [Fact]
    public void SeedConfigFrom_DoesNothing_WhenSourceAndTargetAreTheSame()
    {
        // The stable-build case: AppIdentity.ConfigDirectory == StableConfigDirectory,
        // so the seed must be inert rather than copying a file onto itself.
        var dir = Dir("same");
        WriteConfig(dir, """{ "pollIntervalSeconds": 22 }""");

        Assert.False(ConfigStore.SeedConfigFrom(dir, dir));
        Assert.False(ConfigStore.SeedConfigFrom(dir, dir.ToUpperInvariant()));
        Assert.Equal(22, new ConfigStore(dir).Load().PollIntervalSeconds);
    }

#if !BETA
    [Fact]
    public void SeedConfigFrom_InStableBuild_IsAlwaysANoOp()
    {
        // Wiring check against the real AppIdentity values, not temp paths. Only
        // meaningful in a stable build, where source and target are the same folder.
        // Under BETA they genuinely differ and seeding is the intended behavior, so
        // asserting a no-op here would assert the opposite of what beta must do.
        Assert.False(ConfigStore.SeedConfigFrom(
            AppIdentity.StableConfigDirectory, AppIdentity.ConfigDirectory));
    }
#endif

    [Fact]
    public void SeedConfigFrom_EmptyOrNullPaths_ReturnFalse()
    {
        Assert.False(ConfigStore.SeedConfigFrom("", Dir("x")));
        Assert.False(ConfigStore.SeedConfigFrom(Dir("y"), ""));
        Assert.False(ConfigStore.SeedConfigFrom(null!, null!));
    }
}
