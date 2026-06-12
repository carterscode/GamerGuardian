using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

public sealed class MemoryIntegrityMonitor : IMonitoredSetting
{
    public string Id => "memintegrity";

    private const string SubKey = @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity";
    private const string ValueName = "Enabled";

    /// <summary>VBS-off is a strict superset of Memory-Integrity-off: while the VBS
    /// monitor is actively holding the whole stack disabled it owns the HVCI key,
    /// and this monitor must not fight it (alternating writes + UAC prompts every
    /// poll). The Settings row shows "overridden by VBS" while this is true.</summary>
    public static bool DefersToVbs(AppConfig config) =>
        config.Global.Vbs is { Monitor: true, DesiredOn: false };

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.MemoryIntegrity;

        if (DefersToVbs(config)) yield break;

        using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
        if (k?.GetValue(ValueName) is not int rawValue) yield break;
        var current = rawValue != 0;

        // A prior full-VBS disable leaves markers (policy-mirror zeros, master
        // switch 0) that override this scenario key — writing Enabled=1 alone
        // would verify here yet do nothing after reboot. Treat those blockers as
        // drift when the user wants Memory Integrity on, and clear them in the
        // same apply.
        bool blocked = false;
        if (pref.DesiredOn)
        {
            try { blocked = VbsMonitor.HvciBlocked(VbsMonitor.ReadSnapshot()); }
            catch { }
        }

        if (current == pref.DesiredOn && !blocked) yield break;

        bool desired = pref.DesiredOn;
        uint desiredRaw = desired ? 1u : 0u;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "Memory Integrity (Core Isolation) — requires reboot"
                + (blocked ? " — also clears VBS disable markers (master switch + policy) that would keep it off" : ""),
            CurrentValue: current ? (blocked ? "On (blocked by VBS markers)" : "On") : "Off",
            DesiredValue: desired ? "On" : "Off",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            RequiresReboot: true,
            IsMonitored: pref.Monitor,
            RawBefore: blocked ? $"{rawValue} (VBS disable markers present)" : rawValue.ToString(),
            RawDesired: blocked ? $"{desiredRaw} (+ EVBS=1, policy zeros removed)" : desiredRaw.ToString());
    }

    public static bool? ReadCurrent()
    {
        using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
        if (k?.GetValue(ValueName) is int v) return v != 0;
        return null;
    }

    public static void Apply(bool on)
    {
        if (on)
        {
            VbsMonitor.VbsSnapshot? snap = null;
            try { snap = VbsMonitor.ReadSnapshot(); }
            catch { }
            if (snap is not null && VbsMonitor.HvciBlocked(snap))
            {
                var (adds, deletes) = VbsMonitor.BuildHvciUnblockOps(snap);
                adds.Add((SubKey, ValueName, "REG_DWORD", "1"));
                ElevatedRegistry.ApplyHklmBatch(adds, deletes);
                return;
            }
        }
        ElevatedRegistry.SetHklmDword(SubKey, ValueName, on ? 1u : 0u);
    }
}
