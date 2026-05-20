using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class CpuInfoTests
{
    [Theory]
    [InlineData("AMD Ryzen 7 5800X3D 8-Core Processor", true)]
    [InlineData("AMD Ryzen 7 7800X3D 8-Core Processor", true)]
    [InlineData("AMD Ryzen 9 7950X3D 16-Core Processor", true)]
    [InlineData("AMD Ryzen 7 9800X3D 8-Core Processor", true)]
    [InlineData("AMD Ryzen 9 9950X3D 16-Core Processor", true)]
    // Hypothetical future X3D variants -- pattern still matches
    [InlineData("AMD Ryzen 9 9850X3D 16-Core Processor", true)]
    [InlineData("AMD Ryzen Threadripper 5970X3D", true)]
    public void IsAmdX3D_X3dChips_True(string name, bool expected)
    {
        Assert.Equal(expected, CpuInfo.IsAmdX3D(name));
    }

    [Theory]
    [InlineData("AMD Ryzen 9 7950X 16-Core Processor")]
    [InlineData("AMD Ryzen 7 7700X 8-Core Processor")]
    [InlineData("AMD Ryzen 7 9700X 8-Core Processor")]
    [InlineData("AMD Ryzen 9 9950X 16-Core Processor")]
    [InlineData("Intel(R) Core(TM) i9-14900K CPU @ 3.20GHz")]
    [InlineData("Intel(R) Core(TM) Ultra 9 285K")]
    [InlineData("Snapdragon X Elite")]
    [InlineData("Apple M3")]
    public void IsAmdX3D_NonX3dChips_False(string name)
    {
        Assert.False(CpuInfo.IsAmdX3D(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsAmdX3D_NullOrEmpty_False(string? name)
    {
        Assert.False(CpuInfo.IsAmdX3D(name));
    }

    [Fact]
    public void IsAmdX3D_CaseInsensitive()
    {
        Assert.True(CpuInfo.IsAmdX3D("amd ryzen 7 7800x3d"));
        Assert.True(CpuInfo.IsAmdX3D("AMD RYZEN 9 9950X3D"));
    }

    [Fact]
    public void GetName_ReturnsNonEmpty_OnRealMachine()
    {
        // Smoke test against the actual machine -- can't assert content but the
        // registry value should be present on any modern Windows install.
        var name = CpuInfo.GetName();
        Assert.False(string.IsNullOrWhiteSpace(name));
    }
}
