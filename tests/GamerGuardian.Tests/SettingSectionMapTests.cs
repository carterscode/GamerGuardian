using System;
using System.Collections.Generic;
using System.Linq;
using GamerGuardian.Services;
using Xunit;
using Xunit.Abstractions;

namespace GamerGuardian.Tests;

/// <summary>
/// Guards the setting-id to section map. The central promise: every id the app can
/// actually produce resolves to a real section, and nothing silently lands in
/// <see cref="SettingSection.Unknown"/>.
/// </summary>
public class SettingSectionMapTests
{
    private readonly ITestOutputHelper _out;

    public SettingSectionMapTests(ITestOutputHelper output) => _out = output;

    /// <summary>
    /// Every setting id the app can produce, built the same way the monitors build
    /// them: the documented globals plus one id per catalog entry. Per-display
    /// instance ids ("hdr:KEY") are machine-dependent and covered separately.
    /// </summary>
    private static IEnumerable<string> AllKnownIds() =>
        SettingSectionMap.MappedIds.Keys
            .Concat(SettingDocsCatalog.All.Select(d => d.SettingId))
            .Concat(ServiceCatalog.All.Select(d => $"service:{d.Name.ToLowerInvariant()}"))
            .Concat(ScheduledTaskCatalog.All.Select(d => $"task:{d.TaskPath.ToLowerInvariant()}"))
            .Concat(WindowsAiAppCatalog.All.Select(d => $"ai.app:{d.PackageName}"))
            .Distinct(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void EveryKnownId_ResolvesToASection_NoneUnknown()
    {
        var unmapped = AllKnownIds()
            .Where(id => SettingSectionMap.SectionFor(id) == SettingSection.Unknown)
            .ToList();

        Assert.True(unmapped.Count == 0,
            "These setting ids do not map to a section: " + string.Join(", ", unmapped));
    }

    [Fact]
    public void EverySettingDocsCatalogEntry_ResolvesToASection()
    {
        foreach (var d in SettingDocsCatalog.All)
            Assert.True(SettingSectionMap.IsMapped(d.SettingId), $"unmapped: {d.SettingId}");
    }

    [Fact]
    public void EveryServiceCatalogEntry_MapsToServices()
    {
        Assert.NotEmpty(ServiceCatalog.All);
        foreach (var d in ServiceCatalog.All)
            Assert.Equal(SettingSection.Services,
                SettingSectionMap.SectionFor($"service:{d.Name.ToLowerInvariant()}"));
    }

    [Fact]
    public void EveryScheduledTaskCatalogEntry_MapsToServices()
    {
        Assert.NotEmpty(ScheduledTaskCatalog.All);
        foreach (var d in ScheduledTaskCatalog.All)
            Assert.Equal(SettingSection.Services,
                SettingSectionMap.SectionFor($"task:{d.TaskPath.ToLowerInvariant()}"));
    }

    [Fact]
    public void EveryWindowsAiAppCatalogEntry_MapsToWindowsAi()
    {
        Assert.NotEmpty(WindowsAiAppCatalog.All);
        foreach (var d in WindowsAiAppCatalog.All)
            Assert.Equal(SettingSection.WindowsAi,
                SettingSectionMap.SectionFor($"ai.app:{d.PackageName}"));
    }

    /// <summary>
    /// CpuTuneCatalog entries are keyed by recipe key ("amd-single-x3d-exact"), which
    /// are not setting ids. The one setting id the CPU tuning feature produces is
    /// "cpuplan" (see CpuPlanApply), and it must land on the CPU / Power section.
    /// </summary>
    [Fact]
    public void CpuTuneCatalog_ContributesCpuPlanId_MappedToCpuPower()
    {
        Assert.NotEmpty(CpuTuneCatalog.All);
        Assert.Equal(SettingSection.CpuPower, SettingSectionMap.SectionFor("cpuplan"));
        Assert.Equal(SettingSection.CpuPower, SettingSectionMap.SectionFor("powerplan"));
    }

    [Theory]
    [InlineData("hdr")]
    [InlineData("refresh")]
    [InlineData("resolution")]
    [InlineData("drr")]
    public void DisplayIds_MapToDisplay_BareAndPerInstance(string baseId)
    {
        // The catalog carries the bare id; the monitors emit "<base>:<displayKey>".
        Assert.Equal(SettingSection.Display, SettingSectionMap.SectionFor(baseId));
        Assert.Equal(SettingSection.Display, SettingSectionMap.SectionFor($"{baseId}:DISPLAY1"));
        Assert.Equal(SettingSection.Display, SettingSectionMap.SectionFor($"{baseId}:\\\\.\\DISPLAY2"));
    }

    [Fact]
    public void PrefixedIds_ResolveByFamily_EvenWhenTheEntryIsUnknownToTheDocsCatalog()
    {
        // Section is a property of the family, so an id for a service/task/package
        // with no docs entry still lands in the right section.
        Assert.Equal(SettingSection.Services, SettingSectionMap.SectionFor("service:notarealservice"));
        Assert.Equal(SettingSection.Services, SettingSectionMap.SectionFor(@"task:\Some\Unknown\Task"));
        Assert.Equal(SettingSection.WindowsAi, SettingSectionMap.SectionFor("ai.app:Not.A.Real.Package"));
    }

    [Fact]
    public void ServiceIdMatching_IsCaseInsensitive()
    {
        // The monitor lowercases the service name; the docs catalog keeps original
        // casing. Both spellings must resolve.
        Assert.Equal(SettingSection.Services, SettingSectionMap.SectionFor("service:DiagTrack"));
        Assert.Equal(SettingSection.Services, SettingSectionMap.SectionFor("service:diagtrack"));
        Assert.Equal(SettingSection.Gaming, SettingSectionMap.SectionFor("GameMode"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("definitely.not.a.real.setting")]
    [InlineData("privacy.notreal")]
    public void UnmappedOrEmptyIds_ReturnUnknown_AndDoNotThrow(string? id)
    {
        Assert.Equal(SettingSection.Unknown, SettingSectionMap.SectionFor(id));
        Assert.False(SettingSectionMap.IsMapped(id));
    }

    [Fact]
    public void NoIdMapsToBios_TheTabIsAdvisoryOnly()
    {
        Assert.DoesNotContain(SettingSection.Bios, SettingSectionMap.MappedIds.Values);
        Assert.DoesNotContain(AllKnownIds(), id => SettingSectionMap.SectionFor(id) == SettingSection.Bios);
    }

    /// <summary>
    /// Reports the per-section counts and pins the totals, so a setting added to a
    /// catalog without a section decision shows up as a failure here.
    /// </summary>
    [Fact]
    public void ReportPerSectionCounts()
    {
        var bySection = AllKnownIds()
            .GroupBy(SettingSectionMap.SectionFor)
            .ToDictionary(g => g.Key, g => g.Count());

        int Count(SettingSection s) => bySection.TryGetValue(s, out var n) ? n : 0;

        _out.WriteLine("Setting ids per section:");
        foreach (var s in Enum.GetValues<SettingSection>())
            _out.WriteLine($"  {s,-10} {Count(s)}");
        _out.WriteLine($"  {"TOTAL",-10} {bySection.Values.Sum()}");

        Assert.Equal(13, Count(SettingSection.Gaming));
        Assert.Equal(4, Count(SettingSection.Display));
        Assert.Equal(3, Count(SettingSection.CpuPower));
        Assert.Equal(6, Count(SettingSection.Telemetry));
        Assert.Equal(13, Count(SettingSection.WindowsAi));   // 9 policy toggles + 4 UWP packages
        Assert.Equal(3, Count(SettingSection.Network));
        Assert.Equal(8, Count(SettingSection.Debloat));
        Assert.Equal(33, Count(SettingSection.Services));    // 28 services + 5 scheduled tasks
        Assert.Equal(0, Count(SettingSection.Bios));
        Assert.Equal(0, Count(SettingSection.Unknown));
        Assert.Equal(83, bySection.Values.Sum());
    }
}
