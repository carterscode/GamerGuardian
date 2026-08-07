using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamerGuardian.Models;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

/// <summary>
/// Covers <see cref="MonitorService.MergeDrift"/> — the pure rule behind the
/// published <c>CurrentDrift</c> snapshot the UI counts. Two guarantees matter:
/// a tier-scoped tick must not erase the tier it didn't check, and a setting the
/// app just auto-applied and verified must not still read as drifted.
/// </summary>
public class MonitorServiceDriftSetTests
{
    // "hags" is Stable; "hdr"/"refresh" are Volatile (see MonitorVolatility).
    private const string StableId = "hags";
    private const string StableId2 = "gamemode";
    private const string VolatileId = "hdr:DISPLAY1";
    private const string VolatileId2 = "refresh:DISPLAY1";

    private static DriftItem Drift(string id) => new(
        SettingId: id,
        DisplayKey: "global",
        DisplayLabel: "Global",
        Description: id,
        CurrentValue: "On",
        DesiredValue: "Off",
        AutoApply: false,
        Apply: () => Task.CompletedTask);

    private static IReadOnlyDictionary<string, DriftItem> Snapshot(params string[] ids) =>
        ids.ToDictionary(i => i, Drift, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlySet<string> NothingApplied = new HashSet<string>();

    private static IReadOnlySet<string> Applied(params string[] ids) =>
        new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void FullScan_ReplacesEverything()
    {
        var previous = Snapshot(StableId, VolatileId);

        var merged = MonitorService.MergeDrift(
            previous, tier: null, new[] { Drift(StableId2) }, NothingApplied);

        Assert.Equal(new[] { StableId2 }, merged.Keys);
    }

    [Fact]
    public void VolatileOnlyTick_DoesNotEraseStableEntries()
    {
        // The regression this guards: a 30-second display poll must not flush the
        // ~40 stable settings it never looked at, or the count flickers to near-zero.
        var previous = Snapshot(StableId, VolatileId);

        var merged = MonitorService.MergeDrift(
            previous, MonitorTier.Volatile, new[] { Drift(VolatileId2) }, NothingApplied);

        Assert.Contains(StableId, merged.Keys);        // untouched tier carried forward
        Assert.Contains(VolatileId2, merged.Keys);     // this tick's finding
        Assert.DoesNotContain(VolatileId, merged.Keys); // volatile entry that cleared
    }

    [Fact]
    public void StableOnlyTick_DoesNotEraseVolatileEntries()
    {
        var previous = Snapshot(StableId, VolatileId);

        var merged = MonitorService.MergeDrift(
            previous, MonitorTier.Stable, new[] { Drift(StableId2) }, NothingApplied);

        Assert.Contains(VolatileId, merged.Keys);
        Assert.Contains(StableId2, merged.Keys);
        Assert.DoesNotContain(StableId, merged.Keys);
    }

    [Fact]
    public void SettingThatStoppedDrifting_IsDropped_WhenItsTierWasChecked()
    {
        var previous = Snapshot(StableId);

        var merged = MonitorService.MergeDrift(
            previous, MonitorTier.Stable, Array.Empty<DriftItem>(), NothingApplied);

        Assert.Empty(merged);
    }

    [Fact]
    public void AutoAppliedAndVerified_IsNotReportedAsDrifted()
    {
        // It was drifting when the scan started, the app fixed it during the same
        // tick, so the published count must not still include it.
        var merged = MonitorService.MergeDrift(
            MonitorService.MergeDrift(Snapshot(), null, Array.Empty<DriftItem>(), NothingApplied),
            tier: null,
            new[] { Drift(StableId), Drift(StableId2) },
            Applied(StableId));

        Assert.DoesNotContain(StableId, merged.Keys);
        Assert.Contains(StableId2, merged.Keys);
    }

    [Fact]
    public void AutoApplyThatFailedToVerify_StaysDrifted()
    {
        var merged = MonitorService.MergeDrift(
            Snapshot(), tier: null, new[] { Drift(StableId) }, NothingApplied);

        Assert.Contains(StableId, merged.Keys);
    }

    [Fact]
    public void AppliedAndVerified_AlsoClearsAnEntryCarriedFromAnEarlierTick()
    {
        var previous = Snapshot(VolatileId);

        // A stable-tier tick that auto-fixed a volatile setting still clears it.
        var merged = MonitorService.MergeDrift(
            previous, MonitorTier.Stable, Array.Empty<DriftItem>(), Applied(VolatileId));

        Assert.Empty(merged);
    }

    [Fact]
    public void Merge_IsCaseInsensitive_OnSettingIds()
    {
        var merged = MonitorService.MergeDrift(
            Snapshot(), tier: null, new[] { Drift("HAGS") }, Applied("hags"));

        Assert.Empty(merged);
    }

    [Fact]
    public void Merge_DoesNotMutateItsInputs()
    {
        var previous = new Dictionary<string, DriftItem>(StringComparer.OrdinalIgnoreCase)
        {
            [StableId] = Drift(StableId),
        };
        var incoming = new List<DriftItem> { Drift(VolatileId) };

        var merged = MonitorService.MergeDrift(previous, null, incoming, NothingApplied);

        Assert.Single(previous);
        Assert.Single(incoming);
        Assert.NotSame(previous, merged);
    }

    [Fact]
    public void EmptyScan_WithNoPreviousDrift_YieldsEmptySnapshot()
    {
        var merged = MonitorService.MergeDrift(
            Snapshot(), tier: null, Array.Empty<DriftItem>(), NothingApplied);

        Assert.Empty(merged);
    }

    /// <summary>
    /// The snapshot is what the UI groups by section, so the two must compose:
    /// every drifted id resolves to a section and the per-section counts add up.
    /// </summary>
    [Fact]
    public void Snapshot_GroupsBySection_ForTheCountSurface()
    {
        var merged = MonitorService.MergeDrift(
            Snapshot(),
            tier: null,
            new[]
            {
                Drift("hags"),                  // Gaming
                Drift("gamemode"),              // Gaming
                Drift("hdr:DISPLAY1"),          // Display
                Drift("service:diagtrack"),     // Services
                Drift("ai.copilot"),            // WindowsAi
            },
            NothingApplied);

        var bySection = merged.Keys
            .GroupBy(SettingSectionMap.SectionFor)
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(2, bySection[SettingSection.Gaming]);
        Assert.Equal(1, bySection[SettingSection.Display]);
        Assert.Equal(1, bySection[SettingSection.Services]);
        Assert.Equal(1, bySection[SettingSection.WindowsAi]);
        Assert.DoesNotContain(SettingSection.Unknown, bySection.Keys);
        Assert.Equal(5, merged.Count);
    }
}
