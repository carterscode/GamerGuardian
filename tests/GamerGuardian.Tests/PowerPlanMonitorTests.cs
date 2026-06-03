using GamerGuardian.Models;
using GamerGuardian.Monitors;
using Xunit;

namespace GamerGuardian.Tests;

public class PowerPlanMonitorTests
{
    // Regression guard for the fixed Power Saver GUID. The old wrong value was
    // a1841308-1541-4fbf-8c20-7b0a7e3e9b8a; the real Windows GUID is below.
    [Fact]
    public void PowerSaver_HasCorrectWindowsGuid()
    {
        Assert.Equal(new Guid("a1841308-3541-4fab-bc81-f71556f20b4a"), PowerPlanMonitor.PowerSaver);
    }

    [Fact]
    public void PowerSaver_IsNotTheOldWrongGuid()
    {
        Assert.NotEqual(new Guid("a1841308-1541-4fbf-8c20-7b0a7e3e9b8a"), PowerPlanMonitor.PowerSaver);
    }

    // Existing-behavior guard: confirm the fix did not slip the ToGuid switch.
    [Fact]
    public void ToGuid_PowerSaver_ReturnsCorrectedGuid()
    {
        Assert.Equal(PowerPlanMonitor.PowerSaver, PowerPlanMonitor.ToGuid(PowerPlanChoice.PowerSaver));
    }

    [Fact]
    public void ToGuid_Balanced_Unchanged()
    {
        Assert.Equal(PowerPlanMonitor.Balanced, PowerPlanMonitor.ToGuid(PowerPlanChoice.Balanced));
    }

    [Fact]
    public void ResolveInstalledScheme_PrefersExactGuidMatch()
    {
        var balanced = PowerPlanMonitor.Balanced;
        var installed = new[] { Guid.NewGuid(), balanced, Guid.NewGuid() };

        var result = PowerPlanMonitor.ResolveInstalledScheme(
            balanced, "Balanced", installed, _ => "Some Other Name");

        Assert.Equal(balanced, result);
    }

    [Fact]
    public void ResolveInstalledScheme_FallsBackToFriendlyName()
    {
        var notInstalledWellKnown = Guid.NewGuid();
        var actual = Guid.NewGuid();
        var installed = new[] { Guid.NewGuid(), actual };

        var result = PowerPlanMonitor.ResolveInstalledScheme(
            notInstalledWellKnown, "Balanced", installed,
            g => g == actual ? "Balanced" : "Power saver");

        Assert.Equal(actual, result);
    }

    [Fact]
    public void ResolveInstalledScheme_ReturnsEmptyWhenAbsent()
    {
        var installed = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var result = PowerPlanMonitor.ResolveInstalledScheme(
            Guid.NewGuid(), "Balanced", installed, _ => "High performance");

        Assert.Equal(Guid.Empty, result);
    }
}
