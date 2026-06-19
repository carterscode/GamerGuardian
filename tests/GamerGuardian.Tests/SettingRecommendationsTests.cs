using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class SettingRecommendationsTests
{
    [Theory]
    // Global gaming: clear performance wins
    [InlineData("gamemode", true)]
    [InlineData("gamedvr", false)]
    [InlineData("hags", true)]
    [InlineData("vrr", true)]
    [InlineData("sysresponse", true)]
    [InlineData("usbsuspend", true)]
    [InlineData("gamestask", true)]
    [InlineData("mouseaccel", false)]
    [InlineData("fso", true)]
    [InlineData("powerthrottling", true)]
    [InlineData("faststartup", true)]
    [InlineData("visualfx", true)]
    // Security: recommend keeping protection ON for a typical gamer
    [InlineData("memintegrity", true)]
    [InlineData("vbs", true)]
    // AI: off
    [InlineData("ai.copilot", false)]
    [InlineData("ai.office", false)]
    // Privacy: lean privacy-respecting
    [InlineData("privacy.advertisingid", false)]
    [InlineData("privacy.cdp", true)]            // "Gaming" = disabled via policy
    [InlineData("privacy.activityhistory", true)]
    // Debloat: lean clutter-free
    [InlineData("debloat.widgets", false)]
    // Network: throttling is safe; Nagle/NIC contested -> Default
    [InlineData("netthrottle", true)]
    [InlineData("network.nagle", false)]
    [InlineData("network.nicpower", false)]
    public void ForToggle_ReturnsExpectedRecommendation(string settingId, bool expected)
    {
        Assert.Equal(expected, SettingRecommendations.ForToggle(settingId));
    }

    [Fact]
    public void ForToggle_UnknownId_ReturnsNull()
    {
        Assert.Null(SettingRecommendations.ForToggle("definitely_not_a_setting"));
        Assert.Null(SettingRecommendations.ForToggle(null!));
    }

    [Theory]
    // The hint must render in each row's own vocabulary, never a generic On/Off.
    [InlineData("gamedvr", "Enabled", "Disabled", "Recommended: Disabled")]
    [InlineData("gamemode", "Enabled", "Disabled", "Recommended: Enabled")]
    [InlineData("netthrottle", "Gaming", "Default", "Recommended: Gaming")]
    [InlineData("network.nagle", "Gaming", "Default", "Recommended: Default")]
    [InlineData("ai.copilot", "On", "Off", "Recommended: Off")]
    [InlineData("privacy.advertisingid", "Enabled", "Disabled", "Recommended: Disabled")]
    public void FormatToggleHint_RendersInRowVocabulary(
        string settingId, string onLabel, string offLabel, string expected)
    {
        Assert.Equal(expected, SettingRecommendations.FormatToggleHint(settingId, onLabel, offLabel));
    }

    [Fact]
    public void FormatToggleHint_UnknownId_ReturnsEmpty()
    {
        Assert.Equal(string.Empty,
            SettingRecommendations.FormatToggleHint("nope", "Enabled", "Disabled"));
    }

    [Fact]
    public void Preset_CoveredToggleIds_AllExistInTheMap()
    {
        // The one-click preset reads SettingRecommendations.ToggleDesiredOn by id;
        // a missing id would throw at apply time. This pins the contract so a
        // renamed/removed entry fails here with a clear message instead.
        string[] presetIds =
        {
            "gamemode", "gamedvr", "hags", "vrr", "sysresponse", "netthrottle",
            "usbsuspend", "gamestask", "mouseaccel", "fso",
            "ai.copilot", "ai.recall", "ai.clicktodo", "ai.edge", "ai.notepadpaint",
            "ai.settingssearch", "ai.actions", "ai.inputinsights", "ai.office",
        };
        foreach (var id in presetIds)
            Assert.True(SettingRecommendations.ToggleDesiredOn.ContainsKey(id),
                $"Preset relies on '{id}' but it's missing from the recommendation map");
    }
}
