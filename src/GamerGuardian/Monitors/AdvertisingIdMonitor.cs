using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Per-user advertising identifier. When enabled, apps can read a stable
/// advertising ID to profile the user across sessions. Gaming/privacy-recommended
/// OFF. Direct HKCU value, so no elevation. Intuitive Enabled/Disabled semantics:
/// <c>DesiredOn</c> maps to the feature being enabled; the default pref is OFF
/// (the privacy-optimized value).
/// </summary>
public sealed class AdvertisingIdMonitor : IMonitoredSetting
{
    public string Id => "privacy.advertisingid";
    private const string SubKey = @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo";
    private const string ValueName = "Enabled";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.AdvertisingId;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        int rawBefore = current.Value ? 1 : 0;
        int rawDesired = desired ? 1 : 0;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "privacy",
            DisplayLabel: "Privacy",
            Description: "Advertising ID (per-user ad profiling)",
            CurrentValue: current.Value ? "Enabled" : "Disabled",
            DesiredValue: desired ? "Enabled" : "Disabled",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: $"Enabled={rawBefore}",
            RawDesired: $"Enabled={rawDesired}");
    }

    public static bool? ReadCurrent()
    {
        using var k = Registry.CurrentUser.OpenSubKey(SubKey, writable: false);
        if (k?.GetValue(ValueName) is int v) return v != 0;
        return null;
    }

    public static void Apply(bool on)
    {
        using var k = Registry.CurrentUser.CreateSubKey(SubKey, writable: true)!;
        k.SetValue(ValueName, on ? 1 : 0, RegistryValueKind.DWord);
    }
}
