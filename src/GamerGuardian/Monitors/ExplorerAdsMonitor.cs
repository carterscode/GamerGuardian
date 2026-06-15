using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// File Explorer "sync provider" notifications -- the OneDrive / Microsoft 365
/// upsell banners that appear in the Explorer navigation pane and status bar.
/// Single per-user Explorer\Advanced DWORD (0 = off). Intuitive Enabled/Disabled:
/// <c>DesiredOn</c> = the banners enabled; recommended OFF.
/// </summary>
public sealed class ExplorerAdsMonitor : IMonitoredSetting
{
    public string Id => "debloat.explorerads";
    private const string SubKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string ValueName = "ShowSyncProviderNotifications";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.ExplorerAds;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "debloat",
            DisplayLabel: "Debloat",
            Description: "File Explorer OneDrive / Office ad banners",
            CurrentValue: current.Value ? "Enabled" : "Disabled",
            DesiredValue: desired ? "Enabled" : "Disabled",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: current.Value ? "(default / banners on)" : "ShowSyncProviderNotifications=0",
            RawDesired: desired ? "(deleted / Windows default)" : "ShowSyncProviderNotifications=0");
    }

    /// <summary>True (on) unless the value is explicitly 0. Absent = Windows default on.</summary>
    public static bool? ReadCurrent()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(SubKey, writable: false);
            return (k?.GetValue(ValueName) as int?) != 0;
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
                k?.DeleteValue(ValueName, throwOnMissingValue: false);
            }
            else
            {
                using var k = Registry.CurrentUser.CreateSubKey(SubKey, writable: true)!;
                k.SetValue(ValueName, 0, RegistryValueKind.DWord);
            }
        }
        catch { }
    }
}
