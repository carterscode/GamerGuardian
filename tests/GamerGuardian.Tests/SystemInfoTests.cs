using System.Linq;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

/// <summary>
/// Covers the Status page's "This PC" cards. The formatters are pure and tested
/// directly; the card builders are exercised for shape and for the guarantee that
/// they never throw, since they read the registry and display APIs on machines whose
/// hardware we cannot predict.
/// </summary>
public class SystemInfoTests
{
    [Theory]
    [InlineData(null, "Unknown")]
    [InlineData(0UL, "Unknown")]
    public void FormatBytes_UnknownForMissingOrZero(ulong? input, string expected)
    {
        Assert.Equal(expected, SystemInfo.FormatBytes(input));
    }

    [Fact]
    public void FormatBytes_RendersGigabytes()
    {
        Assert.Equal("8 GB", SystemInfo.FormatBytes(8UL * 1024 * 1024 * 1024));
        Assert.Equal("31.9 GB", SystemInfo.FormatBytes(34300000000UL));
    }

    [Fact]
    public void FormatBytes_RendersTerabytesAboveAThousandGigabytes()
    {
        Assert.Equal("2 TB", SystemInfo.FormatBytes(2UL * 1024 * 1024 * 1024 * 1024));
    }

    [Fact]
    public void FormatBytes_DropsDecimalsOnLargeGigabyteValues()
    {
        // Above 100 GB the decimal is noise on a card this size.
        Assert.Equal("128 GB", SystemInfo.FormatBytes(128UL * 1024 * 1024 * 1024));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShortenGpuName_UnknownForMissing(string? input)
    {
        Assert.Equal("Unknown", SystemInfo.ShortenGpuName(input));
    }

    [Fact]
    public void ShortenGpuName_StripsVendorNoiseAndCollapsesWhitespace()
    {
        Assert.Equal("NVIDIA GeForce RTX 4080",
            SystemInfo.ShortenGpuName("NVIDIA(R)  GeForce RTX 4080 "));
        Assert.Equal("AMD Radeon RX 7900 XT",
            SystemInfo.ShortenGpuName("AMD Radeon™ RX 7900 XT"));
    }

    [Fact]
    public void ShortenGpuName_LeavesACleanNameAlone()
    {
        Assert.Equal("Intel Arc A770", SystemInfo.ShortenGpuName("Intel Arc A770"));
    }

    [Fact]
    public void CorrectEdition_RewritesWindows10ToWindows11AboveBuild22000()
    {
        // The registry's ProductName still says "Windows 10" on Windows 11, so an
        // unfiltered read shows the wrong OS on every Win11 machine -- which is
        // every machine this app supports.
        Assert.Equal("Windows 11 Pro", SystemInfo.CorrectEdition("Windows 10 Pro", "26200"));
        Assert.Equal("Windows 11 Home", SystemInfo.CorrectEdition("Windows 10 Home", "22000"));
    }

    [Fact]
    public void CorrectEdition_LeavesGenuineWindows10Alone()
    {
        Assert.Equal("Windows 10 Pro", SystemInfo.CorrectEdition("Windows 10 Pro", "19045"));
    }

    [Fact]
    public void CorrectEdition_LeavesAlreadyCorrectNamesAlone()
    {
        Assert.Equal("Windows 11 Enterprise", SystemInfo.CorrectEdition("Windows 11 Enterprise", "26200"));
    }

    [Theory]
    [InlineData(null, "26200")]
    [InlineData("", "26200")]
    public void CorrectEdition_UnknownForMissingProduct(string? product, string build)
    {
        Assert.Equal("Unknown", SystemInfo.CorrectEdition(product, build));
    }

    [Fact]
    public void CorrectEdition_UnparseableBuild_LeavesNameAlone()
    {
        Assert.Equal("Windows 10 Pro", SystemInfo.CorrectEdition("Windows 10 Pro", "not-a-number"));
        Assert.Equal("Windows 10 Pro", SystemInfo.CorrectEdition("Windows 10 Pro", null));
    }

    [Fact]
    public void All_ReturnsTheSixCards_InOrder()
    {
        var cards = SystemInfo.All();
        Assert.Equal(6, cards.Count);
        Assert.Equal(
            new[] { "Processor", "Graphics", "Memory", "Windows", "Displays", "Power plan" },
            cards.Select(c => c.Title).ToArray());
    }

    [Fact]
    public void All_CardsAlwaysCarryAtLeastOneRow_AndNeverThrow()
    {
        // These read the registry, display APIs and power schemes on whatever
        // hardware the suite runs on. They must degrade to "Unknown", not throw.
        foreach (var card in SystemInfo.All())
        {
            Assert.False(string.IsNullOrWhiteSpace(card.Title));
            Assert.False(string.IsNullOrWhiteSpace(card.Subtitle));
            Assert.NotEmpty(card.Rows);
            foreach (var (label, value) in card.Rows)
            {
                Assert.False(string.IsNullOrWhiteSpace(label));
                Assert.False(string.IsNullOrWhiteSpace(value));
            }
        }
    }

    [Fact]
    public void NoStorageCard_TheAppManagesNothingAboutDisks()
    {
        // Sparkle's dashboard has one because it ships a junk cleaner; GamerGuardian
        // does not, so a disk card would be decoration rather than context.
        Assert.DoesNotContain(SystemInfo.All(), c =>
            c.Title.Contains("Storage", System.StringComparison.OrdinalIgnoreCase) ||
            c.Title.Contains("Disk", System.StringComparison.OrdinalIgnoreCase));
    }
}
