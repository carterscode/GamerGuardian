using GamerGuardian.Models;
using GamerGuardian.Native;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class DisplayPreferenceResolverTests
{
    private static DisplayInfo Display(string gdi, string friendly, string devicePath) =>
        new(GdiDeviceName: gdi, FriendlyName: friendly, DevicePath: devicePath,
            AdapterId: default, TargetId: 0, SourceId: 0);

    private static DisplayPreference FixedRefresh(uint hz, string label) => new()
    {
        DisplayLabel = label,
        RefreshRate = new RefreshRatePref { Target = RefreshRateTarget.Fixed, FixedHz = hz },
    };

    [Fact]
    public void Resolve_ExactKeyHit_ReturnsSavedPrefs()
    {
        var d = Display(@"\\.\DISPLAY1", "PG32UCDM", @"\\?\DISPLAY#AUS32F2#abc");
        var cfg = new AppConfig();
        cfg.Displays[d.StableKey] = FixedRefresh(240, "PG32UCDM");

        var pref = DisplayPreferenceResolver.Resolve(cfg, d, new[] { d });

        Assert.Equal(RefreshRateTarget.Fixed, pref.RefreshRate.Target);
        Assert.Equal(240u, pref.RefreshRate.FixedHz);
        Assert.Single(cfg.Displays); // no new entry minted
    }

    [Fact]
    public void Resolve_DevicePathDisappeared_ReusesSavedPrefsUnderNewKey()
    {
        // Saved while the monitor reported a DevicePath...
        var saved = FixedRefresh(240, "PG32UCDM");
        var cfg = new AppConfig();
        cfg.Displays[@"\\?\DISPLAY#AUS32F2#abc"] = saved;

        // ...now it enumerates WITHOUT one, so StableKey falls back to label|gdi.
        var noPath = Display(@"\\.\DISPLAY1", "PG32UCDM", "");
        Assert.Equal(@"PG32UCDM|\\.\DISPLAY1", noPath.StableKey);

        var pref = DisplayPreferenceResolver.Resolve(cfg, noPath, new[] { noPath });

        // The Fixed 240 target survived instead of resetting to Maximum.
        Assert.Same(saved, pref);
        Assert.Equal(240u, pref.RefreshRate.FixedHz);
        Assert.Single(cfg.Displays);
        Assert.True(cfg.Displays.ContainsKey(noPath.StableKey));
        Assert.False(cfg.Displays.ContainsKey(@"\\?\DISPLAY#AUS32F2#abc")); // re-keyed
    }

    [Fact]
    public void Resolve_UnknownDisplay_CreatesDefault()
    {
        var cfg = new AppConfig();
        var d = Display(@"\\.\DISPLAY1", "NewPanel", @"\\?\DISPLAY#NEW#xyz");

        var pref = DisplayPreferenceResolver.Resolve(cfg, d, new[] { d });

        Assert.Equal(RefreshRateTarget.Maximum, pref.RefreshRate.Target);
        Assert.Equal("NewPanel", pref.DisplayLabel);
        Assert.True(cfg.Displays.ContainsKey(d.StableKey));
    }

    [Fact]
    public void Resolve_TwoIdenticalActiveMonitors_DoesNotSharePrefs()
    {
        // Same friendly name on two active outputs: label is ambiguous, so we
        // must NOT hand both displays the same saved pref block.
        var saved = FixedRefresh(240, "Acme27");
        var cfg = new AppConfig();
        cfg.Displays[@"\\?\DISPLAY#ACME#one"] = saved;

        var a = Display(@"\\.\DISPLAY1", "Acme27", "");   // path missing this tick
        var b = Display(@"\\.\DISPLAY2", "Acme27", @"\\?\DISPLAY#ACME#two");
        var active = new[] { a, b };

        var pref = DisplayPreferenceResolver.Resolve(cfg, a, active);

        Assert.NotSame(saved, pref); // got a fresh default, not the other monitor's prefs
        Assert.Equal(RefreshRateTarget.Maximum, pref.RefreshRate.Target);
    }

    [Fact]
    public void DedupeDisplays_CollapsesValueIdenticalDuplicates_KeepingDevicePathKey()
    {
        var cfg = new AppConfig();
        cfg.Displays[@"\\?\DISPLAY#GSM84CD#abc"] = FixedRefresh(120, "LG TV");
        cfg.Displays[@"LG TV|\\.\DISPLAY1"] = FixedRefresh(120, "LG TV"); // stale fallback dupe

        DisplayPreferenceResolver.DedupeDisplays(cfg);

        Assert.Single(cfg.Displays);
        Assert.True(cfg.Displays.ContainsKey(@"\\?\DISPLAY#GSM84CD#abc"));
        Assert.False(cfg.Displays.ContainsKey(@"LG TV|\\.\DISPLAY1"));
    }

    [Fact]
    public void DedupeDisplays_KeepsDuplicatesWithDifferentSettings()
    {
        // Same label but genuinely different settings → could be two real
        // panels of one model; don't merge and lose data.
        var cfg = new AppConfig();
        cfg.Displays[@"\\?\DISPLAY#ACME#one"] = FixedRefresh(240, "Acme27");
        cfg.Displays[@"\\?\DISPLAY#ACME#two"] = FixedRefresh(144, "Acme27");

        DisplayPreferenceResolver.DedupeDisplays(cfg);

        Assert.Equal(2, cfg.Displays.Count);
    }
}
