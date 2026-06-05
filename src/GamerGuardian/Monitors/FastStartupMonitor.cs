using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Fast Startup (hybrid boot). Saves the kernel session to the hiberfile on
/// shutdown so the next boot is faster, but it can leave drivers/hardware in a
/// stale state and skip a true cold boot -- gaming-recommended OFF. Driven by
/// the HKLM value <c>HiberbootEnabled</c> (0 = off = gaming). Requires a reboot
/// to take effect. Inverted Gaming/Default semantics: <c>DesiredOn=true</c> =
/// gaming (HiberbootEnabled=0).
/// </summary>
public sealed class FastStartupMonitor : IMonitoredSetting
{
    public string Id => "faststartup";
    private const string SubKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Power";
    private const string ValueName = "HiberbootEnabled";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.FastStartup;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn; // true = gaming (fast startup off)
        uint desiredRaw = desired ? 0u : 1u;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "Fast Startup (hybrid boot)",
            CurrentValue: current.Value ? "Disabled (gaming)" : "Default",
            DesiredValue: desired ? "Disabled (gaming)" : "Default",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => ElevatedRegistry.SetHklmDword(SubKey, ValueName, desiredRaw)),
            RequiresReboot: true,
            IsMonitored: pref.Monitor,
            RawBefore: current.Value ? "HiberbootEnabled=0" : "HiberbootEnabled=1",
            RawDesired: $"HiberbootEnabled={desiredRaw}");
    }

    /// <summary>True when Fast Startup is in the gaming state (HiberbootEnabled=0).
    /// Null when the value is absent (hibernate disabled -- not managed).</summary>
    public static bool? ReadCurrent()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
            if (k?.GetValue(ValueName) is int v) return v == 0;
            return null;
        }
        catch { return null; }
    }
}
