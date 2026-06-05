using System.Text.Json;
using GamerGuardian.Models;
using GamerGuardian.Monitors;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class CpuPlanBuilderTests
{
    private const string MachineA = "machine-a";
    private const string MachineB = "machine-b";

    private static CpuTuneResult Recipe(string model) =>
        CpuTuneCatalog.Resolve(CpuDetector.Parse($"AMD Ryzen 9 {model} Processor", "AuthenticAMD", ""));

    private static InstalledPlan Balanced => new(PowerPlanMonitor.Balanced, "Balanced");

    [Fact]
    public void Decide_NoStored_NoneInstalled_Create()
    {
        var plan = CpuPlanBuilder.Decide(new CpuPlanPref(), Recipe("9850X3D"), MachineA, new[] { Balanced });
        Assert.Equal(BuildDecision.Create, plan.Decision);
        Assert.Null(plan.ExistingGuid);
    }

    [Fact]
    public void Decide_StoredValid_HashMatches_ReuseExisting()
    {
        var recipe = Recipe("9850X3D");
        var g = Guid.NewGuid();
        var pref = new CpuPlanPref
        {
            BuiltSchemeGuid = g.ToString(),
            ContentHash = recipe.ContentHash,
            MachineToken = MachineA,
        };
        var installed = new[] { Balanced, new InstalledPlan(g, recipe.PlanName) };

        var plan = CpuPlanBuilder.Decide(pref, recipe, MachineA, installed);

        Assert.Equal(BuildDecision.ReuseExisting, plan.Decision);
        Assert.Equal(g, plan.ExistingGuid);
    }

    [Fact]
    public void Decide_StoredValid_HashDiffers_ReTune()
    {
        var recipe = Recipe("9850X3D");
        var g = Guid.NewGuid();
        var pref = new CpuPlanPref
        {
            BuiltSchemeGuid = g.ToString(),
            ContentHash = "STALE-HASH",
            MachineToken = MachineA,
        };
        var installed = new[] { Balanced, new InstalledPlan(g, recipe.PlanName) };

        var plan = CpuPlanBuilder.Decide(pref, recipe, MachineA, installed);

        Assert.Equal(BuildDecision.ReTune, plan.Decision);
        Assert.Equal(g, plan.ExistingGuid);
    }

    [Fact]
    public void Decide_StoredGuidNotInstalled_Create_TreatedAsNoPlan()
    {
        var recipe = Recipe("9850X3D");
        var pref = new CpuPlanPref
        {
            BuiltSchemeGuid = Guid.NewGuid().ToString(),
            ContentHash = recipe.ContentHash,
            MachineToken = MachineA,
        };
        var plan = CpuPlanBuilder.Decide(pref, recipe, MachineA, new[] { Balanced });

        Assert.Equal(BuildDecision.Create, plan.Decision);
    }

    [Fact]
    public void Decide_NoStored_OrphanWithSameName_Adopts_NotDuplicate()
    {
        var recipe = Recipe("9850X3D");
        var orphan = Guid.NewGuid();
        var installed = new[] { Balanced, new InstalledPlan(orphan, recipe.PlanName) };

        var plan = CpuPlanBuilder.Decide(new CpuPlanPref(), recipe, MachineA, installed);

        Assert.Equal(BuildDecision.ReTune, plan.Decision);
        Assert.Equal(orphan, plan.ExistingGuid);
    }

    [Fact]
    public void Decide_ForeignMachineToken_NeverTargetsForeign_ReconcilesLocally()
    {
        var recipe = Recipe("9850X3D");
        var foreign = Guid.NewGuid();   // stored, built on machine B, not installed here
        var local = Guid.NewGuid();     // a local GG plan with the same name
        var pref = new CpuPlanPref
        {
            BuiltSchemeGuid = foreign.ToString(),
            ContentHash = recipe.ContentHash,
            MachineToken = MachineB,
        };
        var installed = new[] { Balanced, new InstalledPlan(local, recipe.PlanName) };

        var plan = CpuPlanBuilder.Decide(pref, recipe, MachineA, installed);

        Assert.Equal(BuildDecision.ReTune, plan.Decision);
        Assert.Equal(local, plan.ExistingGuid);
        Assert.NotEqual(foreign, plan.ExistingGuid);
    }

    [Fact]
    public void Decide_CatalogRefactor_SameHash_ReuseExisting_NoChurn()
    {
        var recipe = Recipe("9850X3D");
        var g = Guid.NewGuid();
        var pref = new CpuPlanPref
        {
            BuiltSchemeGuid = g.ToString(),
            ContentHash = recipe.ContentHash, // identical override set -> same hash
            MachineToken = MachineA,
        };
        var installed = new[] { Balanced, new InstalledPlan(g, recipe.PlanName) };

        Assert.Equal(BuildDecision.ReuseExisting,
            CpuPlanBuilder.Decide(pref, recipe, MachineA, installed).Decision);
    }

    [Fact]
    public void MaySafelyDelete_TrueOnlyWithPositiveIdentity()
    {
        var gg = Guid.NewGuid();
        var renamed = Guid.NewGuid();
        var installed = new[]
        {
            Balanced,
            new InstalledPlan(gg, "GamerGuardian Gaming [9850X3D]"),
            new InstalledPlan(renamed, "My Custom Plan"),
        };

        Assert.True(CpuPlanBuilder.MaySafelyDelete(gg, installed));
        Assert.False(CpuPlanBuilder.MaySafelyDelete(PowerPlanMonitor.Balanced, installed));           // Microsoft
        Assert.False(CpuPlanBuilder.MaySafelyDelete(PowerPlanMonitor.HighPerformance, installed));    // Microsoft
        Assert.False(CpuPlanBuilder.MaySafelyDelete(PowerPlanMonitor.PowerSaver, installed));         // Microsoft
        Assert.False(CpuPlanBuilder.MaySafelyDelete(PowerPlanMonitor.UltimatePerformance, installed));// Microsoft
        Assert.False(CpuPlanBuilder.MaySafelyDelete(renamed, installed));                   // not GG-named
        Assert.False(CpuPlanBuilder.MaySafelyDelete(Guid.NewGuid(), installed));            // not installed
        Assert.False(CpuPlanBuilder.MaySafelyDelete(Guid.Empty, installed));
    }

    [Fact]
    public void Decide_NullMachineToken_StoredGuidInstalled_StillReuses()
    {
        // Compatibility path: a config from before machine-token binding has a
        // null token; the foreign-machine guard must be skipped, not block reuse.
        var recipe = Recipe("9850X3D");
        var g = Guid.NewGuid();
        var pref = new CpuPlanPref
        {
            BuiltSchemeGuid = g.ToString(),
            ContentHash = recipe.ContentHash,
            MachineToken = null,
        };
        var installed = new[] { Balanced, new InstalledPlan(g, recipe.PlanName) };

        Assert.Equal(BuildDecision.ReuseExisting,
            CpuPlanBuilder.Decide(pref, recipe, MachineA, installed).Decision);
    }

    [Fact]
    public void Decide_DoesNotAdoptRenamedMicrosoftPlan()
    {
        // A user renamed a built-in to the GG format; adoption must refuse it.
        var recipe = Recipe("9850X3D");
        var installed = new[] { new InstalledPlan(PowerPlanMonitor.Balanced, recipe.PlanName) };

        var plan = CpuPlanBuilder.Decide(new CpuPlanPref(), recipe, MachineA, installed);

        Assert.Equal(BuildDecision.Create, plan.Decision); // not ReTune against the built-in
    }

    [Fact]
    public void Decide_MultipleOrphans_AdoptsOneNotCreate()
    {
        var recipe = Recipe("9850X3D");
        var o1 = Guid.NewGuid();
        var o2 = Guid.NewGuid();
        var installed = new[]
        {
            Balanced,
            new InstalledPlan(o1, recipe.PlanName),
            new InstalledPlan(o2, recipe.PlanName),
        };

        var plan = CpuPlanBuilder.Decide(new CpuPlanPref(), recipe, MachineA, installed);

        Assert.Equal(BuildDecision.ReTune, plan.Decision);
        Assert.Contains(plan.ExistingGuid, new[] { (Guid?)o1, o2 });
    }

    [Fact]
    public void BestPrebuilt_ReturnsRecommendedWhenInstalled_ElseEmpty()
    {
        var recipe = Recipe("9850X3D"); // recommends Balanced
        var installedWith = new[] { Balanced };
        var installedWithout = new[] { new InstalledPlan(Guid.NewGuid(), "Something else") };

        Assert.Equal(PowerPlanMonitor.Balanced, CpuPlanBuilder.BestPrebuilt(recipe, installedWith));
        Assert.Equal(Guid.Empty, CpuPlanBuilder.BestPrebuilt(recipe, installedWithout));
    }

    [Fact]
    public void CpuPlanPref_RoundTripsThroughJson_UnderGlobal()
    {
        var cfg = new AppConfig();
        cfg.Global.CpuPlan.BuiltSchemeGuid = Guid.NewGuid().ToString();
        cfg.Global.CpuPlan.ContentHash = "ABCD1234";
        cfg.Global.CpuPlan.MachineToken = MachineA;
        cfg.Global.CpuPlan.CpuModel = "9850X3D";

        var json = JsonSerializer.Serialize(cfg);
        var back = JsonSerializer.Deserialize<AppConfig>(json)!;

        Assert.Equal(cfg.Global.CpuPlan.BuiltSchemeGuid, back.Global.CpuPlan.BuiltSchemeGuid);
        Assert.Equal("ABCD1234", back.Global.CpuPlan.ContentHash);
        Assert.Equal(MachineA, back.Global.CpuPlan.MachineToken);
        Assert.Equal("9850X3D", back.Global.CpuPlan.CpuModel);
    }
}
