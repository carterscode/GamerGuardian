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

        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "Network Throttling (multimedia packet pacing)",
            CurrentValue: current.Value ? "Disabled (gaming)" : "Default",
            DesiredValue: desired ? "Disabled (gaming)" : "Default",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => ElevatedRegistry.SetHklmDword(
                SubKey, ValueName, desired ? DisabledValue : DefaultValue)),
            IsMonitored: pref.Monitor);
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
