using GamerGuardian.Models;
using GamerGuardian.Services;

namespace GamerGuardian.Monitors;

/// <summary>
/// Monitors a single Windows scheduled task's enabled state against the user's
/// preference. One instance per <see cref="ScheduledTaskDefinition"/>; projected
/// from <see cref="ScheduledTaskCatalog.All"/> in <c>App.xaml.cs</c> (not in
/// fixedMonitors, same as <see cref="WindowsServiceMonitor"/>).
///
/// A task absent on this Windows build is treated as "not drift" (build-to-build
/// variance; the Exp variant may not exist). State is read without elevation; only
/// the disable/enable write prompts for UAC.
/// </summary>
public sealed class ScheduledTaskMonitor : IMonitoredSetting
{
    private readonly ScheduledTaskDefinition _def;
    private readonly Func<string, ScheduledTaskState> _readState;

    public ScheduledTaskMonitor(ScheduledTaskDefinition def)
        : this(def, ScheduledTaskController.QueryState) { }

    // Test seam: inject a state reader so the drift logic is testable without schtasks.
    public ScheduledTaskMonitor(ScheduledTaskDefinition def, Func<string, ScheduledTaskState> readState)
    {
        _def = def;
        _readState = readState;
    }

    public string Id => $"task:{_def.TaskPath.ToLowerInvariant()}";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        if (!config.ScheduledTasks.TryGetValue(_def.TaskPath, out var pref) || pref is null) yield break;
        if (!pref.Monitor) yield break;

        var current = _readState(_def.TaskPath);
        if (current == ScheduledTaskState.NotPresent) yield break;  // absent on this build -> not drift

        bool wantDisabled = pref.Desired == ScheduledTaskTarget.Disabled;
        bool currentlyDisabled = current == ScheduledTaskState.Disabled;
        if (wantDisabled == currentlyDisabled) yield break;

        // Want=Default churn-prevention: only flag drift when the task is currently
        // disabled (a state we might have set). If the user wants Default and the task
        // is already enabled, there's nothing to do.
        if (!wantDisabled && !currentlyDisabled) yield break;

        var captured = pref.Desired;
        var path = _def.TaskPath;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "task",
            DisplayLabel: _def.DisplayName,
            Description: wantDisabled
                ? $"{_def.DisplayName} — disable scheduled task"
                : $"{_def.DisplayName} — re-enable scheduled task",
            CurrentValue: currentlyDisabled ? "Disabled" : "Enabled",
            DesiredValue: wantDisabled ? "Disabled" : "Enabled",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() =>
            {
                if (captured == ScheduledTaskTarget.Disabled)
                    ScheduledTaskController.DisableElevated(new[] { path });
                else
                    ScheduledTaskController.EnableElevated(new[] { path });
            }),
            IsMonitored: pref.Monitor,
            RawBefore: $"Status={(currentlyDisabled ? "Disabled" : "Enabled")}",
            RawDesired: $"Status={(wantDisabled ? "Disabled" : "Enabled")}");
    }
}
