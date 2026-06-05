using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Windows Power Throttling. The OS throttles background/idle threads to save
/// power; gaming-recommended OFF for sustained performance. Driven by the HKLM
/// value <c>PowerThrottlingOff</c> (1 = throttling disabled = gaming). Absence
/// means the Windows default (throttling ON). This is a plain registry setting,
/// not a power-scheme operation. Inverted Gaming/Default semantics:
/// <c>DesiredOn=true</c> = gaming (PowerThrottlingOff=1).
/// </summary>
public sealed class PowerThrottlingMonitor : IMonitoredSetting
{
    public string Id => "powerthrottling";
    private const string SubKey = @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling";
    private const string ValueName = "PowerThrottlingOff";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.PowerThrottling;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn; // true = gaming (throttling off)
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "Windows Power Throttling (sustained-performance)",
            CurrentValue: current.Value ? "Disabled (gaming)" : "Default",
            DesiredValue: desired ? "Disabled (gaming)" : "Default",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: current.Value ? "PowerThrottlingOff=1" : "(default / unset)",
            RawDesired: desired ? "PowerThrottlingOff=1" : "(deleted)");
    }

    /// <summary>True when Power Throttling is in the gaming state (explicitly off).</summary>
    public static bool? ReadCurrent()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
            var v = k?.GetValue(ValueName) as int?;
            return v == 1;
        }
        catch { return null; }
    }

    public static void Apply(bool gaming)
    {
        if (gaming)
            ElevatedRegistry.SetHklmDword(SubKey, ValueName, 1);
        else
            ElevatedRegistry.DeleteHklmValue(SubKey, ValueName);
    }
}
