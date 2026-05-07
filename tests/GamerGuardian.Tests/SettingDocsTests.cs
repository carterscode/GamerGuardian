using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class SettingDocsTests
{
    [Theory]
    [InlineData("hags")]
    [InlineData("memintegrity")]
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
    public void MechanismFor_KnownIds_ReturnsNonEmpty(string id)
    {
        var mech = SettingDocs.MechanismFor(id);
        Assert.False(string.IsNullOrWhiteSpace(mech), $"no Mechanism for {id}");
        Assert.NotEqual("(unknown)", mech);
    }

    [Theory]
    [InlineData("hags")]
    [InlineData("memintegrity")]
    [InlineData("gamemode")]
    [InlineData("powerplan")]
    public void VerifyCommandFor_KnownIds_ReturnsNonEmpty(string id)
    {
        var cmd = SettingDocs.VerifyCommandFor(id);
        Assert.False(string.IsNullOrWhiteSpace(cmd), $"no Verify command for {id}");
    }

    [Fact]
    public void MechanismFor_DisplayPrefixIds_RecognizesAllThree()
    {
        Assert.NotEqual("(unknown)", SettingDocs.MechanismFor("hdr:DISPLAY1"));
        Assert.NotEqual("(unknown)", SettingDocs.MechanismFor("refresh:DISPLAY1"));
        Assert.NotEqual("(unknown)", SettingDocs.MechanismFor("resolution:DISPLAY1"));
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
}
