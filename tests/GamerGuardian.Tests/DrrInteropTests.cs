using GamerGuardian.Native;
using Xunit;

namespace GamerGuardian.Tests;

public class DrrInteropTests
{
    [Fact]
    public void IsDrrEnabled_TrueWhenBoostFlagSet()
    {
        Assert.True(DrrInterop.IsDrrEnabled(DisplayConfig.DISPLAYCONFIG_PATH_BOOST_REFRESH_RATE));
        Assert.True(DrrInterop.IsDrrEnabled(0x10));
    }

    [Fact]
    public void IsDrrEnabled_FalseWhenBoostFlagClear()
    {
        Assert.False(DrrInterop.IsDrrEnabled(0x00));
        Assert.False(DrrInterop.IsDrrEnabled(0x08)); // a different path flag (virtual-mode support)
    }

    [Fact]
    public void IsDrrEnabled_IgnoresUnrelatedBits()
    {
        // Boost bit set alongside other flags -> still enabled.
        Assert.True(DrrInterop.IsDrrEnabled(0x10 | 0x08 | 0x01));
        // Only unrelated bits set -> not enabled.
        Assert.False(DrrInterop.IsDrrEnabled(0x08 | 0x01));
    }
}
