using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

public sealed class SystemResponsivenessMonitor : IMonitoredSetting
{
    public string Id => "sysresponse";

    private const string SubKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
    private const string ValueName = "SystemResponsiveness";
    private const uint GamingValue = 10;
    private const uint DefaultValue = 20;

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.SystemResponsiveness;

        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "System Responsiveness (multimedia CPU reservation)",
            CurrentValue: current.Value ? "Gaming" : "Default",
            DesiredValue: desired ? "Gaming" : "Default",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => ElevatedRegistry.SetHklmDword(
                SubKey, ValueName, desired ? GamingValue : DefaultValue)),
            RequiresReboot: true,
            IsMonitored: pref.Monitor);
    }

    public static bool? ReadCurrent()
    {
        using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
        if (k?.GetValue(ValueName) is int v) return v <= (int)GamingValue;
        return false;
    }
}
