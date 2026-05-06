using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

public sealed class HagsMonitor : IMonitoredSetting
{
    public string Id => "hags";
    private const string SubKey = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
    private const string ValueName = "HwSchMode";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.Hags;

        using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
        if (k?.GetValue(ValueName) is not int rawValue) yield break;
        var current = rawValue == 2;
        if (current == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        uint desiredRaw = desired ? 2u : 1u;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "Hardware-accelerated GPU Scheduling (HAGS) — requires reboot",
            CurrentValue: current ? "On" : "Off",
            DesiredValue: desired ? "On" : "Off",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => ElevatedRegistry.SetHklmDword(SubKey, ValueName, desiredRaw)),
            RequiresReboot: true,
            IsMonitored: pref.Monitor,
            RawBefore: rawValue.ToString(),
            RawDesired: desiredRaw.ToString());
    }

    public static bool? ReadCurrent()
    {
        using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
        if (k?.GetValue(ValueName) is int v) return v == 2;
        return null;
    }
}
