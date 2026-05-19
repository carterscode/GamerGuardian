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
    public void GlobalPreferences_HasAllFiveAiToggles()
    {
        var g = new GlobalPreferences();
        Assert.NotNull(g.Copilot);
        Assert.NotNull(g.Recall);
        Assert.NotNull(g.ClickToDo);
        Assert.NotNull(g.EdgeAi);
        Assert.NotNull(g.NotepadPaintAi);
        // Default-On so a fresh config doesn't aggressively disable anything
        // before the user opts in via Monitor=true.
        Assert.True(g.Copilot.DesiredOn);
        Assert.True(g.Recall.DesiredOn);
        Assert.True(g.ClickToDo.DesiredOn);
        Assert.True(g.EdgeAi.DesiredOn);
        Assert.True(g.NotepadPaintAi.DesiredOn);
        Assert.False(g.Copilot.Monitor);
    }

    [Theory]
    [InlineData("ai.copilot")]
    [InlineData("ai.recall")]
    [InlineData("ai.clicktodo")]
    [InlineData("ai.edge")]
    [InlineData("ai.notepadpaint")]
    public void SettingDocs_AiPolicyIds_HaveMechanismAndVerify(string id)
    {
        Assert.False(string.IsNullOrWhiteSpace(SettingDocs.MechanismFor(id)));
        Assert.NotEqual("(unknown)", SettingDocs.MechanismFor(id));
        Assert.False(string.IsNullOrWhiteSpace(SettingDocs.VerifyCommandFor(id)));
        Assert.False(string.IsNullOrWhiteSpace(SettingDocs.ApplyCommandFor(id)));
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
