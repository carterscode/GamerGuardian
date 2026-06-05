using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Activity History / Timeline collection and publishing. Three HKLM policy
/// values flipped together: <c>EnableActivityFeed</c>, <c>PublishUserActivities</c>,
/// <c>UploadUserActivities</c>. Absence means the Windows default (Activity History
/// ON). Inverted Gaming/Default semantics: <c>DesiredOn=true</c> means the gaming
/// state (all three = 0). Writes batch through one elevation prompt; reversal
/// deletes all three values to restore the default.
/// </summary>
public sealed class ActivityHistoryMonitor : IMonitoredSetting
{
    public string Id => "privacy.activityhistory";
    private const string SubKey = @"SOFTWARE\Policies\Microsoft\Windows\System";
    private static readonly string[] Values =
        { "EnableActivityFeed", "PublishUserActivities", "UploadUserActivities" };

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.ActivityHistory;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn; // true = gaming (all three disabled)
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "privacy",
            DisplayLabel: "Privacy",
            Description: desired
                ? "Activity History / Timeline -- disable collection via policy"
                : "Activity History / Timeline -- restore Windows default",
            CurrentValue: current.Value ? "Disabled (gaming)" : "Default",
            DesiredValue: desired ? "Disabled (gaming)" : "Default",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: current.Value
                ? "EnableActivityFeed=0, PublishUserActivities=0, UploadUserActivities=0"
                : "(default / unset)",
            RawDesired: desired
                ? "EnableActivityFeed=0, PublishUserActivities=0, UploadUserActivities=0"
                : "(deleted)");
    }

    /// <summary>True when Activity History is fully in the gaming state
    /// (all three policy values explicitly 0).</summary>
    public static bool? ReadCurrent()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
            bool allOff = true;
            foreach (var name in Values)
                allOff &= (k?.GetValue(name) as int?) == 0;
            return allOff;
        }
        catch { return null; }
    }

    public static void Apply(bool gaming)
    {
        if (gaming)
            ElevatedRegistry.SetHklmMulti(
                Array.ConvertAll(Values, n => (SubKey, n, "REG_DWORD", "0")));
        else
            ElevatedRegistry.DeleteHklmMulti(
                Array.ConvertAll(Values, n => (SubKey, n)));
    }
}
