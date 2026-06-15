using GamerGuardian.Models;
using GamerGuardian.Monitors;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

/// <summary>
/// Wiring tests for the Debloat tab (ads/nags/suggested-content + background
/// bloat) and the two new Privacy data-collection toggles. Same surface-contract
/// approach as WindowsAiTests -- we don't mutate the live registry, only assert
/// IDs, prefs, docs, mechanisms, and CheckDrift behavior given crafted prefs.
/// </summary>
public class DebloatTests
{
    [Fact]
    public void GlobalPreferences_HasAllDebloatAndPrivacyToggles()
    {
        var g = new GlobalPreferences();
        Assert.NotNull(g.SuggestedContent);
        Assert.NotNull(g.LockScreenSpotlight);
        Assert.NotNull(g.FinishSetupNag);
        Assert.NotNull(g.StartRecommendations);
        Assert.NotNull(g.ExplorerAds);
        Assert.NotNull(g.FeedbackNag);
        Assert.NotNull(g.Widgets);
        Assert.NotNull(g.EdgeBackground);
        Assert.NotNull(g.OnlineSpeech);
        Assert.NotNull(g.InkingTyping);
    }

    [Fact]
    public void DebloatAndPrivacyToggles_DefaultToDebloatedAndUnmonitored()
    {
        // DesiredOn=false = the debloated/privacy state; Monitor=false = zero
        // behavior change until the user opts in. Mirrors the Privacy-tab defaults.
        var g = new GlobalPreferences();
        foreach (var pref in new[]
        {
            g.SuggestedContent, g.LockScreenSpotlight, g.FinishSetupNag,
            g.StartRecommendations, g.ExplorerAds, g.FeedbackNag,
            g.Widgets, g.EdgeBackground, g.OnlineSpeech, g.InkingTyping,
        })
        {
            Assert.False(pref.DesiredOn);
            Assert.False(pref.Monitor);
            Assert.False(pref.AutoApply);
        }
    }

    [Theory]
    [InlineData("debloat.suggestedcontent")]
    [InlineData("debloat.spotlight")]
    [InlineData("debloat.finishsetup")]
    [InlineData("debloat.startrecommend")]
    [InlineData("debloat.explorerads")]
    [InlineData("debloat.feedback")]
    [InlineData("debloat.widgets")]
    [InlineData("debloat.edge")]
    [InlineData("privacy.speech")]
    [InlineData("privacy.inking")]
    public void SettingDocs_NewIds_HaveMechanismApplyAndVerify(string id)
    {
        Assert.False(string.IsNullOrWhiteSpace(SettingDocs.MechanismFor(id)));
        Assert.NotEqual("(unknown)", SettingDocs.MechanismFor(id));
        Assert.False(string.IsNullOrWhiteSpace(SettingDocs.ApplyCommandFor(id)));
        Assert.False(string.IsNullOrWhiteSpace(SettingDocs.VerifyCommandFor(id)));
    }

    [Theory]
    [InlineData("debloat.suggestedcontent", "SilentInstalledAppsEnabled")]
    [InlineData("debloat.spotlight", "RotatingLockScreenOverlayEnabled")]
    [InlineData("debloat.finishsetup", "ScoobeSystemSettingEnabled")]
    [InlineData("debloat.startrecommend", "Start_IrisRecommendations")]
    [InlineData("debloat.explorerads", "ShowSyncProviderNotifications")]
    [InlineData("debloat.feedback", "NumberOfSIUFInPeriod")]
    [InlineData("debloat.widgets", "AllowNewsAndInterests")]
    [InlineData("debloat.edge", "StartupBoostEnabled")]
    [InlineData("privacy.speech", "HasAccepted")]
    [InlineData("privacy.inking", "AcceptedPrivacyPolicy")]
    public void SettingDocs_ApplyCommand_TouchesExpectedKey(string id, string expected)
    {
        Assert.Contains(expected, SettingDocs.ApplyCommandFor(id));
    }

    [Fact]
    public void SettingDocs_Widgets_UsesHklmPolicyAndTaskbarFlag()
    {
        var apply = SettingDocs.ApplyCommandFor("debloat.widgets");
        Assert.Contains("HKLM", apply);
        Assert.Contains("Dsh", apply);
        Assert.Contains("TaskbarDa", apply);
    }

    [Fact]
    public void SettingDocs_InkingTyping_DoesNotTouchTextCollection_OwnedByInputInsights()
    {
        // privacy.inking must not write RestrictImplicitTextCollection -- that
        // value is owned by the Windows AI tab's ai.inputinsights monitor. Two
        // monitors fighting over one value would thrash.
        Assert.DoesNotContain("RestrictImplicitTextCollection", SettingDocs.ApplyCommandFor("privacy.inking"));
        Assert.Contains("RestrictImplicitInkCollection", SettingDocs.ApplyCommandFor("privacy.inking"));
    }

    [Fact]
    public void SuggestedContentMonitor_NoDrift_WhenDesiredMatchesCurrent()
    {
        var cfg = new AppConfig();
        cfg.Global.SuggestedContent.Monitor = true;
        var current = SuggestedContentMonitor.ReadCurrent();
        var drift = new SuggestedContentMonitor().CheckDrift(cfg).ToList();
        if (current is bool c && c == cfg.Global.SuggestedContent.DesiredOn)
            Assert.Empty(drift);
    }

    [Fact]
    public void WidgetsMonitor_NoDrift_WhenDesiredMatchesCurrent()
    {
        var cfg = new AppConfig();
        cfg.Global.Widgets.Monitor = true;
        var current = WidgetsMonitor.ReadCurrent();
        var drift = new WidgetsMonitor().CheckDrift(cfg).ToList();
        if (current is bool c && c == cfg.Global.Widgets.DesiredOn)
            Assert.Empty(drift);
    }

    [Fact]
    public void AppConfigCloner_RoundTrips_DebloatPrefs()
    {
        var src = new AppConfig();
        src.Global.SuggestedContent.Monitor = true;
        src.Global.SuggestedContent.DesiredOn = true;
        src.Global.Widgets.AutoApply = true;

        var clone = AppConfigCloner.Clone(src);

        Assert.True(clone.Global.SuggestedContent.Monitor);
        Assert.True(clone.Global.SuggestedContent.DesiredOn);
        Assert.True(clone.Global.Widgets.AutoApply);

        // Draft mutation must not bleed back into the source (staged-apply invariant).
        clone.Global.SuggestedContent.Monitor = false;
        Assert.True(src.Global.SuggestedContent.Monitor);
    }
}
