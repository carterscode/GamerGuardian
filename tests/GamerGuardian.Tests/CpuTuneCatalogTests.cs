using GamerGuardian.Models;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class CpuTuneCatalogTests
{
    private static CpuInfo Amd(string model) =>
        CpuDetector.Parse($"AMD Ryzen 9 {model} Processor", "AuthenticAMD", "");

    private static CpuInfo Intel(string name) =>
        CpuDetector.Parse(name, "GenuineIntel", "");

    private static uint? MinCores(CpuTuneResult r) =>
        r.Overrides.FirstOrDefault(o => o.Label.Contains("min cores", StringComparison.OrdinalIgnoreCase))?.Value;

    private static bool HasParkOverride(CpuTuneResult r) =>
        r.Overrides.Any(o => o.Label.Contains("min cores", StringComparison.OrdinalIgnoreCase));

    private static bool HasBoost(CpuTuneResult r) =>
        r.Overrides.Any(o => o.Label.Contains("boost mode", StringComparison.OrdinalIgnoreCase));

    [Theory]
    [InlineData("9850X3D")]
    [InlineData("9800X3D")]
    public void SingleCcdX3d_NoParking_Exact(string model)
    {
        var r = CpuTuneCatalog.Resolve(Amd(model));
        Assert.Equal(TuneTier.Exact, r.Definition.Tier);
        Assert.Equal(CcdTopology.Single, r.Topology);
        Assert.Equal(ParkingStrategy.NoParking, r.Parking);
        Assert.Equal(100u, MinCores(r));
        Assert.True(HasBoost(r));
        Assert.False(r.IsGeneric);
    }

    [Fact]
    public void AsymmetricDual_9950X3D_ParksFrequencyCcd()
    {
        var r = CpuTuneCatalog.Resolve(Amd("9950X3D"));
        Assert.Equal(CcdTopology.Dual, r.Topology);
        Assert.Equal(ParkingStrategy.ParkFrequencyCcd, r.Parking);
        Assert.Equal(50u, MinCores(r));
        Assert.NotEqual(100u, MinCores(r));
        Assert.True(r.NeedsCcdRoutingStack);
        Assert.Equal(PowerPlanChoice.Balanced, r.RecommendedPrebuilt);
    }

    [Fact]
    public void DualVCache_9950X3D2_NoParking_DespiteTwoCcds()
    {
        var r = CpuTuneCatalog.Resolve(Amd("9950X3D2"));
        Assert.Equal(ParkingStrategy.NoParking, r.Parking);
        Assert.Equal(100u, MinCores(r));
        Assert.NotEqual(50u, MinCores(r));
        Assert.False(r.NeedsCcdRoutingStack);
    }

    [Theory]
    [InlineData("7950X3D")]
    [InlineData("7900X3D")]
    [InlineData("9900X3D")]
    public void AsymmetricDualFamily_Parks(string model)
    {
        var r = CpuTuneCatalog.Resolve(Amd(model));
        Assert.Equal(ParkingStrategy.ParkFrequencyCcd, r.Parking);
        Assert.Equal(50u, MinCores(r));
        Assert.False(r.IsGeneric);
    }

    [Fact]
    public void SingleCcdFamily_7800X3D_NoParking()
    {
        var r = CpuTuneCatalog.Resolve(Amd("7800X3D"));
        Assert.Equal(ParkingStrategy.NoParking, r.Parking);
        Assert.Equal(100u, MinCores(r));
    }

    [Theory]
    [InlineData("7700X")]
    [InlineData("9700X")]
    public void NonX3dSingle_NoParking_NotGeneric(string model)
    {
        var r = CpuTuneCatalog.Resolve(Amd(model));
        Assert.Equal(ParkingStrategy.NoParking, r.Parking);
        Assert.Equal(100u, MinCores(r));
        Assert.False(r.IsGeneric);
        Assert.Equal(PowerPlanChoice.Balanced, r.RecommendedPrebuilt);
    }

    [Theory]
    [InlineData("7950X")]
    [InlineData("9950X")]
    public void NonX3dSymmetricDual_NoParking_NotMin50(string model)
    {
        var r = CpuTuneCatalog.Resolve(Amd(model));
        Assert.Equal(ParkingStrategy.NoParking, r.Parking);
        Assert.Equal(100u, MinCores(r));
        Assert.NotEqual(50u, MinCores(r));
    }

    [Theory]
    [InlineData("Intel(R) Core(TM) i7-14700K")]
    [InlineData("Intel(R) Core(TM) Ultra 9 285K")]
    public void IntelHybrid_BoostOnly_NoParkOverride(string name)
    {
        var r = CpuTuneCatalog.Resolve(Intel(name));
        Assert.True(HasBoost(r));
        Assert.False(HasParkOverride(r));
        Assert.Equal(PowerPlanChoice.Balanced, r.RecommendedPrebuilt);
        Assert.False(r.IsGeneric);
    }

    [Fact]
    public void Unknown_Generic_NoParkOverride()
    {
        var r = CpuTuneCatalog.Resolve(CpuInfo.Unknown("Some Weird CPU"));
        Assert.Equal(TuneTier.Generic, r.Definition.Tier);
        Assert.True(r.IsGeneric);
        Assert.False(HasParkOverride(r));
        Assert.True(HasBoost(r));
    }

    [Fact]
    public void ContentHash_DeterministicAndValueSensitive()
    {
        var a = CpuTuneCatalog.Resolve(Amd("9850X3D"));
        var b = CpuTuneCatalog.Resolve(Amd("9800X3D")); // same single-CCD recipe
        Assert.Equal(a.ContentHash, b.ContentHash);

        var dual = CpuTuneCatalog.Resolve(Amd("9950X3D"));
        Assert.NotEqual(a.ContentHash, dual.ContentHash);
    }

    [Fact]
    public void NoEntry_RecommendsHighPerformance()
    {
        Assert.All(CpuTuneCatalog.All, d => Assert.Equal(PowerPlanChoice.Balanced, d.RecommendedPrebuilt));
    }

    [Fact]
    public void ParkingGuard_OnlyAsymmetricDualParks()
    {
        // The only entries that may set min cores = 50 are the asymmetric dual-CCD
        // X3D recipes; every other entry leaves min cores >= 100 or omits it.
        foreach (var def in CpuTuneCatalog.All)
        {
            var min = def.Overrides
                .FirstOrDefault(o => o.Label.Contains("min cores", StringComparison.OrdinalIgnoreCase))?.Value;
            if (min == 50u)
                Assert.Equal(ParkingStrategy.ParkFrequencyCcd, def.Parking);
            else if (min is not null)
                Assert.True(min >= 100u, $"{def.Key} has unexpected min cores {min}");
        }
    }

    [Fact]
    public void CatalogInvariants()
    {
        Assert.NotEmpty(CpuTuneCatalog.All);

        var keys = CpuTuneCatalog.All.Select(d => d.Key).ToList();
        Assert.Equal(keys.Count, keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        Assert.Contains(CpuTuneCatalog.All, d => d.Tier == TuneTier.Generic);

        foreach (var def in CpuTuneCatalog.All)
        {
            Assert.NotNull(def.Overrides);
            foreach (var o in def.Overrides)
                Assert.InRange(o.Value, 0u, 100u);
        }
    }
}
