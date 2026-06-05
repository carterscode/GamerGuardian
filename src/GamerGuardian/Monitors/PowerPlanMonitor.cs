using GamerGuardian.Models;
using GamerGuardian.Native;

namespace GamerGuardian.Monitors;

public sealed class PowerPlanMonitor : IMonitoredSetting
{
    public string Id => "powerplan";

    public static readonly Guid Balanced = new("381b4222-f694-41f0-9685-ff5bb260df2e");
    public static readonly Guid HighPerformance = new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
    // Corrected: the previous value (a1841308-1541-4fbf-...) was wrong (the repo's
    // documented "hardcoded power-scheme GUIDs lie" gotcha). The real Windows
    // Power Saver scheme GUID is a1841308-3541-4fab-bc81-f71556f20b4a.
    public static readonly Guid PowerSaver = new("a1841308-3541-4fab-bc81-f71556f20b4a");
    public static readonly Guid UltimatePerformance = new("e9a42b02-d5df-448d-aa00-03f14749eb61");

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.PowerPlan;

        var active = GetActivePlan();
        if (active == Guid.Empty) yield break;

        var desiredGuid = ResolveDesiredGuid(pref);
        if (desiredGuid == Guid.Empty) yield break;
        if (active == desiredGuid) yield break;

        var available = ListAvailablePlans();
        if (!available.TryGetValue(desiredGuid, out var planName)) yield break;

        var desiredName = pref.DesiredName ?? planName;

        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "Windows power plan",
            CurrentValue: available.GetValueOrDefault(active, active.ToString()),
            DesiredValue: desiredName,
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => SetActivePlan(desiredGuid)),
            IsMonitored: pref.Monitor,
            RawBefore: active.ToString(),
            RawDesired: desiredGuid.ToString());
    }

    public static Guid ResolveDesiredGuid(PowerPlanPref pref)
    {
        if (!string.IsNullOrEmpty(pref.DesiredGuid) && Guid.TryParse(pref.DesiredGuid, out var g))
            return g;
        return ToGuid(pref.Desired);
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

    /// <summary>
    /// Resolve a base scheme that is actually installed on this machine. Prefers
    /// an exact GUID match against the enumerated schemes (honouring the repo rule
    /// to verify against <see cref="Powrprof.EnumerateSchemes"/> rather than
    /// trusting a constant blindly), falling back to a friendly-name match.
    /// Returns <see cref="Guid.Empty"/> when no candidate is installed.
    /// </summary>
    public static Guid ResolveInstalledScheme(
        Guid wellKnownGuid, string friendlyNameContains,
        IEnumerable<Guid> installed, Func<Guid, string?> nameOf)
    {
        Guid nameMatch = Guid.Empty;
        foreach (var g in installed)
        {
            if (g == wellKnownGuid) return g;
            if (nameMatch == Guid.Empty)
            {
                var name = nameOf(g);
                if (name is not null &&
                    name.Contains(friendlyNameContains, StringComparison.OrdinalIgnoreCase))
                    nameMatch = g;
            }
        }
        return nameMatch;
    }

    /// <summary>Resolve the installed Balanced scheme to use as a clone base.</summary>
    public static Guid ResolveBalancedBase() =>
        ResolveInstalledScheme(Balanced, "Balanced", Powrprof.EnumerateSchemes(), Powrprof.ReadFriendlyName);
}
