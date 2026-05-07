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
        foreach (var def in ServiceCatalog.All)
        {
            if (def.RecommendedTarget.HasValue)
            {
                Assert.NotEqual(ServiceTargetState.Default, def.RecommendedTarget.Value);
            }
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
}
