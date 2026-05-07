using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Monitors a single Windows service start type against the user's preference.
/// One instance per <see cref="ServiceDefinition"/>; registered N times in
/// <c>App.xaml.cs</c> from <see cref="ServiceCatalog.All"/>.
///
/// Most services use the standard SCM path (sc.exe stop / config). For services
/// Windows actively reverts (DoSvc et al.) the definition can specify a
/// <see cref="PolicyOverride"/>; the monitor then writes the documented Group
/// Policy registry value instead, which Windows Update respects without revert.
/// </summary>
public sealed class WindowsServiceMonitor : IMonitoredSetting
{
    private readonly ServiceDefinition _def;

    public WindowsServiceMonitor(ServiceDefinition def) { _def = def; }

    public string Id => $"service:{_def.Name.ToLowerInvariant()}";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        if (!config.Services.TryGetValue(_def.Name, out var pref) || pref is null) yield break;

        if (_def.PolicyOverride is { } po)
        {
            foreach (var d in CheckPolicyDrift(po, pref)) yield return d;
            yield break;
        }

        if (!WindowsServiceController.Exists(_def.Name)) yield break;

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
            RawBefore: ToRegistryStartValue(current).ToString(),
            RawDesired: ToRegistryStartValue(desired).ToString());
    }

    /// <summary>
    /// Drift check for services that have a <see cref="PolicyOverride"/>. We
    /// don't touch the service start type at all — Windows reverts that. We
    /// read/write the policy registry value instead, which is what Windows
    /// Update components actually consult.
    ///
    /// "Disabled" maps to writing <see cref="PolicyOverride.DisabledValue"/>;
    /// "Default" maps to deleting the policy value (lets Windows use its built-in
    /// default). "Manual" is treated as Disabled because the policy surface is
    /// usually binary — if a service has a meaningful Manual state we wouldn't
    /// have given it a PolicyOverride.
    /// </summary>
    private IEnumerable<DriftItem> CheckPolicyDrift(PolicyOverride po, ServicePref pref)
    {
        int? current;
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(po.PolicyKey, writable: false);
            current = k?.GetValue(po.PolicyValue) as int?;
        }
        catch
        {
            yield break;
        }

        bool wantDisabled = pref.Desired == ServiceTargetState.Disabled
            || pref.Desired == ServiceTargetState.Manual;
        bool currentlyDisabled = current.HasValue && (uint)current.Value == po.DisabledValue;

        if (wantDisabled == currentlyDisabled) yield break;

        // Same Want=Default churn-prevention as the service path: only flag
        // drift when the current state matches one we might have set ourselves
        // (i.e. the policy is set to the Disabled value). If the policy holds
        // some other value the user or another tool wrote, leave it alone.
        if (!wantDisabled && !currentlyDisabled) yield break;

        var captured = pref.Desired;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "service",
            DisplayLabel: _def.DisplayName,
            Description: wantDisabled
                ? $"{_def.DisplayName} — apply Group Policy override ({po.Description})"
                : $"{_def.DisplayName} — restore Group Policy default (delete {po.PolicyValue})",
            CurrentValue: currentlyDisabled ? "Disabled by policy" : (current.HasValue ? $"policy={current.Value}" : "Default"),
            DesiredValue: wantDisabled ? "Disabled by policy" : "Default",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() =>
            {
                if (captured == ServiceTargetState.Default)
                    ElevatedRegistry.DeleteHklmValue(po.PolicyKey, po.PolicyValue);
                else
                    ElevatedRegistry.SetHklmDword(po.PolicyKey, po.PolicyValue, po.DisabledValue);
            }),
            RequiresReboot: _def.RequiresReboot,
            IsMonitored: pref.Monitor,
            RawBefore: current?.ToString() ?? "(unset)",
            RawDesired: wantDisabled ? po.DisabledValue.ToString() : "(deleted)");
    }

    /// <summary>
    /// Maps our enum to the actual Windows-side <c>Start</c> registry value, so
    /// log lines match what <c>sc qc</c> or <c>reg query</c> would show. Note
    /// that AutomaticDelayed is also written as 2 — the "delayed" part lives in
    /// a separate <c>DelayedAutostart</c> DWORD next to it.
    /// </summary>
    private static int ToRegistryStartValue(ServiceStartType s) => s switch
    {
        ServiceStartType.Boot => 0,
        ServiceStartType.System => 1,
        ServiceStartType.Automatic => 2,
        ServiceStartType.AutomaticDelayed => 2,
        ServiceStartType.Manual => 3,
        ServiceStartType.Disabled => 4,
        _ => -1,
    };

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
