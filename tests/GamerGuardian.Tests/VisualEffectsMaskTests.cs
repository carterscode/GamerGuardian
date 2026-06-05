using GamerGuardian.Monitors;
using Xunit;

namespace GamerGuardian.Tests;

public class VisualEffectsMaskTests
{
    [Theory]
    [InlineData(2, true)]
    [InlineData(0, false)]   // let Windows choose
    [InlineData(1, false)]   // best appearance
    [InlineData(3, false)]   // custom
    [InlineData(null, false)]
    public void IsBestPerformance_OnlyTrueForTwo(int? value, bool expected)
    {
        Assert.Equal(expected, VisualEffectsMonitor.IsBestPerformance(value));
    }

    [Fact]
    public void AnimationsDisabled_TrueForBestPerformanceMask()
    {
        // The empirically-confirmed best-performance mask: byte0 = 0x90,
        // animation bits (0x0E) clear.
        var mask = new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 };
        Assert.True(VisualEffectsMonitor.AnimationsDisabled(mask));
    }

    [Fact]
    public void AnimationsDisabled_FalseWhenAnimationBitsSet()
    {
        // A typical "best appearance" mask has the animation bits set in byte0
        // (e.g. 0x9E sets bits 1-3).
        var mask = new byte[] { 0x9E, 0x1E, 0x07, 0x80, 0x12, 0x00, 0x00, 0x00 };
        Assert.False(VisualEffectsMonitor.AnimationsDisabled(mask));
    }

    [Fact]
    public void AnimationsDisabled_FalseForOneAnimationBit()
    {
        Assert.False(VisualEffectsMonitor.AnimationsDisabled(new byte[] { 0x02 })); // menu animation bit
        Assert.True(VisualEffectsMonitor.AnimationsDisabled(new byte[] { 0x90 }));  // animation bits clear
    }

    [Fact]
    public void AnimationsDisabled_FalseForNullOrEmpty()
    {
        Assert.False(VisualEffectsMonitor.AnimationsDisabled(null));
        Assert.False(VisualEffectsMonitor.AnimationsDisabled(System.Array.Empty<byte>()));
    }
}
