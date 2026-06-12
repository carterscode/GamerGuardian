using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class ApplyCommandTests
{
    [Fact]
    public void ApplyCommandFor_Service_EmitsScConfigWithCorrectStartWord()
    {
        // raw=4 is the registry Start= dword for disabled; sc.exe wants "start= disabled".
        var cmd = SettingDocs.ApplyCommandFor("service:diagtrack", rawDesired: "4");
        Assert.Contains("sc.exe", cmd);
        Assert.Contains("\"diagtrack\"", cmd, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("start= disabled", cmd);
    }

    [Fact]
    public void ApplyCommandFor_Service_Auto_StartWord()
    {
        Assert.Contains("start= auto", SettingDocs.ApplyCommandFor("service:DiagTrack", rawDesired: "2"));
    }

    [Fact]
    public void ApplyCommandFor_Service_Manual_StartWord()
    {
        Assert.Contains("start= demand", SettingDocs.ApplyCommandFor("service:DiagTrack", rawDesired: "3"));
    }

    [Fact]
    public void ApplyCommandFor_PolicyOverrideService_DoSvc_EmitsRegistryWrite()
    {
        // DoSvc has a policy override -- the apply command should target the
        // policy registry path, not the service's Start dword.
        var cmd = SettingDocs.ApplyCommandFor("service:dosvc");
        Assert.Contains("HKLM:\\", cmd);
        Assert.Contains("DODownloadMode", cmd);
        Assert.DoesNotContain("sc.exe", cmd);
    }

    [Fact]
    public void ApplyCommandFor_PolicyOverrideService_DeletePath()
    {
        // When the user picks Default, the apply command should DELETE the
        // policy value rather than re-write it.
        var cmd = SettingDocs.ApplyCommandFor("service:dosvc", rawDesired: "(deleted)");
        Assert.Contains("Remove-ItemProperty", cmd);
        Assert.Contains("DODownloadMode", cmd);
    }

    [Theory]
    [InlineData("hags")]
    [InlineData("gamemode")]
    [InlineData("gamedvr")]
    [InlineData("sysresponse")]
    [InlineData("netthrottle")]
    [InlineData("usbsuspend")]
    [InlineData("vrr")]
    [InlineData("memintegrity")]
    [InlineData("vbs")]
    public void ApplyCommandFor_RegistryToggleSettings_EmitSetItemProperty(string id)
    {
        var cmd = SettingDocs.ApplyCommandFor(id);
        Assert.Contains("Set-ItemProperty", cmd);
        Assert.Contains("Type DWord", cmd);
    }

    [Fact]
    public void ApplyCommandFor_UsesRawDesiredWhenProvided()
    {
        var def = SettingDocs.ApplyCommandFor("hags");
        var custom = SettingDocs.ApplyCommandFor("hags", rawDesired: "1");
        // The default falls back to 2 (enable HAGS); a rawDesired of 1 should be
        // surfaced verbatim so users can see exactly what the app wrote.
        Assert.Contains("Value 2", def);
        Assert.Contains("Value 1", custom);
    }
}
