using GamerGuardian.Models;
using GamerGuardian.Monitors;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class ScheduledTaskMonitorTests
{
    private static ScheduledTaskDefinition Def =>
        new(@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
            "Microsoft Compatibility Appraiser", "desc", ScheduledTaskTarget.Disabled);

    private static AppConfig ConfigWith(ScheduledTaskPref pref)
    {
        var cfg = new AppConfig();
        cfg.ScheduledTasks[Def.TaskPath] = pref;
        return cfg;
    }

    private static ScheduledTaskMonitor MonitorReturning(ScheduledTaskState state) =>
        new(Def, _ => state);

    [Fact]
    public void Monitored_WantDisabled_TaskEnabled_YieldsDriftWithAutoApply()
    {
        var cfg = ConfigWith(new ScheduledTaskPref
        {
            Monitor = true,
            AutoApply = true,
            Desired = ScheduledTaskTarget.Disabled,
        });

        var drift = MonitorReturning(ScheduledTaskState.Enabled).CheckDrift(cfg).ToList();

        Assert.Single(drift);
        Assert.True(drift[0].AutoApply);
        Assert.Equal("Enabled", drift[0].CurrentValue);
        Assert.Equal("Disabled", drift[0].DesiredValue);
    }

    [Fact]
    public void Monitored_WantDisabled_TaskAlreadyDisabled_NoDrift()
    {
        var cfg = ConfigWith(new ScheduledTaskPref { Monitor = true, Desired = ScheduledTaskTarget.Disabled });
        Assert.Empty(MonitorReturning(ScheduledTaskState.Disabled).CheckDrift(cfg));
    }

    [Fact]
    public void NotPresentTask_NeverDrifts_AndNeverThrows()
    {
        var cfg = ConfigWith(new ScheduledTaskPref { Monitor = true, Desired = ScheduledTaskTarget.Disabled });
        Assert.Empty(MonitorReturning(ScheduledTaskState.NotPresent).CheckDrift(cfg));
    }

    [Fact]
    public void Unmonitored_WantDisabled_TaskEnabled_StillDrifts_SoManualApplyWorks()
    {
        // Manual Apply ("do it now") must disable a task the user wants Disabled even
        // when Monitor is off -- matching the app-wide rule that Apply ignores IsMonitored.
        var cfg = ConfigWith(new ScheduledTaskPref { Monitor = false, Desired = ScheduledTaskTarget.Disabled });
        var drift = MonitorReturning(ScheduledTaskState.Enabled).CheckDrift(cfg).ToList();
        Assert.Single(drift);
        Assert.False(drift[0].IsMonitored);   // background poll ignores it; Apply still acts
    }

    [Fact]
    public void FullyUnmanaged_DefaultAndUnmonitored_NeverDrifts_AndSkipsTheQuery()
    {
        // Default + unmonitored = the common "never engaged" state: no drift and the
        // state reader is never invoked (no background schtasks spawn).
        var cfg = ConfigWith(new ScheduledTaskPref { Monitor = false, Desired = ScheduledTaskTarget.Default });
        bool queried = false;
        var monitor = new ScheduledTaskMonitor(Def, _ => { queried = true; return ScheduledTaskState.Enabled; });
        Assert.Empty(monitor.CheckDrift(cfg));
        Assert.False(queried);
    }

    [Fact]
    public void NoPrefForTask_NeverDrifts()
    {
        // Config has no entry for this task -> untouched install, no drift.
        Assert.Empty(MonitorReturning(ScheduledTaskState.Enabled).CheckDrift(new AppConfig()));
    }

    [Fact]
    public void WantDefault_TaskDisabled_YieldsReEnableDrift()
    {
        var cfg = ConfigWith(new ScheduledTaskPref { Monitor = true, Desired = ScheduledTaskTarget.Default });
        var drift = MonitorReturning(ScheduledTaskState.Disabled).CheckDrift(cfg).ToList();
        Assert.Single(drift);
        Assert.Equal("Enabled", drift[0].DesiredValue);
    }

    [Fact]
    public void WantDefault_TaskEnabled_NoDrift()
    {
        // Want=Default churn prevention: an enabled task we don't manage is left alone.
        var cfg = ConfigWith(new ScheduledTaskPref { Monitor = true, Desired = ScheduledTaskTarget.Default });
        Assert.Empty(MonitorReturning(ScheduledTaskState.Enabled).CheckDrift(cfg));
    }
}
