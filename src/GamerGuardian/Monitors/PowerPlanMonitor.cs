using GamerGuardian.Models;
using GamerGuardian.Native;

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
            Apply: () => Task.Run(() => SetActivePlan(desiredGuid)),
            IsMonitored: pref.Monitor);
    }

    public static Guid ToGuid(PowerPlanChoice c) => c switch
    {
        PowerPlanChoice.Balanced => Balanced,
        PowerPlanChoice.HighPerformance => HighPerformance,
        PowerPlanChoice.PowerSaver => PowerSaver,
        PowerPlanChoice.UltimatePerformance => UltimatePerformance,
        _ => Balanced,
    };

    public static Guid GetActivePlan() => Powrprof.GetActiveScheme();

    public static Dictionary<Guid, string> ListAvailablePlans()
    {
        var result = new Dictionary<Guid, string>();
        try
        {
            foreach (var g in Powrprof.EnumerateSchemes())
            {
                var name = Powrprof.ReadFriendlyName(g) ?? g.ToString();
                result[g] = name;
            }
        }
        catch { }
        return result;
    }

    public static bool SetActivePlan(Guid g) => Powrprof.SetActiveScheme(g);
}
