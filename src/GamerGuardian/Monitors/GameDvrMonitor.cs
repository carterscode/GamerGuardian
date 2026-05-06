using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

public sealed class GameDvrMonitor : IMonitoredSetting
{
    public string Id => "gamedvr";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.GameDvr;

        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        int rawBefore = current.Value ? 1 : 0;
        int rawDesired = desired ? 1 : 0;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "Game DVR background recording",
            CurrentValue: current.Value ? "On" : "Off",
            DesiredValue: desired ? "On" : "Off",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: $"GameDVR_Enabled={rawBefore}, AppCaptureEnabled={rawBefore}",
            RawDesired: $"GameDVR_Enabled={rawDesired}, AppCaptureEnabled={rawDesired}");
    }

    public static bool? ReadCurrent()
    {
        using var ks = Registry.CurrentUser.OpenSubKey(@"System\GameConfigStore", writable: false);
        if (ks?.GetValue("GameDVR_Enabled") is int v) return v != 0;
        using var kp = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\GameDVR", writable: false);
        if (kp?.GetValue("AppCaptureEnabled") is int vp) return vp != 0;
        return null;
    }

    public static void Apply(bool on)
    {
        using (var k = Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore", writable: true)!)
            k.SetValue("GameDVR_Enabled", on ? 1 : 0, RegistryValueKind.DWord);
        using (var k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\GameDVR", writable: true)!)
            k.SetValue("AppCaptureEnabled", on ? 1 : 0, RegistryValueKind.DWord);
    }
}
