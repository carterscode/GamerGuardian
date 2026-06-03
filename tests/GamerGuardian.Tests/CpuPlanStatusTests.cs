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
    public void PlanActive_ServiceStopped_PartlyUnmet()
    {
        Assert.Equal(CcdDependencyStatus.PartlyUnmet,
            CpuPlanStatus.DependencyStatus(planActive: true, CcdServiceState.Stopped, gameBarEnabled: true));
    }

    [Fact]
    public void ServiceNotFound_Unknown()
    {
        Assert.Equal(CcdDependencyStatus.Unknown,
            CpuPlanStatus.DependencyStatus(planActive: true, CcdServiceState.NotFoundOrUnknown, gameBarEnabled: true));
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
}
