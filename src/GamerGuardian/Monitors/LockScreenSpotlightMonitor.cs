using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Windows Spotlight lock-screen "fun facts / tips" overlay and the ad-like
/// captions on the rotating lock-screen images. Per-user ContentDeliveryManager
/// DWORDs (0 = off). Intuitive Enabled/Disabled: <c>DesiredOn</c> = the overlay
/// enabled; recommended OFF.
///
/// <para>Note: this suppresses the tips/ads OVERLAY only. If your lock-screen
/// background is set to "Windows Spotlight," Windows may still rotate images;
/// switch the lock screen to Picture/Slideshow in Settings for a full opt-out.</para>
/// </summary>
public sealed class LockScreenSpotlightMonitor : IMonitoredSetting
{
    public string Id => "debloat.spotlight";
    private const string SubKey = @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";

    private static readonly string[] Values =
    {
        "RotatingLockScreenOverlayEnabled", // ad/fact overlay on the lock screen
        "SubscribedContent-338387Enabled",  // lock screen tips / fun facts
    };

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.LockScreenSpotlight;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "debloat",
            DisplayLabel: "Debloat",
            Description: "Lock screen tips, fun facts & ads",
            CurrentValue: current.Value ? "Enabled" : "Disabled",
            DesiredValue: desired ? "Enabled" : "Disabled",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: current.Value ? "(default / overlay on)" : "RotatingLockScreenOverlayEnabled=0, SubscribedContent-338387Enabled=0",
            RawDesired: desired ? "(deleted / Windows default)" : "RotatingLockScreenOverlayEnabled=0, SubscribedContent-338387Enabled=0");
    }

    public static bool? ReadCurrent()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(SubKey, writable: false);
            foreach (var v in Values)
            {
                var cur = k?.GetValue(v) as int?;
                if (cur != 0) return true;
            }
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
