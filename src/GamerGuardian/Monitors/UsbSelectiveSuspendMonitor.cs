using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

public sealed class UsbSelectiveSuspendMonitor : IMonitoredSetting
{
    public string Id => "usbsuspend";

    private const string SubKey = @"SYSTEM\CurrentControlSet\Services\USB";
    private const string ValueName = "DisableSelectiveSuspend";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.UsbSelectiveSuspend;

        using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
        var rawObj = k?.GetValue(ValueName);
        var rawValue = rawObj is int v ? v : 0;
        var current = rawValue != 0;
        if (current == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        uint desiredRaw = desired ? 1u : 0u;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "USB Selective Suspend (global override)",
            CurrentValue: current ? "Disabled (gaming)" : "Default",
            DesiredValue: desired ? "Disabled (gaming)" : "Default",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => ElevatedRegistry.SetHklmDword(SubKey, ValueName, desiredRaw)),
            RequiresReboot: true,
            IsMonitored: pref.Monitor,
            RawBefore: rawObj is null ? "(unset)" : rawValue.ToString(),
            RawDesired: desiredRaw.ToString());
    }

    public static bool? ReadCurrent()
    {
        using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
        if (k?.GetValue(ValueName) is int v) return v != 0;
        return false;
    }
}
