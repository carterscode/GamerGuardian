using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

public sealed class GameModeMonitor : IMonitoredSetting
{
    public string Id => "gamemode";
    private const string SubKey = @"Software\Microsoft\GameBar";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.GameMode;

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
            Description: "Windows Game Mode",
            CurrentValue: current.Value ? "On" : "Off",
            DesiredValue: desired ? "On" : "Off",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: $"AutoGameModeEnabled={rawBefore}, AllowAutoGameMode={rawBefore}",
            RawDesired: $"AutoGameModeEnabled={rawDesired}, AllowAutoGameMode={rawDesired}");
    }

    public static bool? ReadCurrent()
    {
        using var key = Registry.CurrentUser.OpenSubKey(SubKey, writable: false);
        var v = key?.GetValue("AutoGameModeEnabled");
        if (v is int i) return i != 0;
        v = key?.GetValue("AllowAutoGameMode");
        if (v is int j) return j != 0;
        return null;
    }

    public static void Apply(bool on)
    {
        using var key = Registry.CurrentUser.CreateSubKey(SubKey, writable: true)!;
        key.SetValue("AutoGameModeEnabled", on ? 1 : 0, RegistryValueKind.DWord);
        key.SetValue("AllowAutoGameMode", on ? 1 : 0, RegistryValueKind.DWord);
    }
}
