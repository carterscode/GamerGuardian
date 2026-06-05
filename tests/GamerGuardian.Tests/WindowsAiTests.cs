using GamerGuardian.Models;
using GamerGuardian.Monitors;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

/// <summary>
/// AI feature wiring tests. We don't touch the real registry / AppX store --
/// those would need an isolated Windows VM. These tests verify the surface
/// contracts: setting IDs, SettingDocs entries, catalog completeness, prefs
/// existing in AppConfig, and CheckDrift behavior given preconstructed prefs.
/// </summary>
public class WindowsAiTests
{
    [Fact]
    public void GlobalPreferences_HasAllAiToggles()
    {
        var g = new GlobalPreferences();
        // Original 5 (v0.1.38)
        Assert.NotNull(g.Copilot);
        Assert.NotNull(g.Recall);
        Assert.NotNull(g.ClickToDo);
        Assert.NotNull(g.EdgeAi);
        Assert.NotNull(g.NotepadPaintAi);
        // v0.1.39 parity additions
        Assert.NotNull(g.SettingsSearchAi);
        Assert.NotNull(g.AiActions);
        Assert.NotNull(g.InputInsights);
        Assert.NotNull(g.OfficeCopilot);
        // Default-On so a fresh config doesn't aggressively disable anything
        // before the user opts in via Monitor=true.
        Assert.True(g.Copilot.DesiredOn);
        Assert.True(g.Recall.DesiredOn);
        Assert.True(g.ClickToDo.DesiredOn);
        Assert.True(g.EdgeAi.DesiredOn);
        Assert.True(g.NotepadPaintAi.DesiredOn);
        Assert.True(g.SettingsSearchAi.DesiredOn);
        Assert.True(g.AiActions.DesiredOn);
        Assert.True(g.InputInsights.DesiredOn);
        Assert.True(g.OfficeCopilot.DesiredOn);
        Assert.False(g.Copilot.Monitor);
        Assert.False(g.OfficeCopilot.Monitor);
    }

    [Theory]
    // v0.1.38 originals
    [InlineData("ai.copilot")]
    [InlineData("ai.recall")]
    [InlineData("ai.clicktodo")]
    [InlineData("ai.edge")]
    [InlineData("ai.notepadpaint")]
    // v0.1.39 parity additions
    [InlineData("ai.settingssearch")]
    [InlineData("ai.actions")]
    [InlineData("ai.inputinsights")]
    [InlineData("ai.office")]
    public void SettingDocs_AiPolicyIds_HaveMechanismAndVerify(string id)
    {
        Assert.False(string.IsNullOrWhiteSpace(SettingDocs.MechanismFor(id)));
        Assert.NotEqual("(unknown)", SettingDocs.MechanismFor(id));
        Assert.False(string.IsNullOrWhiteSpace(SettingDocs.VerifyCommandFor(id)));
        Assert.False(string.IsNullOrWhiteSpace(SettingDocs.ApplyCommandFor(id)));
    }

    [Theory]
    [InlineData("ai.settingssearch", "BingSearchEnabled")]
    [InlineData("ai.settingssearch", "IsDynamicSearchBoxEnabled")]
    [InlineData("ai.settingssearch", "DisableSearchBoxSuggestions")]
    [InlineData("ai.actions", "FeatureManagement")]
    [InlineData("ai.actions", "1853569164")]
    [InlineData("ai.actions", "4098520719")]
    [InlineData("ai.inputinsights", "RestrictImplicitTextCollection")]
    [InlineData("ai.inputinsights", "InsightsEnabled")]
    [InlineData("ai.office", "EnableCopilot")]
    [InlineData("ai.office", "disabletraining")]
    public void SettingDocs_ApplyCommand_NewIds_TouchExpectedKeys(string id, string expectedSubstring)
    {
        var cmd = SettingDocs.ApplyCommandFor(id);
        Assert.Contains(expectedSubstring, cmd);
    }

    [Theory]
    [InlineData("ai.recall", "TurnOffSavingSnapshots")]
    [InlineData("ai.edge", "ComposeInlineEnabled")]
    [InlineData("ai.edge", "AllowBrowsingWithCopilot")]
    [InlineData("ai.copilot", "BrandedKey")]
    [InlineData("ai.copilot", "BackgroundAccessApplications")]
    [InlineData("ai.notepadpaint", "IsSignedUpForTargetingService")]
    [InlineData("ai.notepadpaint", "Policies\\Paint")]
    public void SettingDocs_Mechanism_ExtendedMonitors_ListNewKeys(string id, string expectedSubstring)
    {
        var mech = SettingDocs.MechanismFor(id);
        Assert.Contains(expectedSubstring, mech);
    }

    [Fact]
    public void SettingDocs_ApplyCommand_Copilot_TouchesAllThreeKeys()
    {
        var cmd = SettingDocs.ApplyCommandFor("ai.copilot");
        Assert.Contains("WindowsCopilot", cmd);
        Assert.Contains("TurnOffWindowsCopilot", cmd);
        Assert.Contains("ShowCopilotButton", cmd);
        Assert.Contains("HKLM:", cmd);
        Assert.Contains("HKCU:", cmd);
    }

    [Fact]
    public void SettingDocs_ApplyCommand_Recall_TouchesBothWindowsAiValues()
    {
        var cmd = SettingDocs.ApplyCommandFor("ai.recall");
        Assert.Contains("AllowRecallEnablement", cmd);
        Assert.Contains("DisableAIDataAnalysis", cmd);
        Assert.Contains("WindowsAI", cmd);
    }

    [Fact]
    public void SettingDocs_SettingsSearchAi_UsesReliableBingSearchEnabled_NotBogusTaskbarCompanion()
    {
        // Regression guard for the recurring "Search box AI suggestions" drift
        // popup (commit 3202e9c band-aided the read; this is the real fix). The
        // authoritative mechanism is now BingSearchEnabled under
        // CurrentVersion\Search -- the value Windows 11 actually honors and that
        // reads back reliably. The bogus TaskbarCompanion value name (never
        // recognized by Explorer) must be gone from every doc surface so the
        // verify-fail/15-min-backoff/popup loop can't recur.
        var mech = SettingDocs.MechanismFor("ai.settingssearch");
        var apply = SettingDocs.ApplyCommandFor("ai.settingssearch");
        var verify = SettingDocs.VerifyCommandFor("ai.settingssearch");

        Assert.Contains("BingSearchEnabled", mech);
        Assert.Contains("BingSearchEnabled", apply);
        Assert.Contains("BingSearchEnabled", verify);
        Assert.Contains("CurrentVersion\\Search", apply);

        Assert.DoesNotContain("TaskbarCompanion", mech);
        Assert.DoesNotContain("TaskbarCompanion", apply);
        Assert.DoesNotContain("TaskbarCompanion", verify);
    }

    [Fact]
    public void SettingsSearchAiMonitor_NoDrift_WhenDesiredMatchesCurrent()
    {
        // Mirrors CopilotMonitor's live-read test: with a default config
        // (DesiredOn=true) there should be no drift on any machine whose search
        // box AI suggestions aren't already disabled. Asserts ReadCurrent and
        // CheckDrift agree -- the invariant whose violation produced the popup
        // loop -- without mutating the registry.
        var cfg = new AppConfig();
        cfg.Global.SettingsSearchAi.Monitor = true;
        var current = SettingsSearchAiMonitor.ReadCurrent();
        var drift = new SettingsSearchAiMonitor().CheckDrift(cfg).ToList();
        if (current is bool c && c == cfg.Global.SettingsSearchAi.DesiredOn)
            Assert.Empty(drift);
    }

    [Fact]
    public void SettingDocs_ApplyCommand_EdgeAi_TouchesAllThreePolicies()
    {
        var cmd = SettingDocs.ApplyCommandFor("ai.edge");
        Assert.Contains("HubsSidebarEnabled", cmd);
        Assert.Contains("CopilotPageContext", cmd);
        Assert.Contains("GenAILocalFoundationalModelSettings", cmd);
        Assert.Contains("Policies\\Microsoft\\Edge", cmd);
    }

    [Fact]
    public void SettingDocs_ApplyCommand_AiAppPrefix_EmitsRemoveAppxPackage()
    {
        var cmd = SettingDocs.ApplyCommandFor("ai.app:Microsoft.Copilot");
        Assert.Contains("Get-AppxPackage", cmd);
        Assert.Contains("Microsoft.Copilot", cmd);
        Assert.Contains("Remove-AppxPackage", cmd);
    }

    [Fact]
    public void SettingDocs_Verify_AiAppPrefix_EmitsGetAppxProbe()
    {
        var cmd = SettingDocs.VerifyCommandFor("ai.app:MicrosoftWindows.Client.AIX");
        Assert.Contains("Get-AppxPackage", cmd);
        Assert.Contains("MicrosoftWindows.Client.AIX", cmd);
    }

    [Fact]
    public void WindowsAiAppCatalog_HasTheKnownPackages()
    {
        var names = WindowsAiAppCatalog.All.Select(d => d.PackageName).ToHashSet();
        Assert.Contains("Microsoft.Copilot", names);
        Assert.Contains("Microsoft.Windows.Ai.Copilot.Provider", names);
        Assert.Contains("MicrosoftWindows.Client.AIX", names);
        // No duplicates -- would cause two monitors with the same Id and break drift detection
        Assert.Equal(WindowsAiAppCatalog.All.Count, names.Count);
    }

    [Fact]
    public void ServiceCatalog_IncludesWsaiFabricSvc()
    {
        var svc = ServiceCatalog.All.FirstOrDefault(d =>
            d.Name.Equals("WSAIFabricSvc", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(svc);
        Assert.Contains("AI Fabric", svc!.DisplayName);
    }

    [Fact]
    public void ServiceCatalog_IncludesAarSvc()
    {
        var svc = ServiceCatalog.All.FirstOrDefault(d =>
            d.Name.Equals("AarSvc", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(svc);
        Assert.Contains("Agent Activation", svc!.DisplayName);
    }

    [Fact]
    public void CopilotMonitor_NoDriftWhenDesiredMatchesCurrent()
    {
        // ReadCurrent returns true when no off-policy is set. With a default
        // GlobalPreferences (DesiredOn=true) there should be no drift on any
        // dev machine that doesn't actively block Copilot.
        var cfg = new AppConfig();
        cfg.Global.Copilot.Monitor = true;
        var current = CopilotMonitor.ReadCurrent();
        var drift = new CopilotMonitor().CheckDrift(cfg).ToList();
        if (current is bool c && c == cfg.Global.Copilot.DesiredOn)
            Assert.Empty(drift);
        // If current != desired, drift should fire -- but we won't assert that
        // here because we'd be asserting against the test machine's state.
    }

    [Fact]
    public void WindowsAiAppMonitor_NoDrift_WhenDesiredRemovedFalse()
    {
        var cfg = new AppConfig();
        cfg.WindowsAiApps["Microsoft.Copilot"] = new WindowsAiAppPref
        {
            Monitor = true,
            DesiredRemoved = false,
            AutoApply = false,
        };
        var monitor = new WindowsAiAppMonitor(WindowsAiAppCatalog.All[0]);
        Assert.Empty(monitor.CheckDrift(cfg));
    }

    [Fact]
    public void WindowsAiAppMonitor_NoDrift_WhenPackageMissingFromConfig()
    {
        var cfg = new AppConfig();
        var monitor = new WindowsAiAppMonitor(WindowsAiAppCatalog.All[0]);
        Assert.Empty(monitor.CheckDrift(cfg));
    }

    [Fact]
    public void AppConfigCloner_RoundTrips_WindowsAiApps()
    {
        var src = new AppConfig();
        src.WindowsAiApps["Microsoft.Copilot"] = new WindowsAiAppPref
        {
            Monitor = true,
            DesiredRemoved = true,
            AutoApply = true,
        };
        var clone = AppConfigCloner.Clone(src);
        Assert.True(clone.WindowsAiApps["Microsoft.Copilot"].DesiredRemoved);
        Assert.True(clone.WindowsAiApps["Microsoft.Copilot"].AutoApply);
        clone.WindowsAiApps["Microsoft.Copilot"].DesiredRemoved = false;
        // Source not affected by draft mutation -- this is the core invariant
        // the staged-apply UI depends on.
        Assert.True(src.WindowsAiApps["Microsoft.Copilot"].DesiredRemoved);
    }
}
