using GamerGuardian.Models;
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

    [Fact]
    public void Apply_PowerPlan_SetsPlanFromCpuAwareLogic_OrSkipsIfPlanMissing()
    {
        // Hard to test the CPU-aware GUID selection deterministically without
        // mocking CpuInfo + PowerPlanMonitor.ListAvailablePlans. Smoke test:
        // verify that IF the preset touched the power plan, the chosen GUID
        // matches one of the two well-known plans (Balanced for X3D, High
        // Performance otherwise). If neither plan is installed locally, the
        // preset skips and DesiredGuid stays null.
        var cfg = new AppConfig();
        RecommendedPreset.ApplyToDraft(cfg);
        var guid = cfg.Global.PowerPlan.DesiredGuid;
        if (!string.IsNullOrEmpty(guid))
        {
            Assert.True(
                string.Equals(guid, "381b4222-f694-41f0-9685-ff5bb260df2e", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(guid, "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", StringComparison.OrdinalIgnoreCase),
                $"PowerPlan.DesiredGuid was set to '{guid}' which is neither Balanced nor High Performance");
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
