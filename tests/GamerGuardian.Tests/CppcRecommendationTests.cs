using System.Linq;
using GamerGuardian.Models;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

/// <summary>
/// Covers the BIOS guidance shown for asymmetric dual-CCD X3D parts.
///
/// <para>These assertions stand in for a screenshot: the dependency panel and the
/// BIOS page only render this content on a dual-CCD machine, and the development
/// machine is a single-CCD 9850X3D, so the rendered result cannot be captured
/// locally. What the catalog hands the UI can still be pinned down exactly.</para>
/// </summary>
public class CppcRecommendationTests
{
    private static CpuTuneResult Resolve(string model) =>
        CpuTuneCatalog.Resolve(CpuDetector.Parse($"AMD Ryzen 9 {model} Processor", "AuthenticAMD", ""));

    private static BiosRecommendation? Cppc(string model) =>
        Resolve(model).Bios.FirstOrDefault(b => b.Name.Contains("CPPC"));

    [Theory]
    [InlineData("9950X3D")]
    [InlineData("7950X3D")]
    [InlineData("9900X3D")]
    [InlineData("7900X3D")]
    public void AsymmetricDualCcd_RecommendsCache(string model)
    {
        var rec = Cppc(model);

        Assert.NotNull(rec);
        Assert.Equal("Cache", rec!.RecommendedValue);
        Assert.Equal(CpuTuneCatalog.PreferredCppcValue, rec.RecommendedValue);
    }

    [Theory]
    [InlineData("9950X3D")]
    [InlineData("7950X3D")]
    public void AsymmetricDualCcd_StillDocumentsDriverAsTheAlternative(string model)
    {
        // Cache is a tradeoff, not a correction. Driver remains AMD's official
        // setting and is the better choice on a machine that also does heavy
        // multi-threaded work, so the panel must keep offering it.
        var rec = Cppc(model);

        Assert.NotNull(rec!.Alternative);
        Assert.Contains("Driver", rec.Alternative!);
    }

    [Fact]
    public void CacheRationale_ExplainsWhyItDoesNotDependOnGameDetection()
    {
        // The whole reason to prefer Cache on a gaming box: it removes the
        // dependency on Xbox Game Bar recognising the title.
        var rec = Cppc("9950X3D");

        Assert.Contains("Game Bar", rec!.Rationale);
    }

    [Fact]
    public void OptionNamesAreFlaggedAsVendorSpecific()
    {
        // The app cannot read BIOS state and board vendors label these differently,
        // so the guidance must not imply one exact menu wording.
        var rec = Cppc("9950X3D");

        Assert.Contains("vary by board", rec!.Rationale);
    }

    [Fact]
    public void SingleCcdX3D_HasNoCppcGuidance()
    {
        // A single-CCD X3D has nothing to route between clusters, so offering a
        // preferred-CCD setting there would be noise.
        Assert.Null(CpuTuneCatalog.Resolve(
            CpuDetector.Parse("AMD Ryzen 7 9800X3D 8-Core Processor", "AuthenticAMD", ""))
            .Bios.FirstOrDefault(b => b.Name.Contains("CPPC")));
    }

    [Fact]
    public void DualVCacheParts_GetNoCppcGuidanceEither()
    {
        // The 9950X3D2 carries V-Cache on both CCDs, so there is no cache CCD to
        // prefer and the recommendation would be actively wrong.
        Assert.Null(Cppc("9950X3D2"));
    }

    [Fact]
    public void EveryRecommendationWithAnAlternativeNamesADifferentValue()
    {
        // Guards against an Alternative that just restates the recommendation.
        foreach (var model in new[] { "9950X3D", "7950X3D", "9900X3D", "7900X3D" })
        {
            foreach (var b in Resolve(model).Bios.Where(b => b.Alternative is not null))
            {
                Assert.DoesNotContain($"{b.Alternative}", b.RecommendedValue);
            }
        }
    }
}
