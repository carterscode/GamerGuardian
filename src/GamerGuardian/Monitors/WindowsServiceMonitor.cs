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

        var desired = pref.Desired switch
        {
            ServiceTargetState.Manual => ServiceStartType.Manual,
            ServiceTargetState.Disabled => ServiceStartType.Disabled,
            _ => _def.DefaultStartType,
        };
        if (current == desired) yield break;

        // For Want=Default we only flag drift when the current state matches one we
        // might have set ourselves (Disabled, or Manual when default isn't Manual).
        // This prevents churning on Windows-side state changes (triggers, updates)
        // for services the user hasn't asked us to enforce.
        if (pref.Desired == ServiceTargetState.Default)
        {
            bool weMightHaveSetIt = current == ServiceStartType.Disabled
                || (current == ServiceStartType.Manual && _def.DefaultStartType != ServiceStartType.Manual);
            if (!weMightHaveSetIt) yield break;
        }

        var actionWord = pref.Desired switch
        {
            ServiceTargetState.Disabled => "stop and disable",
            ServiceTargetState.Manual => "stop and set to Manual",
            _ => $"restore to {DescribeStart(_def.DefaultStartType)}",
        };
        var capturedDesired = pref.Desired;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "service",
            DisplayLabel: _def.DisplayName,
            Description: $"{_def.DisplayName} — {actionWord}",
            CurrentValue: DescribeStart(current),
            DesiredValue: DescribeStart(desired),
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() =>
            {
                switch (capturedDesired)
                {
                    case ServiceTargetState.Disabled:
                        WindowsServiceController.DisableElevated(_def.Name);
                        break;
                    case ServiceTargetState.Manual:
                        WindowsServiceController.SetManualElevated(_def.Name);
                        break;
                    default:
                        WindowsServiceController.RestoreDefaultElevated(_def.Name, _def.DefaultStartType);
                        break;
                }
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
