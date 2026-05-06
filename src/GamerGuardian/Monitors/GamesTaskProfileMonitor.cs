using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

public sealed class GamesTaskProfileMonitor : IMonitoredSetting
{
    public string Id => "gamestask";

    private const string SubKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";

    private static readonly (string Name, string Kind, string Default, string Gaming)[] Values =
    {
        ("Priority",            "REG_DWORD", "2",      "6"),
        ("Scheduling Category", "REG_SZ",    "Medium", "High"),
        ("SFIO Priority",       "REG_SZ",    "Normal", "High"),
    };

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.GamesTaskProfile;
        if (!pref.Monitor) yield break;

        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "Games multimedia task profile (priority/scheduling)",
            CurrentValue: current.Value ? "Gaming" : "Default",
            DesiredValue: desired ? "Gaming" : "Default",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => ApplyValues(desired)));
    }

    public static bool? ReadCurrent()
    {
        using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
        if (k is null) return null;
        foreach (var v in Values)
        {
            var raw = k.GetValue(v.Name);
            string actual = raw is int i ? i.ToString() : raw?.ToString() ?? "";
            if (!string.Equals(actual, v.Gaming, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    private static bool ApplyValues(bool gaming)
    {
        var writes = Values.Select(v =>
            (subkey: SubKey, name: v.Name, kind: v.Kind, data: gaming ? v.Gaming : v.Default));
        return ElevatedRegistry.SetHklmMulti(writes);
    }
}
