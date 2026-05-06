using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

public sealed class MemoryIntegrityMonitor : IMonitoredSetting
{
    public string Id => "memintegrity";

    private const string SubKey = @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity";
    private const string ValueName = "Enabled";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.MemoryIntegrity;
        if (!pref.Monitor) yield break;

        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "Memory Integrity (Core Isolation) — requires reboot",
            CurrentValue: current.Value ? "On" : "Off",
            DesiredValue: desired ? "On" : "Off",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => ElevatedRegistry.SetHklmDword(SubKey, ValueName, desired ? 1u : 0u)));
    }

    public static bool? ReadCurrent()
    {
        using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
        if (k?.GetValue(ValueName) is int v) return v != 0;
        return null;
    }
}
