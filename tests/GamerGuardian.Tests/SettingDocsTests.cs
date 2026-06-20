using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class SettingDocsTests
{
    [Theory]
    [InlineData("hags")]
    [InlineData("memintegrity")]
    [InlineData("vbs")]
    [InlineData("gamemode")]
    [InlineData("gamedvr")]
    [InlineData("mouseaccel")]
    [InlineData("fso")]
    [InlineData("vrr")]
    [InlineData("sysresponse")]
    [InlineData("netthrottle")]
    [InlineData("usbsuspend")]
    [InlineData("gamestask")]
    [InlineData("powerplan")]
    [InlineData("privacy.advertisingid")]
    [InlineData("privacy.tailoredexp")]
    [InlineData("privacy.cdp")]
    [InlineData("privacy.activityhistory")]
    [InlineData("powerthrottling")]
    [InlineData("faststartup")]
    [InlineData("visualfx")]
    [InlineData("network.nagle")]
    [InlineData("network.nicpower")]
    public void MechanismFor_KnownIds_ReturnsNonEmpty(string id)
    {
        var mech = SettingDocs.MechanismFor(id);
        Assert.False(string.IsNullOrWhiteSpace(mech), $"no Mechanism for {id}");
        Assert.NotEqual("(unknown)", mech);
    }

    [Theory]
    [InlineData("hags")]
    [InlineData("memintegrity")]
    [InlineData("vbs")]
    [InlineData("gamemode")]
    [InlineData("powerplan")]
    [InlineData("privacy.advertisingid")]
    [InlineData("privacy.cdp")]
    [InlineData("privacy.activityhistory")]
    [InlineData("powerthrottling")]
    [InlineData("faststartup")]
    [InlineData("network.nagle")]
    [InlineData("network.nicpower")]
    public void VerifyCommandFor_KnownIds_ReturnsNonEmpty(string id)
    {
        var cmd = SettingDocs.VerifyCommandFor(id);
        Assert.False(string.IsNullOrWhiteSpace(cmd), $"no Verify command for {id}");
    }

    [Theory]
    [InlineData("hags")]
    [InlineData("memintegrity")]
    [InlineData("vbs")]
    [InlineData("gamemode")]
    [InlineData("gamedvr")]
    [InlineData("sysresponse")]
    [InlineData("netthrottle")]
    [InlineData("usbsuspend")]
    [InlineData("vrr")]
    [InlineData("fso")]
    [InlineData("powerthrottling")]
    [InlineData("faststartup")]
    [InlineData("visualfx")]
    [InlineData("privacy.advertisingid")]
    [InlineData("privacy.cdp")]
    [InlineData("ai.copilot")]
    [InlineData("ai.recall")]
    [InlineData("debloat.widgets")]
    public void ReverseCommandFor_RegistryToggles_ReturnsNonEmpty(string id)
    {
        // Every registry-backed toggle must offer a clean command to put it back
        // (so the Learn more block can show both directions). Services, display
        // settings, mouse accel and the Games task profile deliberately return "" and
        // fall back to the prose Reversible via.
        var cmd = SettingDocs.ReverseCommandFor(id);
        Assert.False(string.IsNullOrWhiteSpace(cmd), $"no Reverse command for {id}");
    }

    [Fact]
    public void ReverseCommandFor_PolicyOverrideService_DeletesPolicyValue()
    {
        var cmd = SettingDocs.ReverseCommandFor("service:dosvc");
        Assert.Contains("Remove-ItemProperty", cmd);
        Assert.Contains("DODownloadMode", cmd);
    }

    [Fact]
    public void ReverseCommandFor_Service_ReEnables()
    {
        var cmd = SettingDocs.ReverseCommandFor("service:DiagTrack");
        Assert.Contains("sc.exe", cmd);
        Assert.Contains("DiagTrack", cmd, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MechanismFor_DisplayPrefixIds_RecognizesAllThree()
    {
        Assert.NotEqual("(unknown)", SettingDocs.MechanismFor("hdr:DISPLAY1"));
        Assert.NotEqual("(unknown)", SettingDocs.MechanismFor("refresh:DISPLAY1"));
        Assert.NotEqual("(unknown)", SettingDocs.MechanismFor("resolution:DISPLAY1"));
        Assert.NotEqual("(unknown)", SettingDocs.MechanismFor("drr:DISPLAY1"));
    }

    [Fact]
    public void MechanismFor_ServicePrefix_IncludesServiceName()
    {
        var mech = SettingDocs.MechanismFor("service:diagtrack");
        Assert.Contains("diagtrack", mech, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VerifyCommandFor_ServicePrefix_IncludesScQc()
    {
        var cmd = SettingDocs.VerifyCommandFor("service:diagtrack");
        Assert.Contains("sc qc", cmd);
        Assert.Contains("diagtrack", cmd, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MechanismFor_UnknownId_ReturnsUnknownMarker()
    {
        Assert.Equal("(unknown)", SettingDocs.MechanismFor("definitely_not_a_real_setting"));
    }

    [Fact]
    public void MechanismFor_DoSvc_SurfacesPolicyPath()
    {
        // DoSvc has a PolicyOverride; mechanism should reference the policy
        // registry path, not the Services\DoSvc\Start path that sc.exe would
        // touch (and that Windows would revert).
        var mech = SettingDocs.MechanismFor("service:dosvc");
        Assert.Contains("Policies\\Microsoft\\Windows\\DeliveryOptimization", mech);
        Assert.Contains("DODownloadMode", mech);
        Assert.DoesNotContain("CurrentControlSet\\Services", mech);
    }

    [Fact]
    public void VerifyCommandFor_DoSvc_QueriesPolicyValue()
    {
        var cmd = SettingDocs.VerifyCommandFor("service:dosvc");
        Assert.Contains("Policies\\Microsoft\\Windows\\DeliveryOptimization", cmd);
        Assert.Contains("DODownloadMode", cmd);
        // sc qc would query the Services hive; the policy verify shouldn't.
        Assert.DoesNotContain("sc qc", cmd);
    }

    [Fact]
    public void Catalog_EveryEntry_HasRecommendation()
    {
        // The Settings UI shows "Recommended: {x}" next to Current/Default for
        // every documented setting (GlobalToggleRow / ServiceRow.RecommendedText).
        // A blank Recommended would render an empty/hidden line, so guard against
        // any catalog entry shipping without one.
        foreach (var d in SettingDocsCatalog.All)
            Assert.False(string.IsNullOrWhiteSpace(d.Recommended),
                $"no Recommended for {d.SettingId}");
    }

    [Fact]
    public void Catalog_Get_ResolvesRecommendation_ForToggleAndServiceIds()
    {
        // RecommendedText is built from SettingDocsCatalog.Get(SettingId).Recommended.
        // Spot-check the id shapes the toggle/service rows actually pass.
        Assert.False(string.IsNullOrWhiteSpace(SettingDocsCatalog.Get("ai.copilot")?.Recommended));
        Assert.False(string.IsNullOrWhiteSpace(SettingDocsCatalog.Get("service:DiagTrack")?.Recommended));
        Assert.False(string.IsNullOrWhiteSpace(SettingDocsCatalog.Get("powerplan")?.Recommended));
    }
}
