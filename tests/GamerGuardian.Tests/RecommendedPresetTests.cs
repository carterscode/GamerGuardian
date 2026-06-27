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

    // The power plan is isolated from every preset: it's a one-time, user-initiated
    // setup on the CPU / Power tab. No preset button may stage, monitor, or revert it.

    [Fact]
    public void Apply_DoesNotTouchPowerPlan()
    {
        var cfg = new AppConfig();
        RecommendedPreset.ApplyToDraft(cfg, DualCcd());

        Assert.Null(cfg.Global.PowerPlan.DesiredGuid);
        Assert.False(cfg.Global.PowerPlan.Monitor);
        Assert.False(cfg.Global.PowerPlan.AutoApply);
    }

    [Fact]
    public void ApplyExtreme_DoesNotTouchPowerPlan()
    {
        var cfg = new AppConfig();
        RecommendedPreset.ApplyExtremeToDraft(cfg, SingleCcd());

        Assert.Null(cfg.Global.PowerPlan.DesiredGuid);
        Assert.False(cfg.Global.PowerPlan.Monitor);
        Assert.False(cfg.Global.PowerPlan.AutoApply);
    }

    [Fact]
    public void Reset_LeavesUserPowerPlanSetupAlone()
    {
        // A user who configured the power plan manually must keep that setup across
        // a Reset -- the power plan is owned outside the presets.
        var cfg = new AppConfig();
        cfg.Global.PowerPlan.Desired = PowerPlanChoice.HighPerformance;
        cfg.Global.PowerPlan.DesiredGuid = PowerPlanMonitor.HighPerformance.ToString();
        cfg.Global.PowerPlan.Monitor = true;
        cfg.Global.PowerPlan.AutoApply = true;

        RecommendedPreset.ResetToDefaultsToDraft(cfg);

        Assert.Equal(PowerPlanChoice.HighPerformance, cfg.Global.PowerPlan.Desired);
        Assert.Equal(PowerPlanMonitor.HighPerformance.ToString(), cfg.Global.PowerPlan.DesiredGuid);
        Assert.True(cfg.Global.PowerPlan.Monitor);
        Assert.True(cfg.Global.PowerPlan.AutoApply);
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

    // ---- Recommendation maps cover every toggle ----------------------------

    [Fact]
    public void ExtremeAndDefaultMaps_CoverExactlyTheRecommendedToggleSet()
    {
        var recKeys = SettingRecommendations.ToggleDesiredOn.Keys.OrderBy(k => k).ToArray();
        var extremeKeys = SettingRecommendations.ExtremeDesiredOn.Keys.OrderBy(k => k).ToArray();
        var defaultKeys = SettingRecommendations.WindowsDefaultDesiredOn.Keys.OrderBy(k => k).ToArray();
        Assert.Equal(recKeys, extremeKeys);
        Assert.Equal(recKeys, defaultKeys);
    }

    [Fact]
    public void ExtremeMap_FlipsTheFourSoftenedSettings_KeepsTheRest()
    {
        var rec = SettingRecommendations.ToggleDesiredOn;
        var ext = SettingRecommendations.ExtremeDesiredOn;

        // The four the standard recommendation deliberately softens go aggressive.
        Assert.False(ext["memintegrity"]);    // off
        Assert.False(ext["vbs"]);             // off
        Assert.True(ext["network.nagle"]);    // disable Nagle
        Assert.True(ext["network.nicpower"]); // disable NIC power saving

        // Everything else matches the standard recommendation (already max-gaming).
        foreach (var (id, on) in rec)
        {
            if (id is "memintegrity" or "vbs" or "network.nagle" or "network.nicpower") continue;
            Assert.Equal(on, ext[id]);
        }
    }

    // ---- Extreme preset ----------------------------------------------------

    [Fact]
    public void ApplyExtreme_FreshConfig_TurnsOnMonitorAndAutoApplyForEverySetting()
    {
        var cfg = new AppConfig();
        var result = RecommendedPreset.ApplyExtremeToDraft(cfg, SingleCcd());
        Assert.True(result.SettingsChanged > 0);

        // Spot-check across every category: Monitor + Auto-apply on everywhere.
        foreach (var pref in new[]
                 {
                     cfg.Global.GameMode, cfg.Global.MemoryIntegrity, cfg.Global.Vbs,
                     cfg.Global.PowerThrottling, cfg.Global.Copilot, cfg.Global.AdvertisingId,
                     cfg.Global.SuggestedContent, cfg.Global.Nagle, cfg.Global.NicPower,
                 })
        {
            Assert.True(pref.Monitor, "Extreme must turn Monitor on for every setting");
            Assert.True(pref.AutoApply, "Extreme must turn Auto-apply on for every setting");
        }
    }

    [Fact]
    public void ApplyExtreme_DisablesSecurityAndPushesContestedNetworkTweaks()
    {
        var cfg = new AppConfig();
        RecommendedPreset.ApplyExtremeToDraft(cfg, SingleCcd());

        Assert.False(cfg.Global.MemoryIntegrity.DesiredOn); // off (gaming)
        Assert.False(cfg.Global.Vbs.DesiredOn);             // off (gaming)
        Assert.True(cfg.Global.Nagle.DesiredOn);            // disable Nagle (aggressive)
        Assert.True(cfg.Global.NicPower.DesiredOn);         // disable NIC power saving

        // And it reaches the settings the standard preset leaves alone.
        Assert.False(cfg.Global.SuggestedContent.DesiredOn); // debloat applied
        Assert.False(cfg.Global.AdvertisingId.DesiredOn);    // privacy applied
        Assert.True(cfg.Global.PowerThrottling.DesiredOn);   // system toggle applied (gaming)
    }

    [Fact]
    public void ApplyExtreme_IsIdempotent()
    {
        var cfg = new AppConfig();
        var first = RecommendedPreset.ApplyExtremeToDraft(cfg, SingleCcd());
        Assert.True(first.SettingsChanged > 0);

        var second = RecommendedPreset.ApplyExtremeToDraft(cfg, SingleCcd());
        Assert.Equal(0, second.SettingsChanged);
    }

    [Fact]
    public void ApplyExtreme_NullDraft_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => RecommendedPreset.ApplyExtremeToDraft(null!));
    }

    [Fact]
    public void ApplyExtreme_DualCcd_StillProtectsCcdRoutingServices()
    {
        var cfg = new AppConfig();
        RecommendedPreset.ApplyExtremeToDraft(cfg, DualCcd());
        foreach (var (name, pref) in cfg.Services)
        {
            if (RecommendedPreset.ShouldProtectServiceOnDualCcd(name, DualCcd()))
                Assert.NotEqual(ServiceTargetState.Disabled, pref.Desired);
        }
    }

    // ---- Reset-to-defaults preset ------------------------------------------

    [Fact]
    public void Reset_StagesWindowsDefaults_AndTurnsOffMonitorAndAutoApply()
    {
        // Start from a fully gaming-optimized config, then reset.
        var cfg = new AppConfig();
        RecommendedPreset.ApplyExtremeToDraft(cfg, SingleCcd());

        RecommendedPreset.ResetToDefaultsToDraft(cfg);

        // Windows-default Want values restored.
        Assert.True(cfg.Global.GameMode.DesiredOn);        // on by default
        Assert.True(cfg.Global.GameDvr.DesiredOn);         // capture on by default
        Assert.True(cfg.Global.MemoryIntegrity.DesiredOn); // security on by default
        Assert.True(cfg.Global.Copilot.DesiredOn);         // AI on by default
        Assert.True(cfg.Global.AdvertisingId.DesiredOn);   // ad ID on by default
        Assert.True(cfg.Global.SuggestedContent.DesiredOn);// bloat on by default
        Assert.False(cfg.Global.PowerThrottling.DesiredOn);// throttling on = Default
        Assert.False(cfg.Global.Nagle.DesiredOn);          // Nagle on = Default

        // Monitor + Auto-apply off across the board.
        foreach (var pref in new[]
                 {
                     cfg.Global.GameMode, cfg.Global.MemoryIntegrity, cfg.Global.Copilot,
                     cfg.Global.AdvertisingId, cfg.Global.SuggestedContent, cfg.Global.Nagle,
                 })
        {
            Assert.False(pref.Monitor, "Reset must turn Monitor off");
            Assert.False(pref.AutoApply, "Reset must turn Auto-apply off");
        }
    }

    [Fact]
    public void Reset_ResetsManagedServicesToDefaultUnmanaged()
    {
        var cfg = new AppConfig();
        RecommendedPreset.ApplyExtremeToDraft(cfg, SingleCcd());
        Assert.NotEmpty(cfg.Services); // extreme materialized service prefs

        RecommendedPreset.ResetToDefaultsToDraft(cfg);

        foreach (var (_, pref) in cfg.Services)
        {
            Assert.Equal(ServiceTargetState.Default, pref.Desired);
            Assert.False(pref.Monitor);
            Assert.False(pref.AutoApply);
        }
    }

    [Fact]
    public void Reset_StopsMonitoringDisplays()
    {
        var cfg = new AppConfig();
        cfg.Displays["DISPLAY-A"] = new DisplayPreference { DisplayLabel = "Monitor A" };
        RecommendedPreset.ApplyExtremeToDraft(cfg, SingleCcd());

        RecommendedPreset.ResetToDefaultsToDraft(cfg);

        var d = cfg.Displays["DISPLAY-A"];
        Assert.False(d.Hdr.Monitor);
        Assert.False(d.Hdr.AutoApply);
        Assert.False(d.RefreshRate.Monitor);
        Assert.False(d.RefreshRate.AutoApply);
    }

    [Fact]
    public void Reset_IsIdempotent()
    {
        var cfg = new AppConfig();
        RecommendedPreset.ApplyExtremeToDraft(cfg, SingleCcd());
        var first = RecommendedPreset.ResetToDefaultsToDraft(cfg);
        Assert.True(first.SettingsChanged > 0);

        var second = RecommendedPreset.ResetToDefaultsToDraft(cfg);
        Assert.Equal(0, second.SettingsChanged);
    }

    [Fact]
    public void Reset_NullDraft_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => RecommendedPreset.ResetToDefaultsToDraft(null!));
    }

    // ---- Application Experience scheduled tasks --------------------------------

    [Fact]
    public void Apply_StagesAllApplicationExperienceTasksDisabled()
    {
        var cfg = new AppConfig();
        RecommendedPreset.ApplyToDraft(cfg, DualCcd());

        foreach (var def in ScheduledTaskCatalog.All)
        {
            Assert.True(cfg.ScheduledTasks.ContainsKey(def.TaskPath), $"missing {def.TaskPath}");
            var pref = cfg.ScheduledTasks[def.TaskPath];
            Assert.Equal(ScheduledTaskTarget.Disabled, pref.Desired);
            Assert.True(pref.Monitor);
            Assert.True(pref.AutoApply);
        }
    }

    [Fact]
    public void ApplyExtreme_StagesAllApplicationExperienceTasksDisabled()
    {
        var cfg = new AppConfig();
        RecommendedPreset.ApplyExtremeToDraft(cfg, SingleCcd());

        foreach (var def in ScheduledTaskCatalog.All)
        {
            Assert.True(cfg.ScheduledTasks.TryGetValue(def.TaskPath, out var pref), $"missing {def.TaskPath}");
            Assert.Equal(ScheduledTaskTarget.Disabled, pref!.Desired);
            Assert.True(pref.Monitor && pref.AutoApply);
        }
    }

    [Fact]
    public void Reset_UnmanagesScheduledTasks()
    {
        var cfg = new AppConfig();
        RecommendedPreset.ApplyToDraft(cfg, DualCcd());     // stage them disabled + monitored
        RecommendedPreset.ResetToDefaultsToDraft(cfg);

        Assert.NotEmpty(cfg.ScheduledTasks);
        foreach (var (_, pref) in cfg.ScheduledTasks)
        {
            Assert.Equal(ScheduledTaskTarget.Default, pref.Desired);
            Assert.False(pref.Monitor);
            Assert.False(pref.AutoApply);
        }
    }
}
