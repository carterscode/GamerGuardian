using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Windows Feedback request frequency -- the periodic "rate your experience"
/// dialogs the OS pops (and which can fire often on fresh installs). Controlled
/// by the per-user Siuf\Rules\NumberOfSIUFInPeriod DWORD; 0 = never ask. We also
/// remove PeriodInNanoSeconds, whose presence overrides the count. Intuitive
/// Enabled/Disabled: <c>DesiredOn</c> = feedback prompts enabled; recommended OFF.
/// </summary>
public sealed class FeedbackNagMonitor : IMonitoredSetting
{
    public string Id => "debloat.feedback";
    private const string SubKey = @"Software\Microsoft\Siuf\Rules";
    private const string CountVal = "NumberOfSIUFInPeriod";
    private const string PeriodVal = "PeriodInNanoSeconds";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.FeedbackNag;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "debloat",
            DisplayLabel: "Debloat",
            Description: "Windows feedback request popups",
            CurrentValue: current.Value ? "Enabled" : "Disabled",
            DesiredValue: desired ? "Enabled" : "Disabled",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: current.Value ? "(default / Windows asks for feedback)" : "NumberOfSIUFInPeriod=0",
            RawDesired: desired ? "(deleted / Windows default)" : "NumberOfSIUFInPeriod=0 (PeriodInNanoSeconds removed)");
    }

    /// <summary>True (on) unless the count is explicitly 0.</summary>
    public static bool? ReadCurrent()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(SubKey, writable: false);
            return (k?.GetValue(CountVal) as int?) != 0;
        }
        catch { return null; }
    }

    public static void Apply(bool on)
    {
        try
        {
            if (on)
            {
                using var k = Registry.CurrentUser.OpenSubKey(SubKey, writable: true);
                k?.DeleteValue(CountVal, throwOnMissingValue: false);
            }
            else
            {
                using var k = Registry.CurrentUser.CreateSubKey(SubKey, writable: true)!;
                k.SetValue(CountVal, 0, RegistryValueKind.DWord);
                k.DeleteValue(PeriodVal, throwOnMissingValue: false);
            }
        }
        catch { }
    }
}
