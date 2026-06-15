using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// The "Let's finish setting up your device" / SCOOBE full-screen and
/// notification nags that prompt to set up OneDrive, a Microsoft account, or a
/// Microsoft 365 subscription -- and resurface after feature updates. Driven by
/// the per-user UserProfileEngagement flag plus the device-setup notification
/// under ContentDeliveryManager. Intuitive Enabled/Disabled: <c>DesiredOn</c> =
/// the nag enabled; recommended OFF.
/// </summary>
public sealed class FinishSetupNagMonitor : IMonitoredSetting
{
    public string Id => "debloat.finishsetup";
    private const string EngagementKey = @"Software\Microsoft\Windows\CurrentVersion\UserProfileEngagement";
    private const string EngagementVal = "ScoobeSystemSettingEnabled";
    private const string CdmKey = @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
    private const string CdmVal = "SubscribedContent-310093Enabled"; // device-setup "finish setup" notifications

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.FinishSetupNag;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "debloat",
            DisplayLabel: "Debloat",
            Description: "\"Finish setting up your device\" nag",
            CurrentValue: current.Value ? "Enabled" : "Disabled",
            DesiredValue: desired ? "Enabled" : "Disabled",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: current.Value ? "(default / nag on)" : "ScoobeSystemSettingEnabled=0, SubscribedContent-310093Enabled=0",
            RawDesired: desired ? "(deleted / Windows default)" : "ScoobeSystemSettingEnabled=0, SubscribedContent-310093Enabled=0");
    }

    /// <summary>True (on) unless both nag flags are explicitly 0. Absent values
    /// count as on (the SCOOBE default), so the toggle applies on a fresh machine.</summary>
    public static bool? ReadCurrent()
    {
        try
        {
            using var e = Registry.CurrentUser.OpenSubKey(EngagementKey, writable: false);
            if ((e?.GetValue(EngagementVal) as int?) != 0) return true;
            using var c = Registry.CurrentUser.OpenSubKey(CdmKey, writable: false);
            if ((c?.GetValue(CdmVal) as int?) != 0) return true;
            return false;
        }
        catch { return null; }
    }

    public static void Apply(bool on)
    {
        try
        {
            if (on)
            {
                using (var e = Registry.CurrentUser.OpenSubKey(EngagementKey, writable: true))
                    e?.DeleteValue(EngagementVal, throwOnMissingValue: false);
                using (var c = Registry.CurrentUser.OpenSubKey(CdmKey, writable: true))
                    c?.DeleteValue(CdmVal, throwOnMissingValue: false);
            }
            else
            {
                using (var e = Registry.CurrentUser.CreateSubKey(EngagementKey, writable: true)!)
                    e.SetValue(EngagementVal, 0, RegistryValueKind.DWord);
                using (var c = Registry.CurrentUser.CreateSubKey(CdmKey, writable: true)!)
                    c.SetValue(CdmVal, 0, RegistryValueKind.DWord);
            }
        }
        catch { }
    }
}
