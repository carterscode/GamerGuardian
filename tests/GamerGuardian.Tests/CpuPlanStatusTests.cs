using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class CpuPlanStatusTests
{
    [Fact]
    public void PlanActive_ServiceRunning_Met()
    {
        Assert.Equal(CcdDependencyStatus.Met,
            CpuPlanStatus.DependencyStatus(planActive: true, CcdServiceState.Running, gameBarEnabled: true));
    }

    [Fact]
    public void PlanActive_ServiceIdle_IsMet()
    {
        // The reported bug: AMD's optimizer is installed with StartMode=Auto and sits
        // stopped until it has routing work, so idle is its normal state. Treating it
        // as unmet told users with working installs to go and fix nothing.
        Assert.Equal(CcdDependencyStatus.Met,
            CpuPlanStatus.DependencyStatus(planActive: true, CcdServiceState.Idle, gameBarEnabled: true));
    }

    [Fact]
    public void PlanActive_ServiceDisabled_PartlyUnmet()
    {
        // A Disabled start type is the one service state the user must actually act on.
        Assert.Equal(CcdDependencyStatus.PartlyUnmet,
            CpuPlanStatus.DependencyStatus(planActive: true, CcdServiceState.Disabled, gameBarEnabled: true));
    }

    [Fact]
    public void ServiceNotInstalled_Unknown()
    {
        Assert.Equal(CcdDependencyStatus.Unknown,
            CpuPlanStatus.DependencyStatus(planActive: true, CcdServiceState.NotInstalled, gameBarEnabled: true));
    }

    [Fact]
    public void PlanInactive_PartlyUnmet()
    {
        Assert.Equal(CcdDependencyStatus.PartlyUnmet,
            CpuPlanStatus.DependencyStatus(planActive: false, CcdServiceState.Running, gameBarEnabled: true));
    }

    [Fact]
    public void GameBarDisabled_PartlyUnmet()
    {
        Assert.Equal(CcdDependencyStatus.PartlyUnmet,
            CpuPlanStatus.DependencyStatus(planActive: true, CcdServiceState.Running, gameBarEnabled: false));
    }

    [Fact]
    public void GameBarUnknown_DoesNotBlockMet()
    {
        Assert.Equal(CcdDependencyStatus.Met,
            CpuPlanStatus.DependencyStatus(planActive: true, CcdServiceState.Running, gameBarEnabled: null));
    }

    // ---- Service matching -------------------------------------------------

    [Fact]
    public void Matcher_FindsTheServiceActuallyShipped()
    {
        // Verified against a real install: the service is named "amd3dvcacheSvc"
        // with display name "AMD 3D V-Cache Performance Optimizer Service".
        Assert.True(CpuPlanStatus.LooksLikeVCacheOptimizer(
            "amd3dvcacheSvc", "AMD 3D V-Cache Performance Optimizer Service"));
    }

    [Theory]
    [InlineData("AMD3DVCacheSvc")]
    [InlineData("Amd3DVCacheSvc")]
    [InlineData("amd3dvcachesvc")]
    public void Matcher_IsCaseInsensitiveOnTheServiceName(string name)
    {
        Assert.True(CpuPlanStatus.LooksLikeVCacheOptimizer(name, null));
    }

    [Fact]
    public void Matcher_FallsBackToTheDisplayNameWhenTheServiceIsRenamed()
    {
        // The point of matching rather than probing a fixed list: a driver update
        // that renames the service must not turn into "install AMD chipset drivers"
        // on a machine that already has them.
        Assert.True(CpuPlanStatus.LooksLikeVCacheOptimizer(
            "AmdSomethingElseSvc", "AMD 3D V-Cache Optimizer"));
    }

    [Theory]
    [InlineData("AmdPpkgSvc", "AMD Provisioning Packages Service")]
    [InlineData("AMD External Events Utility", "AMD External Events Utility")]
    [InlineData("AmdAppCompatSvc", "AMD Application Compatibility Database Service")]
    [InlineData("FontCache", "Windows Font Cache Service")]
    public void Matcher_DoesNotClaimUnrelatedServices(string name, string display)
    {
        // These all exist alongside it on a real AMD install; matching any of them
        // would report the routing stack present when it is not. Note the old code
        // probed "AMDProvisioningPackagesSvc" as if it were the optimizer.
        Assert.False(CpuPlanStatus.LooksLikeVCacheOptimizer(name, display));
    }

    [Fact]
    public void RealMachine_DetectorNeverThrowsAndReportsCoherently()
    {
        // Exercises the detector against this machine's real service list rather
        // than a mock. It cannot assert a particular state -- CI runners have no AMD
        // service -- but it can assert the result is self-consistent: a state other
        // than NotInstalled must name the service it found, and NotInstalled must
        // not.
        var info = CpuPlanStatus.ReadAmdVCacheService();

        if (info.State is CcdServiceState.NotInstalled or CcdServiceState.Unreadable)
        {
            Assert.Null(info.ServiceName);
        }
        else
        {
            Assert.False(string.IsNullOrWhiteSpace(info.ServiceName));
            Assert.True(CpuPlanStatus.LooksLikeVCacheOptimizer(info.ServiceName, info.DisplayName));
        }
    }

    [Fact]
    public void ServiceUnreadable_IsUnknownNotAbsent()
    {
        // The panel used to state "isn't installed" as fact when the read had simply
        // failed. Unreadable and NotInstalled both mean "can't confirm the stack",
        // but only one of them justifies telling the user to install something.
        Assert.Equal(CcdDependencyStatus.Unknown,
            CpuPlanStatus.DependencyStatus(planActive: true, CcdServiceState.Unreadable, gameBarEnabled: true));
    }

    [Fact]
    public void Matcher_FindsTheKernelDriverAsWellAsTheWin32Service()
    {
        // AMD's INF package registers BOTH: a kernel driver "amd3dvcache" and a
        // user-mode helper "amd3dvcacheSvc". ServiceController.GetServices() returns
        // Win32 services only, so a machine carrying just the driver looked like it
        // had no routing stack at all. Both must match.
        Assert.True(CpuPlanStatus.LooksLikeVCacheOptimizer("amd3dvcache", null));
        Assert.True(CpuPlanStatus.LooksLikeVCacheOptimizer("amd3dvcacheSvc", null));
    }

    // ---- Display names -----------------------------------------------------

    [Fact]
    public void CleanDisplayName_ResolvesTheIndirectStringInfInstalledServicesUse()
    {
        // Verified against the real key: INF-installed services store DisplayName as
        // an indirect string whose readable fallback follows the last semicolon.
        Assert.Equal("AMD 3D V-Cache Performance Optimizer Service",
            CpuPlanStatus.CleanDisplayName(
                "@oem46.inf,%amd3dvcacheSvc.DisplayName%;AMD 3D V-Cache Performance Optimizer Service"));
    }

    [Fact]
    public void CleanDisplayName_LeavesAPlainNameAlone()
    {
        Assert.Equal("AMD 3D V-Cache Performance Optimizer Service",
            CpuPlanStatus.CleanDisplayName("AMD 3D V-Cache Performance Optimizer Service"));
    }

    [Fact]
    public void CleanDisplayName_ReturnsNullRatherThanAnUnresolvedIndirectString()
    {
        // Showing "@oem46.inf,%amd3dvcacheSvc.DisplayName%" in the panel would be
        // worse than showing nothing.
        Assert.Null(CpuPlanStatus.CleanDisplayName("@oem46.inf,%amd3dvcacheSvc.DisplayName%"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CleanDisplayName_HandlesMissingValues(string? raw)
    {
        Assert.Null(CpuPlanStatus.CleanDisplayName(raw));
    }

    [Fact]
    public void Matcher_HandlesNulls()
    {
        Assert.False(CpuPlanStatus.LooksLikeVCacheOptimizer(null, null));
    }
}
