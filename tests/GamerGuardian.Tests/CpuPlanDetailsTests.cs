using System;
using System.Linq;
using GamerGuardian.Models;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

/// <summary>
/// Covers the plan-naming and plan-details surface: the custom scheme name reveals
/// its Windows base, and the details view accurately describes the diff-from-stock
/// and a CPU-appropriate rationale.
/// </summary>
public class CpuPlanDetailsTests
{
    private static CpuTuneResult Amd(string model) =>
        CpuTuneCatalog.Resolve(CpuDetector.Parse($"AMD Ryzen 9 {model} Processor", "AuthenticAMD", ""));

    private static CpuTuneResult Intel(string name) =>
        CpuTuneCatalog.Resolve(CpuDetector.Parse(name, "GenuineIntel", ""));

    private static CpuTuneResult Generic() =>
        CpuTuneCatalog.Resolve(CpuInfo.Unknown("Some Weird CPU"));

    // ---- Plan name reveals the base (ask #1) ----

    [Fact]
    public void PlanName_IncludesModelAndBase()
    {
        var r = Amd("9950X3D");
        Assert.Contains("9950X3D", r.PlanName);
        Assert.Contains("Balanced", r.PlanName);
        Assert.StartsWith("GamerGuardian Gaming [", r.PlanName);
    }

    [Fact]
    public void PlanName_KeepsBuilderPrefix_SoDeleteGuardStillMatches()
    {
        // MaySafelyDelete keys on this prefix; the base suffix must not break it.
        Assert.StartsWith(Services.CpuPlanBuilder.PlanNamePrefix, Amd("9800X3D").PlanName);
    }

    [Fact]
    public void BasePlan_IsBalanced()
    {
        var r = Amd("9950X3D");
        Assert.Equal(PowerPlanChoice.Balanced, r.BasePlan);
        Assert.Equal("Balanced", r.BasePlanDisplayName);
    }

    // ---- Base summary + change list (ask #2: what it changes vs stock) ----

    [Fact]
    public void BaseSummary_NamesTheBaseWindowsPlan()
    {
        Assert.Contains("Balanced", CpuPlanDetails.BaseSummary(Amd("9950X3D")));
    }

    [Fact]
    public void Changes_AreTheRecipeOverrideLabels()
    {
        var r = Amd("9950X3D");
        var changes = CpuPlanDetails.Changes(r);
        Assert.Equal(r.Overrides.Select(o => o.Label), changes);
        Assert.Contains(changes, c => c.Contains("min cores", StringComparison.OrdinalIgnoreCase));
    }

    // ---- Rationale is CPU-appropriate (ask #2: why it's better for your CPU) ----

    [Fact]
    public void Rationale_AsymmetricDual_MentionsParkingAndCluster()
    {
        var text = CpuPlanDetails.Rationale(Amd("9950X3D"));
        Assert.Contains("park", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cluster", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rationale_SingleCcdX3d_MentionsVCache_NoParking()
    {
        var text = CpuPlanDetails.Rationale(Amd("9800X3D"));
        Assert.Contains("V-Cache", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("disables core parking", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rationale_IntelHybrid_DefersToThreadDirector()
    {
        var text = CpuPlanDetails.Rationale(Intel("Intel(R) Core(TM) Ultra 9 285K"));
        Assert.Contains("Thread Director", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rationale_Generic_IsConservative()
    {
        var text = CpuPlanDetails.Rationale(Generic());
        Assert.Contains("conservative", text, StringComparison.OrdinalIgnoreCase);
    }

    // ---- The 9950X3D "High Performance" default bug (root cause) ----

    [Fact]
    public void PowerPlanPref_DefaultsToBalanced_NotHighPerformance()
    {
        Assert.Equal(PowerPlanChoice.Balanced, new PowerPlanPref().Desired);
    }

    [Fact]
    public void ResolveDesiredGuid_FreshPref_ResolvesToBalanced()
    {
        // A fresh install with no explicit pick must preselect Balanced (matches the
        // "Recommended: Balanced" hint), not High Performance.
        var guid = GamerGuardian.Monitors.PowerPlanMonitor.ResolveDesiredGuid(new PowerPlanPref());
        Assert.Equal(GamerGuardian.Monitors.PowerPlanMonitor.Balanced, guid);
    }
}
