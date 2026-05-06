using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

public sealed class VrrMonitor : IMonitoredSetting
{
    public string Id => "vrr";
    private const string SubKey = @"Software\Microsoft\DirectX\UserGpuPreferences";
    private const string ValueName = "DirectXUserGlobalSettings";
    private const string Key = "VRROptimizeEnable";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.Vrr;

        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "Variable Refresh Rate (Windows)",
            CurrentValue: current.Value ? "On" : "Off",
            DesiredValue: desired ? "On" : "Off",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: $"VRROptimizeEnable={(current.Value ? "1" : "0")}",
            RawDesired: $"VRROptimizeEnable={(desired ? "1" : "0")}");
    }

    public static bool? ReadCurrent()
    {
        using var k = Registry.CurrentUser.OpenSubKey(SubKey, writable: false);
        var raw = k?.GetValue(ValueName) as string;
        if (string.IsNullOrEmpty(raw)) return null;
        var pairs = ParsePairs(raw);
        if (!pairs.TryGetValue(Key, out var v)) return null;
        return v == "1";
    }

    public static void Apply(bool on)
    {
        using var k = Registry.CurrentUser.CreateSubKey(SubKey, writable: true)!;
        var raw = (k.GetValue(ValueName) as string) ?? string.Empty;
        var pairs = ParsePairs(raw);
        pairs[Key] = on ? "1" : "0";
        k.SetValue(ValueName, BuildPairs(pairs), RegistryValueKind.String);
    }

    private static Dictionary<string, string> ParsePairs(string raw)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in raw.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0) continue;
            d[part[..idx]] = part[(idx + 1)..];
        }
        return d;
    }

    private static string BuildPairs(Dictionary<string, string> pairs)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var kv in pairs)
            sb.Append(kv.Key).Append('=').Append(kv.Value).Append(';');
        return sb.ToString();
    }
}
