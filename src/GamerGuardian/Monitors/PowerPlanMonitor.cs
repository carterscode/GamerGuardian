using System.Diagnostics;
using System.Text.RegularExpressions;
using GamerGuardian.Models;

namespace GamerGuardian.Monitors;

public sealed class PowerPlanMonitor : IMonitoredSetting
{
    public string Id => "powerplan";

    public static readonly Guid Balanced = new("381b4222-f694-41f0-9685-ff5bb260df2e");
    public static readonly Guid HighPerformance = new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
    public static readonly Guid PowerSaver = new("a1841308-1541-4fbf-8c20-7b0a7e3e9b8a");
    public static readonly Guid UltimatePerformance = new("e9a42b02-d5df-448d-aa00-03f14749eb61");

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.PowerPlan;
        if (!pref.Monitor) yield break;

        var active = GetActivePlan();
        if (active == Guid.Empty) yield break;

        var desiredGuid = ToGuid(pref.Desired);
        if (active == desiredGuid) yield break;

        var available = ListAvailablePlans();
        if (!available.ContainsKey(desiredGuid)) yield break;

        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "Windows power plan",
            CurrentValue: available.GetValueOrDefault(active, active.ToString()),
            DesiredValue: pref.Desired.ToString(),
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => SetActivePlan(desiredGuid)));
    }

    public static Guid ToGuid(PowerPlanChoice c) => c switch
    {
        PowerPlanChoice.Balanced => Balanced,
        PowerPlanChoice.HighPerformance => HighPerformance,
        PowerPlanChoice.PowerSaver => PowerSaver,
        PowerPlanChoice.UltimatePerformance => UltimatePerformance,
        _ => Balanced,
    };

    public static Guid GetActivePlan()
    {
        var output = Run("powercfg.exe", "/getactivescheme");
        if (output is null) return Guid.Empty;
        var m = Regex.Match(output, @"([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})", RegexOptions.IgnoreCase);
        return m.Success ? Guid.Parse(m.Value) : Guid.Empty;
    }

    public static Dictionary<Guid, string> ListAvailablePlans()
    {
        var dict = new Dictionary<Guid, string>();
        var output = Run("powercfg.exe", "/list");
        if (output is null) return dict;
        foreach (Match m in Regex.Matches(output, @"([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})\s+\(([^)]+)\)", RegexOptions.IgnoreCase))
        {
            if (Guid.TryParse(m.Groups[1].Value, out var g))
                dict[g] = m.Groups[2].Value.Trim();
        }
        return dict;
    }

    public static bool SetActivePlan(Guid g) => Run("powercfg.exe", $"/setactive {g}") != null;

    private static string? Run(string file, string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return null;
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5_000);
            return p.HasExited && p.ExitCode == 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }
}
