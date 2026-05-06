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
        if (!pref.Monitor) yield break;

        var current = ReadCurrent();
        if (current.HasValue && current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "USB Selective Suspend (global override)",
            CurrentValue: (current ?? false) ? "Disabled (gaming)" : "Default",
            DesiredValue: desired ? "Disabled (gaming)" : "Default",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => ElevatedRegistry.SetHklmDword(
                SubKey, ValueName, desired ? 1u : 0u)));
    }

    public static bool? ReadCurrent()
    {
        using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
        if (k?.GetValue(ValueName) is int v) return v != 0;
        return false;
    }
}
