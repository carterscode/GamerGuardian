using GamerGuardian.Models;
using GamerGuardian.Services;

namespace GamerGuardian.Monitors;

/// <summary>
/// Monitors a single Windows service start type against the user's preference.
/// One instance per <see cref="ServiceDefinition"/>; registered N times in
/// <c>App.xaml.cs</c> from <see cref="ServiceCatalog.All"/>.
/// </summary>
public sealed class WindowsServiceMonitor : IMonitoredSetting
{
    private readonly ServiceDefinition _def;

    public WindowsServiceMonitor(ServiceDefinition def) { _def = def; }

    public string Id => $"service:{_def.Name.ToLowerInvariant()}";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        if (!WindowsServiceController.Exists(_def.Name)) yield break;
        if (!config.Services.TryGetValue(_def.Name, out var pref) || pref is null) yield break;

        var current = WindowsServiceController.ReadStartType(_def.Name);
        if (current == ServiceStartType.Unknown) yield break;

        var desired = pref.DesiredDisabled ? ServiceStartType.Disabled : _def.DefaultStartType;
        if (current == desired) yield break;

        // For "default" preference we only flag drift if the current state is *Disabled* —
        // otherwise we'd churn on services Windows naturally promotes Manual → Automatic
        // via triggers. The user explicitly chose to disable; the user explicitly chose
        // not to. Anything in between is fine.
        if (!pref.DesiredDisabled && current != ServiceStartType.Disabled) yield break;

        bool desiredDisabled = pref.DesiredDisabled;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "service",
            DisplayLabel: _def.DisplayName,
            Description: $"{_def.DisplayName} — {(desiredDisabled ? "stop and disable" : $"restore to {DescribeStart(_def.DefaultStartType)}")}",
            CurrentValue: DescribeStart(current),
            DesiredValue: DescribeStart(desired),
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() =>
            {
                if (desiredDisabled)
                    WindowsServiceController.DisableElevated(_def.Name);
                else
                    WindowsServiceController.RestoreDefaultElevated(_def.Name, _def.DefaultStartType);
            }),
            RequiresReboot: _def.RequiresReboot,
            IsMonitored: pref.Monitor,
            RawBefore: ((int)current).ToString(),
            RawDesired: ((int)desired).ToString());
    }

    public static string DescribeStart(ServiceStartType s) => s switch
    {
        ServiceStartType.Boot => "Boot",
        ServiceStartType.System => "System",
        ServiceStartType.Automatic => "Automatic",
        ServiceStartType.AutomaticDelayed => "Automatic (Delayed)",
        ServiceStartType.Manual => "Manual",
        ServiceStartType.Disabled => "Disabled",
        _ => "unknown",
    };
}
