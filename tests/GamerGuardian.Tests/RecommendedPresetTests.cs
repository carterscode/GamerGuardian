using GamerGuardian.Models;
using GamerGuardian.Monitors;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class RecommendedPresetTests
{
    [Fact]
    public void Apply_FreshConfig_StagesEveryCoveredSetting()
    {
        var cfg = new AppConfig();
        var result = RecommendedPreset.ApplyToDraft(cfg);

        // Fresh config has Monitor=false and AutoApply=false everywhere, so
        // every preset-covered setting should register as "changed".
        Assert.True(result.SettingsChanged > 0);
        Assert.True(cfg.Global.GameMode.Monitor);
        Assert.True(cfg.Global.GameMode.AutoApply);
        Assert.True(cfg.Global.GameMode.DesiredOn);  // gaming-recommended = On
        Assert.False(cfg.Global.GameDvr.DesiredOn);  // gaming-recommended = Off
        Assert.True(cfg.Global.GameDvr.Monitor);
        Assert.True(cfg.Global.GameDvr.AutoApply);
    }

    [Fact]
    public void Apply_TurnsOffEveryWindowsAiToggle()
    {
        var cfg = new AppConfig();
        RecommendedPreset.ApplyToDraft(cfg);

        // The preset's "max performance" interpretation = AI off across the board.
        Assert.False(cfg.Global.Copilot.DesiredOn);
        Assert.False(cfg.Global.Recall.DesiredOn);
        Assert.False(cfg.Global.ClickToDo.DesiredOn);
        Assert.False(cfg.Global.EdgeAi.DesiredOn);
        Assert.False(cfg.Global.NotepadPaintAi.DesiredOn);
        Assert.False(cfg.Global.SettingsSearchAi.DesiredOn);
        Assert.False(cfg.Global.AiActions.DesiredOn);
        Assert.False(cfg.Global.InputInsights.DesiredOn);
        Assert.False(cfg.Global.OfficeCopilot.DesiredOn);
        // And every AI toggle is monitored + auto-applied
        Assert.True(cfg.Global.Copilot.Monitor && cfg.Global.Copilot.AutoApply);
        Assert.True(cfg.Global.OfficeCopilot.Monitor && cfg.Global.OfficeCopilot.AutoApply);
    }

    [Fact]
    public void Apply_LeavesMemoryIntegrityAlone()
    {
        // MemoryIntegrity is deliberately not in the preset (security tradeoff).
        // Fresh AppConfig has it at DesiredOn=false, Monitor=false.
        // After the preset, those should be unchanged.
        var cfg = new AppConfig();
        cfg.Global.MemoryIntegrity.DesiredOn = true;  // user set it however they like
        cfg.Global.MemoryIntegrity.Monitor = true;
        cfg.Global.MemoryIntegrity.AutoApply = false;

        RecommendedPreset.ApplyToDraft(cfg);

        Assert.True(cfg.Global.MemoryIntegrity.DesiredOn);
        Assert.True(cfg.Global.MemoryIntegrity.Monitor);
        Assert.False(cfg.Global.MemoryIntegrity.AutoApply);
    }

    [Fact]
    public void Apply_LeavesVbsAlone()
    {
        // The full-VBS-stack toggle is deliberately not in the preset, same as
        // MemoryIntegrity: a one-button preset that disables Credential Guard and
        // breaks Valorant would surprise users.
        var cfg = new AppConfig();
        cfg.Global.Vbs.DesiredOn = true;
        cfg.Global.Vbs.Monitor = true;
        cfg.Global.Vbs.AutoApply = false;

        RecommendedPreset.ApplyToDraft(cfg);

        Assert.True(cfg.Global.Vbs.DesiredOn);
        Assert.True(cfg.Global.Vbs.Monitor);
        Assert.False(cfg.Global.Vbs.AutoApply);
    }

    [Fact]
    public void Apply_IsIdempotent()
    {
        var cfg = new AppConfig();
        var first = RecommendedPreset.ApplyToDraft(cfg);
        Assert.True(first.SettingsChanged > 0);

        // Second run on the same draft should find everything already correct.
        var second = RecommendedPreset.ApplyToDraft(cfg);
        Assert.Equal(0, second.SettingsChanged);
        Assert.Equal(first.SettingsChanged + first.SettingsAlreadyCorrect, second.SettingsAlreadyCorrect);
    }

    [Fact]
    public void Apply_OnlyMutatesFieldsThatDiffer()
    {
        // Pre-set a few fields to the recommended state; others not. The preset
        // should report fewer changes than a fresh config would.
        var cfg = new AppConfig();
        cfg.Global.GameMode.DesiredOn = true;
        cfg.Global.GameMode.Monitor = true;
        cfg.Global.GameMode.AutoApply = true;
        // Hags is already DesiredOn=true by default; set Monitor + AutoApply
        cfg.Global.Hags.Monitor = true;
        cfg.Global.Hags.AutoApply = true;

        var result = RecommendedPreset.ApplyToDraft(cfg);
        var fresh = RecommendedPreset.ApplyToDraft(new AppConfig());
        Assert.True(result.SettingsChanged < fresh.SettingsChanged,
            "Pre-configured fields should reduce the count of changes");
    }

    [Fact]
    public void Apply_SetsServiceTargetsForServicesWithRecommendedTarget()
    {
        var cfg = new AppConfig();
        RecommendedPreset.ApplyToDraft(cfg);

        // Every ServiceCatalog entry with a RecommendedTarget should have an
        // entry in cfg.Services pointing at that target with Monitor+AutoApply on.
        foreach (var def in ServiceCatalog.All)
        {
            if (def.RecommendedTarget is not { } target) continue;
            Assert.True(cfg.Services.ContainsKey(def.Name), $"Missing service pref entry: {def.Name}");
            var pref = cfg.Services[def.Name];
            Assert.Equal(target, pref.Desired);
            Assert.True(pref.Monitor);
            Assert.True(pref.AutoApply);
        }
    }

    [Fact]
    public void Apply_HandlesDisplaysIfPresent()
    {
        var cfg = new AppConfig();
        cfg.Displays["DISPLAY-A"] = new DisplayPreference { DisplayLabel = "Monitor A" };
        cfg.Displays["DISPLAY-B"] = new DisplayPreference { DisplayLabel = "Monitor B" };

        RecommendedPreset.ApplyToDraft(cfg);

        foreach (var d in cfg.Displays.Values)
        {
            Assert.True(d.Hdr.Monitor);
            Assert.True(d.Hdr.AutoApply);
            Assert.True(d.Hdr.DesiredOn);
            Assert.Equal(RefreshRateTarget.Maximum, d.RefreshRate.Target);
            Assert.True(d.RefreshRate.AutoApply);
            // Resolution NOT in preset
            Assert.False(d.Resolution.Monitor);
        }
    }

    [Fact]
    public void Apply_NullDraft_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => RecommendedPreset.ApplyToDraft(null!));
    }

    private static CpuTuneResult DualCcd() =>
        CpuTuneCatalog.Resolve(CpuDetector.Parse("AMD Ryzen 9 9950X3D 16-Core Processor", "AuthenticAMD", ""));

    private static CpuTuneResult SingleCcd() =>
        CpuTuneCatalog.Resolve(CpuDetector.Parse("AMD Ryzen 7 9800X3D 8-Core Processor", "AuthenticAMD", ""));

    private static Dictionary<Guid, string> PlansWithBalanced() =>
        new() { [PowerPlanMonitor.Balanced] = "Balanced" };

    [Fact]
    public void Apply_PowerPlan_IsCpuAware_RecommendsBalanced_NotHighPerformance()
    {
        var cfg = new AppConfig();
        // Inject an installed-plans set so the test doesn't depend on a live OS
        // power-scheme enumeration (deterministic in CI).
        RecommendedPreset.ApplyToDraft(cfg, DualCcd(), PlansWithBalanced());

        Assert.Equal(PowerPlanChoice.Balanced, cfg.Global.PowerPlan.Desired);
        Assert.NotEqual(PowerPlanChoice.HighPerformance, cfg.Global.PowerPlan.Desired);
    }

    [Fact]
    public void Apply_PowerPlan_NotInstalled_LeavesPlanAlone()
    {
        var cfg = new AppConfig();
        var before = cfg.Global.PowerPlan.Desired;
        // Recommended Balanced is not in the (empty) installed set.
        RecommendedPreset.ApplyToDraft(cfg, DualCcd(), new Dictionary<Guid, string>());

        Assert.Equal(before, cfg.Global.PowerPlan.Desired); // unchanged
        Assert.Null(cfg.Global.PowerPlan.DesiredGuid);
    }

    [Fact]
    public void Guardrail_DualCcd_ProtectsCcdRoutingServices()
    {
        var dual = DualCcd();
        Assert.True(RecommendedPreset.ShouldProtectServiceOnDualCcd("AMD3DVCacheSvc", dual));
        Assert.True(RecommendedPreset.ShouldProtectServiceOnDualCcd("AMDProvisioningPackagesSvc", dual));
        Assert.True(RecommendedPreset.ShouldProtectServiceOnDualCcd("GamingServices", dual));
    }

    [Fact]
    public void Guardrail_SingleCcd_DoesNotProtect()
    {
        var single = SingleCcd();
        Assert.False(RecommendedPreset.ShouldProtectServiceOnDualCcd("AMD3DVCacheSvc", single));
        Assert.False(RecommendedPreset.ShouldProtectServiceOnDualCcd("GamingServices", single));
    }

    [Fact]
    public void Guardrail_DualCcd_NeverDisablesProtectedNamedServicesInPreset()
    {
        // AE6: even if a protected-named service carried a Disabled recommended
        // target, the dual-CCD preset must not stage it disabled.
        var cfg = new AppConfig();
        RecommendedPreset.ApplyToDraft(cfg, DualCcd());
        foreach (var (name, pref) in cfg.Services)
        {
            if (RecommendedPreset.ShouldProtectServiceOnDualCcd(name, DualCcd()))
                Assert.NotEqual(ServiceTargetState.Disabled, pref.Desired);
        }
    }

    [Fact]
    public void Apply_DoesNotRemoveUwpAiApps()
    {
        // UWP removal is intentionally not in the preset (irreversible without Store).
        var cfg = new AppConfig();
        RecommendedPreset.ApplyToDraft(cfg);

        // None of the AI app entries should have been added by the preset itself.
        // (They may exist if the user already configured them; the preset should
        // leave them unchanged.)
        foreach (var pkg in WindowsAiAppCatalog.All)
        {
            if (cfg.WindowsAiApps.TryGetValue(pkg.PackageName, out var pref))
            {
                Assert.False(pref.DesiredRemoved,
                    $"Preset should never auto-stage UWP removal for {pkg.PackageName}");
            }
        }
    }

    [Fact]
    public void Apply_ChangeDescriptions_PopulatedForEachChange()
    {
        var cfg = new AppConfig();
        var result = RecommendedPreset.ApplyToDraft(cfg);

        Assert.NotNull(result.ChangeDescriptions);
        Assert.Equal(result.SettingsChanged, result.ChangeDescriptions.Count);
        foreach (var desc in result.ChangeDescriptions)
        {
            Assert.False(string.IsNullOrWhiteSpace(desc));
        }
    }
}
