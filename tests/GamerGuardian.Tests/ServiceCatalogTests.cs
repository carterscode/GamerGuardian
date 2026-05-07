using GamerGuardian.Models;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class ServiceCatalogTests
{
    [Fact]
    public void All_ContainsServices()
    {
        Assert.NotEmpty(ServiceCatalog.All);
    }

    [Fact]
    public void All_HasNoDuplicateServiceNames()
    {
        var names = ServiceCatalog.All.Select(d => d.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void All_EveryEntryHasNonEmptyDisplayNameAndDescription()
    {
        foreach (var def in ServiceCatalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(def.Name), $"empty Name for an entry");
            Assert.False(string.IsNullOrWhiteSpace(def.DisplayName), $"empty DisplayName for {def.Name}");
            Assert.False(string.IsNullOrWhiteSpace(def.Description), $"empty Description for {def.Name}");
        }
    }

    [Fact]
    public void All_DefaultStartTypeIsKnown()
    {
        foreach (var def in ServiceCatalog.All)
        {
            Assert.NotEqual(ServiceStartType.Unknown, def.DefaultStartType);
        }
    }

    [Fact]
    public void RecommendedTarget_NeverDefault()
    {
        // RecommendedTarget == Default would mean "the preset moves it to where it
        // already is" which is meaningless. The convention is: leave RecommendedTarget
        // null for services that aren't in the preset, otherwise specify Manual or Disabled.
        foreach (var def in ServiceCatalog.All.Where(d => d.RecommendedTarget.HasValue))
        {
            Assert.NotEqual(ServiceTargetState.Default, def.RecommendedTarget!.Value);
        }
    }

    [Theory]
    [InlineData("DiagTrack")]
    [InlineData("MapsBroker")]
    [InlineData("Fax")]
    [InlineData("Spooler")]
    [InlineData("DoSvc")]
    [InlineData("iphlpsvc")]
    public void All_IncludesExpectedServices(string serviceName)
    {
        Assert.Contains(ServiceCatalog.All, d =>
            d.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DoSvc_UsesPolicyOverride()
    {
        // DoSvc is protected by Windows Update Medic Service — sc.exe writes
        // are reverted within seconds. The catalog must use the documented
        // Group Policy registry surface instead.
        var def = ServiceCatalog.All.Single(d =>
            d.Name.Equals("DoSvc", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(def.PolicyOverride);
        Assert.Equal(@"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", def.PolicyOverride!.PolicyKey);
        Assert.Equal("DODownloadMode", def.PolicyOverride.PolicyValue);
        Assert.Equal(0u, def.PolicyOverride.DisabledValue);
        Assert.True(def.RequiresReboot, "policy-managed services should mark RequiresReboot");
    }

    [Fact]
    public void PolicyOverride_OnlyOnServicesThatNeedIt()
    {
        // PolicyOverride is reserved for services Windows Update actively reverts.
        // If we ever add a regular service with a PolicyOverride by mistake we'd
        // bypass the SCM path with no good reason. Keep this list explicit.
        var withOverride = ServiceCatalog.All
            .Where(d => d.PolicyOverride is not null)
            .Select(d => d.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(new[] { "DoSvc" }, withOverride.OrderBy(s => s).ToArray());
    }
}
