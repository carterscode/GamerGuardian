using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

public sealed class FullscreenOptimizationsMonitor : IMonitoredSetting
{
    public string Id => "fso";
    private const string SubKey = @"System\GameConfigStore";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.FullscreenOptimizations;

        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "Fullscreen optimizations (global)",
            CurrentValue: current.Value ? "On" : "Off",
            DesiredValue: desired ? "On" : "Off",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor);
    }

    public static bool? ReadCurrent()
    {
        using var k = Registry.CurrentUser.OpenSubKey(SubKey, writable: false);
        if (k?.GetValue("GameDVR_FSEBehaviorMode") is int v)
            return v != 2;
        if (k?.GetValue("GameDVR_DXGIHonorFSEWindowsCompatible") is int v2)
            return v2 != 0;
        return true;
    }

    public static void Apply(bool on)
    {
        using var k = Registry.CurrentUser.CreateSubKey(SubKey, writable: true)!;
        k.SetValue("GameDVR_FSEBehaviorMode", on ? 0 : 2, RegistryValueKind.DWord);
        k.SetValue("GameDVR_DXGIHonorFSEWindowsCompatible", on ? 1 : 0, RegistryValueKind.DWord);
        k.SetValue("GameDVR_HonorUserFSEBehaviorMode", on ? 0 : 1, RegistryValueKind.DWord);
        k.SetValue("GameDVR_EFSEFeatureFlags", 0, RegistryValueKind.DWord);
    }
}
