using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GamerGuardian.Models;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

/// <summary>
/// Covers <see cref="MonitorService.SelectNotifiable"/> — the pure rule that
/// decides which drifted settings raise an on-screen notification. The key
/// guarantee under test: "silent means silent" — a setting the user set to
/// auto-apply must never notify, on any tick, for any reason.
/// </summary>
public class MonitorServiceTests
{
    private static DriftItem Drift(string id, bool autoApply) => new(
        SettingId: id,
        DisplayKey: "global",
        DisplayLabel: "Global",
        Description: id,
        CurrentValue: "On",
        DesiredValue: "Off",
        AutoApply: autoApply,
        Apply: () => Task.CompletedTask);

    private static bool NeverTripped(string _) => false;

    [Fact]
    public void NotifyOnly_Setting_IsNotified()
    {
        var drifted = new[] { Drift("hags", autoApply: false) };
        var result = MonitorService.SelectNotifiable(drifted, NeverTripped);
        Assert.Single(result);
        Assert.Equal("hags", result[0].SettingId);
    }

    [Fact]
    public void AutoApply_Setting_IsNeverNotified()
    {
        // The whole point: a "silently change" setting must not produce a toast.
        var drifted = new[] { Drift("vrr", autoApply: true) };
        Assert.Empty(MonitorService.SelectNotifiable(drifted, NeverTripped));
    }

    [Fact]
    public void AutoApply_Setting_IsNotNotified_EvenWhenNotBeingApplied()
    {
        // This models the verify-backoff / breaker-cooldown case: the setting is
        // auto-apply but couldn't be applied this tick. It must STILL stay silent —
        // this was the regression where backed-off auto-apply settings leaked into
        // the notification path.
        var drifted = new[] { Drift("svc:DoSvc", autoApply: true) };
        Assert.Empty(MonitorService.SelectNotifiable(drifted, NeverTripped));
    }

    [Fact]
    public void BreakerTripped_NotifyOnly_Setting_IsSuppressed()
    {
        var drifted = new[] { Drift("hags", autoApply: false) };
        var result = MonitorService.SelectNotifiable(drifted, id => id == "hags");
        Assert.Empty(result);
    }

    [Fact]
    public void MixedBatch_KeepsOnlyNotifyOnlyUntrippedSettings()
    {
        var drifted = new[]
        {
            Drift("auto1", autoApply: true),    // silent -> excluded
            Drift("notify1", autoApply: false), // notify -> kept
            Drift("notify2", autoApply: false), // notify but tripped -> excluded
            Drift("auto2", autoApply: true),    // silent -> excluded
        };

        var result = MonitorService.SelectNotifiable(drifted, id => id == "notify2");

        Assert.Equal(new[] { "notify1" }, result.Select(d => d.SettingId).ToArray());
    }
}
