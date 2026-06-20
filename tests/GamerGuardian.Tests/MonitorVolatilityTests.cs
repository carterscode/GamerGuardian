using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class MonitorVolatilityTests
{
    [Theory]
    [InlineData("hdr")]
    [InlineData("refresh")]
    [InlineData("resolution")]
    [InlineData("drr")]
    [InlineData("hdr:DISPLAY1")]            // per-display drift id classifies like its base
    [InlineData("refresh:\\\\.\\DISPLAY2")]
    public void DisplaySettings_AreVolatile(string id)
    {
        Assert.Equal(MonitorTier.Volatile, MonitorVolatility.TierFor(id));
        Assert.True(MonitorVolatility.IsVolatile(id));
    }

    [Theory]
    [InlineData("hags")]
    [InlineData("vbs")]
    [InlineData("memintegrity")]
    [InlineData("sysresponse")]
    [InlineData("usbsuspend")]
    [InlineData("powerthrottling")]
    [InlineData("faststartup")]
    [InlineData("visualfx")]
    [InlineData("mouseaccel")]
    [InlineData("powerplan")]
    [InlineData("ai.copilot")]
    [InlineData("ai.recall")]
    [InlineData("privacy.cdp")]
    [InlineData("privacy.advertisingid")]
    [InlineData("debloat.widgets")]
    [InlineData("network.nagle")]
    [InlineData("network.nicpower")]
    [InlineData("service:diagtrack")]
    [InlineData("service:dosvc")]
    [InlineData("ai.app:Microsoft.Copilot")]
    public void RegistryPolicyAndServiceSettings_AreStable(string id)
    {
        Assert.Equal(MonitorTier.Stable, MonitorVolatility.TierFor(id));
        Assert.False(MonitorVolatility.IsVolatile(id));
    }

    [Fact]
    public void NullId_IsStable()
    {
        Assert.Equal(MonitorTier.Stable, MonitorVolatility.TierFor((string?)null));
        Assert.False(MonitorVolatility.IsVolatile(null));
    }

    [Fact]
    public void Classification_IsCaseInsensitive()
    {
        Assert.Equal(MonitorTier.Volatile, MonitorVolatility.TierFor("HDR"));
        Assert.Equal(MonitorTier.Volatile, MonitorVolatility.TierFor("Refresh:DISPLAY1"));
    }
}
