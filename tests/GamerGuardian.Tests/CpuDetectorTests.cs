using GamerGuardian.Models;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class CpuDetectorTests
{
    [Theory]
    [InlineData("AMD Ryzen 7 9800X3D 8-Core Processor", "9800X3D", "Zen5")]
    [InlineData("AMD Ryzen 9 9950X3D 16-Core Processor", "9950X3D", "Zen5")]
    [InlineData("AMD Ryzen 9 9950X3D2 16-Core Processor", "9950X3D2", "Zen5")]
    [InlineData("AMD Ryzen 9 7950X3D 16-Core Processor", "7950X3D", "Zen4")]
    [InlineData("AMD Ryzen 7 7800X3D 8-Core Processor", "7800X3D", "Zen4")]
    [InlineData("AMD Ryzen 7 9700X 8-Core Processor", "9700X", "Zen5")]
    [InlineData("AMD Ryzen 9 7950X 16-Core Processor", "7950X", "Zen4")]
    [InlineData("AMD Ryzen 5 7600 6-Core Processor", "7600", "Zen4")]
    public void Parse_Amd_ModelAndFamily(string name, string expectedModel, string expectedFamily)
    {
        var info = CpuDetector.Parse(name, "AuthenticAMD", "");
        Assert.Equal(CpuVendor.Amd, info.Vendor);
        Assert.Equal(expectedModel, info.Model);
        Assert.Equal(expectedFamily, info.Family);
        Assert.True(info.IsDetected);
    }

    [Fact]
    public void Parse_X3D2_DistinctFromX3D()
    {
        var x3d2 = CpuDetector.Parse("AMD Ryzen 9 9950X3D2 16-Core Processor", "AuthenticAMD", "");
        var x3d = CpuDetector.Parse("AMD Ryzen 9 9950X3D 16-Core Processor", "AuthenticAMD", "");
        Assert.Equal("9950X3D2", x3d2.Model);
        Assert.Equal("9950X3D", x3d.Model);
        Assert.NotEqual(x3d.Model, x3d2.Model);
    }

    [Theory]
    [InlineData("Intel(R) Core(TM) i7-14700K", "14700K", "IntelHybrid")]
    [InlineData("Intel(R) Core(TM) Ultra 9 285K", "285K", "IntelHybrid")]
    [InlineData("Intel(R) Core(TM) i9-12900K", "12900K", "IntelHybrid")]
    public void Parse_Intel_Hybrid(string name, string expectedModel, string expectedFamily)
    {
        var info = CpuDetector.Parse(name, "GenuineIntel", "");
        Assert.Equal(CpuVendor.Intel, info.Vendor);
        Assert.Equal(expectedModel, info.Model);
        Assert.Equal(expectedFamily, info.Family);
    }

    [Fact]
    public void Parse_OlderIntel_NotHybrid()
    {
        var info = CpuDetector.Parse("Intel(R) Core(TM) i7-7700K CPU @ 4.20GHz", "GenuineIntel", "");
        Assert.Equal(CpuVendor.Intel, info.Vendor);
        Assert.Equal("IntelOther", info.Family);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Some Garbage String 12345")]
    public void Parse_UnknownOrEmpty_DoesNotThrow_AndIsNotDetected(string name)
    {
        var info = CpuDetector.Parse(name, "", "");
        Assert.Equal(CpuVendor.Unknown, info.Vendor);
        Assert.False(info.IsDetected);
    }

    [Fact]
    public void Parse_NullArgs_DoNotThrow()
    {
        var info = CpuDetector.Parse(null, null, null);
        Assert.Equal(CpuVendor.Unknown, info.Vendor);
        Assert.False(info.IsDetected);
    }

    [Theory]
    [InlineData("amd ryzen 7 9800x3d")]
    [InlineData("  AMD Ryzen 7   9800X3D  ")]
    public void Parse_CasingAndWhitespace_NormalizeIdentically(string name)
    {
        var info = CpuDetector.Parse(name, "AuthenticAMD", "");
        Assert.Equal("9800X3D", info.Model);
    }
}
