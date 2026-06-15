using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Windows 11 "suggested content" -- the silently-installed promo apps (Candy
/// Crush et al.), Start-menu app suggestions, and the "tips, tricks &amp;
/// suggestions" cards Windows surfaces. All live under the per-user
/// ContentDeliveryManager key as DWORDs where 0 = off. Intuitive Enabled/Disabled
/// semantics: <c>DesiredOn</c> = the feature enabled; the recommended pref is OFF.
/// Absent values count as "on" (Windows default), so the toggle works even on a
/// machine that has never touched these keys.
/// </summary>
public sealed class SuggestedContentMonitor : IMonitoredSetting
{
    public string Id => "debloat.suggestedcontent";
    private const string SubKey = @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";

    private static readonly string[] Values =
    {
        "SilentInstalledAppsEnabled",
        "OemPreInstalledAppsEnabled",
        "PreInstalledAppsEnabled",
        "SubscribedContent-338388Enabled", // Start app suggestions
        "SubscribedContent-338389Enabled", // tips/tricks/suggestions notifications
        "SubscribedContent-338393Enabled", // Settings suggested content
        "SubscribedContent-353694Enabled", // Settings suggested content
        "SubscribedContent-353696Enabled", // Settings suggested content
        "SoftLandingEnabled",              // Windows Tips popups
    };

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.SuggestedContent;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "debloat",
            DisplayLabel: "Debloat",
            Description: "Suggested content & silent app installs",
            CurrentValue: current.Value ? "Enabled" : "Disabled",
            DesiredValue: desired ? "Enabled" : "Disabled",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: current.Value ? "(default / suggestions on)" : "all ContentDeliveryManager suggestion values = 0",
            RawDesired: desired ? "(deleted / Windows default)" : "all ContentDeliveryManager suggestion values = 0");
    }

    /// <summary>True (on) unless every suggestion value is explicitly 0.</summary>
    public static bool? ReadCurrent()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(SubKey, writable: false);
            foreach (var v in Values)
            {
                var cur = k?.GetValue(v) as int?;
                if (cur != 0) return true; // absent or non-zero => still on
            }
            return false; // all explicitly disabled
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
                if (k is null) return;
                foreach (var v in Values) k.DeleteValue(v, throwOnMissingValue: false);
            }
            else
            {
                using var k = Registry.CurrentUser.CreateSubKey(SubKey, writable: true)!;
                foreach (var v in Values) k.SetValue(v, 0, RegistryValueKind.DWord);
            }
        }
        catch { }
    }
}
