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

        // VBS-off is a strict superset of Memory-Integrity-off: while the VBS monitor
        // is actively holding the whole stack disabled it owns the HVCI key, and this
        // monitor must not fight it (alternating writes + UAC prompts every poll).
        if (config.Global.Vbs is { Monitor: true, DesiredOn: false }) yield break;

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
            Description: "Memory Integrity (Core Isolation) — requires reboot",
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
        if (k?.GetValue(ValueName) is int v) return v != 0;
        return null;
    }
}
