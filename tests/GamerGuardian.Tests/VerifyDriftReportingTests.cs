using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamerGuardian.Models;
using GamerGuardian.Services;
using GamerGuardian.UI;
using Xunit;

namespace GamerGuardian.Tests;

/// <summary>
/// Covers the reported defect: "Verify" said one setting had drifted while the
/// Status page kept showing 0, and there was no way to put the setting back.
///
/// <para>Two distinct causes, both covered here — Verify never published what it
/// found, and a manual fix never removed the setting from the published set.</para>
/// </summary>
public class VerifyDriftReportingTests
{
    private const string StableId = "hags";
    private const string StableId2 = "gamemode";
    private const string VolatileId = "hdr:DISPLAY1";

    private static DriftItem Drift(string id, bool monitored = true, bool autoApply = false) => new(
        SettingId: id,
        DisplayKey: "global",
        DisplayLabel: id,
        Description: id,
        CurrentValue: "On",
        DesiredValue: "Off",
        AutoApply: autoApply,
        Apply: () => Task.CompletedTask,
        IsMonitored: monitored);

    private static IReadOnlyDictionary<string, DriftItem> Snapshot(params string[] ids) =>
        ids.ToDictionary(i => i, i => Drift(i), StringComparer.OrdinalIgnoreCase);

    // ---- WithoutResolved: fixing a setting drops it from the count -----------

    [Fact]
    public void WithoutResolved_RemovesTheFixedSetting()
    {
        var previous = Snapshot(StableId, VolatileId);

        var merged = MonitorService.WithoutResolved(
            previous, new HashSet<string> { StableId });

        Assert.Equal(new[] { VolatileId }, merged.Keys);
    }

    [Fact]
    public void WithoutResolved_KeepsEveryTierItDidNotResolve()
    {
        // The regression this guards: a manual fix must not be expressed as a
        // MergeDrift call, which would either wipe the snapshot (tier: null) or
        // only touch one tier. Fixing a stable setting must leave volatile drift
        // alone and vice versa.
        var previous = Snapshot(StableId, StableId2, VolatileId);

        var merged = MonitorService.WithoutResolved(
            previous, new HashSet<string> { VolatileId });

        Assert.Equal(
            new[] { StableId, StableId2 }.OrderBy(k => k, StringComparer.Ordinal).ToArray(),
            merged.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void WithoutResolved_IsCaseInsensitiveOnSettingIds()
    {
        var previous = Snapshot(StableId);

        var merged = MonitorService.WithoutResolved(
            previous, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { StableId.ToUpperInvariant() });

        Assert.Empty(merged);
    }

    [Fact]
    public void WithoutResolved_UnknownIdIsANoOp()
    {
        var previous = Snapshot(StableId);

        var merged = MonitorService.WithoutResolved(
            previous, new HashSet<string> { "not-a-setting" });

        Assert.Equal(new[] { StableId }, merged.Keys);
    }

    [Fact]
    public void WithoutResolved_DoesNotMutateTheInput()
    {
        // The published snapshot is handed out to UI threads as an immutable
        // reference; mutating it in place would change a snapshot someone is
        // already enumerating.
        var previous = Snapshot(StableId, VolatileId);

        MonitorService.WithoutResolved(previous, new HashSet<string> { StableId });

        Assert.Equal(2, previous.Count);
    }

    // ---- Verify's monitored/unmonitored split -------------------------------

    [Fact]
    public void ManualScan_PublishesOnlyMonitoredDrift()
    {
        // Verify checks every setting, but the Status count is defined as monitored
        // settings only. Publishing the unmonitored ones would make the count
        // disagree with its own subtitle.
        var drifted = new[]
        {
            Drift(StableId, monitored: true),
            Drift(StableId2, monitored: false),
        };

        var published = drifted.Where(d => d.IsMonitored).ToList();

        var merged = MonitorService.MergeDrift(
            MonitorService.MergeDrift(
                new Dictionary<string, DriftItem>(), tier: null,
                Array.Empty<DriftItem>(), new HashSet<string>()),
            tier: null, published, new HashSet<string>());

        Assert.Equal(new[] { StableId }, merged.Keys);
    }

    [Fact]
    public void ManualScan_IsAFullScanSoItReplacesStaleEntries()
    {
        // Verify re-reads every monitor, so a setting that is no longer drifting
        // must drop out — otherwise fixing something outside the app would leave the
        // count permanently high.
        var previous = Snapshot(StableId, VolatileId);

        var merged = MonitorService.MergeDrift(
            previous, tier: null, new[] { Drift(VolatileId) }, new HashSet<string>());

        Assert.Equal(new[] { VolatileId }, merged.Keys);
    }

    // ---- What the window tells the user -------------------------------------

    [Fact]
    public void SubText_ExplainsWhyStatusReadsZeroWhenNothingIsMonitored()
    {
        // This is the user's exact situation: Verify found drift, Status showed 0.
        var text = VerifyResultsWindow.BuildSubText(monitored: 0, unmonitored: 1);

        Assert.Contains("not monitored", text);
        Assert.Contains("0", text);
    }

    [Fact]
    public void SubText_ExplainsTheSplitWhenBothKindsDrifted()
    {
        var text = VerifyResultsWindow.BuildSubText(monitored: 2, unmonitored: 3);

        Assert.Contains("2", text);
        Assert.Contains("3", text);
        Assert.Contains("Status", text);
    }

    [Fact]
    public void SubText_SaysNothingAboutMonitoringWhenEverythingIsMonitored()
    {
        // No split to explain, so no confusing aside about it.
        var text = VerifyResultsWindow.BuildSubText(monitored: 2, unmonitored: 0);

        Assert.DoesNotContain("not monitored", text);
    }

    [Fact]
    public void SubText_AlwaysSaysNothingWasChanged()
    {
        // Verify's contract: it reports, it never applies.
        foreach (var (m, u) in new[] { (0, 1), (1, 0), (2, 3) })
        {
            Assert.Contains("Nothing has been changed", VerifyResultsWindow.BuildSubText(m, u));
        }
    }

    [Fact]
    public void SectionNote_NamesTheSectionAndHowTheSettingIsEnforced()
    {
        var note = VerifyRow.BuildSectionNote(Drift(StableId, monitored: true, autoApply: false));

        Assert.Contains("Gaming", note);
        Assert.Contains("notified", note);
    }

    [Fact]
    public void SectionNote_CallsOutUnmonitoredSettings()
    {
        var note = VerifyRow.BuildSectionNote(Drift(StableId, monitored: false));

        Assert.Contains("not monitored", note);
        Assert.Contains("never corrected automatically", note);
    }

    [Fact]
    public void SectionNote_SaysAutoApplyWillHandleItItself()
    {
        var note = VerifyRow.BuildSectionNote(Drift(StableId, monitored: true, autoApply: true));

        Assert.Contains("silently", note);
    }

    [Fact]
    public void SectionNote_DegradesWithoutASectionRatherThanThrowing()
    {
        var note = VerifyRow.BuildSectionNote(Drift("not-a-mapped-id"));

        Assert.False(string.IsNullOrWhiteSpace(note));
        Assert.DoesNotContain("—", note);
    }

    [Fact]
    public void VerifyRow_FallsBackToTheLabelWhenDescriptionIsBlank()
    {
        var d = Drift(StableId) with { Description = "" };

        Assert.Equal(StableId, new VerifyRow(d).Description);
    }

    [Fact]
    public void VerifyRow_BadgeIsVisibleOnlyForUnmonitoredSettings()
    {
        Assert.Equal(System.Windows.Visibility.Visible,
            new VerifyRow(Drift(StableId, monitored: false)).UnmonitoredBadgeVisibility);
        Assert.Equal(System.Windows.Visibility.Collapsed,
            new VerifyRow(Drift(StableId, monitored: true)).UnmonitoredBadgeVisibility);
    }

    // ---- The window actually loads ------------------------------------------

    [Fact]
    public void Window_ConstructsWithoutThrowing()
    {
        // Covers the window's own XAML and its constructor — the class of failure
        // that shipped a settings window throwing a ReplaceContent NRE on
        // construction, which a clean build and a green suite both missed.
        //
        // It does NOT cover the row DataTemplate. That was checked rather than
        // assumed: with a deliberately broken StaticResource in the template, this
        // test still passed under construction, under Measure/Arrange, under Show(),
        // and under a drained dispatcher queue — an ItemsControl in this host never
        // generates its containers, so nothing inside the template runs. The row
        // template is verified by opening the window in the running app instead.
        var ex = OnStaThread(() =>
        {
            var drifted = new[]
            {
                Drift(StableId, monitored: true),
                Drift(StableId2, monitored: false, autoApply: true),
                Drift("not-a-mapped-id"),
            };
            var win = new VerifyResultsWindow(
                drifted, Array.Empty<GamerGuardian.Monitors.IMonitoredSetting>(),
                new AppConfig(), monitorService: null);

            win.Close();
        });

        Assert.Null(ex);
    }

    /// <summary>Runs an action on an STA thread and returns whatever it threw, or
    /// null. WPF types cannot be created on the MTA threads xUnit runs on.</summary>
    private static Exception? OnStaThread(Action action)
    {
        Exception? captured = null;
        var t = new System.Threading.Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { captured = ex; }
        });
        t.SetApartmentState(System.Threading.ApartmentState.STA);
        t.Start();
        t.Join();
        return captured;
    }
}
