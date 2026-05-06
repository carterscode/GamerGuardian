using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

public sealed class NetworkThrottlingMonitor : IMonitoredSetting
{
    public string Id => "netthrottle";

    private const string SubKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
    private const string ValueName = "NetworkThrottlingIndex";
    private const uint DisabledValue = 0xFFFFFFFF;
    private const uint DefaultValue = 10;

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.NetworkThrottling;

        using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
        if (k?.GetValue(ValueName) is not int rawValueInt) yield break;
        var rawValue = unchecked((uint)rawValueInt);
        var current = rawValue == DisabledValue;
        if (current == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        uint desiredRaw = desired ? DisabledValue : DefaultValue;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "Network Throttling (multimedia packet pacing)",
            CurrentValue: current ? "Disabled (gaming)" : "Default",
            DesiredValue: desired ? "Disabled (gaming)" : "Default",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => ElevatedRegistry.SetHklmDword(SubKey, ValueName, desiredRaw)),
            IsMonitored: pref.Monitor,
            RawBefore: rawValue == DisabledValue ? "0xFFFFFFFF" : rawValue.ToString(),
            RawDesired: desiredRaw == DisabledValue ? "0xFFFFFFFF" : desiredRaw.ToString());
    }

    public static bool? ReadCurrent()
    {
        using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
        if (k?.GetValue(ValueName) is int v)
        {
            unchecked { return (uint)v == DisabledValue; }
        }
        return false;
    }
}
