using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

public sealed class VrrMonitor : IMonitoredSetting
{
    public string Id => "vrr";
    private const string SubKey = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
    private const string ValueName = "VRROptimizeEnable";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.Vrr;

        using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
        if (k?.GetValue(ValueName) is not int rawValue) yield break;
        var current = rawValue != 0;
        if (current == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        uint desiredRaw = desired ? 1u : 0u;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "Variable Refresh Rate (DirectX)",
            CurrentValue: current ? "On" : "Off",
            DesiredValue: desired ? "On" : "Off",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => ElevatedRegistry.SetHklmDword(SubKey, ValueName, desiredRaw)),
            IsMonitored: pref.Monitor,
            RawBefore: rawValue.ToString(),
            RawDesired: desiredRaw.ToString());
    }

    public static bool? ReadCurrent()
    {
        using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
        if (k?.GetValue(ValueName) is int v) return v != 0;
        return null;
    }
}
