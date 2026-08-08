using System.Globalization;
using GamerGuardian.Models;
using GamerGuardian.Native;

namespace GamerGuardian.Services;

/// <summary>
/// Produces the human-facing explanation of the GG-authored optimized power plan:
/// which stock Windows plan it is cloned from, exactly which processor settings it
/// changes (its diff from that base), and why those changes suit the detected CPU.
///
/// <para>Pure and unit-tested — the Settings "Plan details" view is just a render
/// of these strings. The change list is the recipe's overrides (each carries a
/// user-facing <see cref="PowerOverride.Label"/>); everything not listed is left at
/// the base plan's default, which is the whole point of cloning rather than
/// authoring a scheme from scratch.</para>
/// </summary>
public static class CpuPlanDetails
{
    /// <summary>One-line summary of the base plan the optimized plan sits on.</summary>
    public static string BaseSummary(CpuTuneResult r) =>
        $"Starts from the Windows \"{r.BasePlanDisplayName}\" plan and changes only the processor "
        + "power settings below. Everything else keeps its "
        + $"{r.BasePlanDisplayName} default, and your existing Windows plans are never modified.";

    /// <summary>The concrete settings the plan overrides vs. the stock base — the
    /// diff a user can verify. Each is already a friendly "Setting = value (why)"
    /// label from the catalog. Applied to both the plugged-in and on-battery rails.</summary>
    public static IReadOnlyList<string> Changes(CpuTuneResult r) =>
        r.Overrides.Select(o => o.Label).ToList();

    /// <summary>One row of the side-by-side "Windows Balanced vs GamerGuardian"
    /// comparison: the setting's friendly name and its value under each plan.
    /// <paramref name="Differs"/> is true only when the base value is known AND
    /// differs from the GamerGuardian value, so the UI can highlight the real
    /// differences without falsely flagging a value it couldn't read.</summary>
    public sealed record PlanComparisonRow(string Setting, string WindowsValue, string GamerGuardianValue, bool Differs);

    /// <summary>
    /// Builds the side-by-side comparison of the stock Windows base plan against the
    /// GamerGuardian tune, one row per processor setting the tune touches. The base
    /// (plugged-in) value is read through the injected <paramref name="readBaseAcValue"/>
    /// delegate — the UI passes a live Powrprof reader against the installed Balanced
    /// scheme; tests pass a stub. When a base value can't be read (setting hidden or
    /// scheme missing) the row shows "Windows default" and is not flagged as differing.
    /// </summary>
    public static IReadOnlyList<PlanComparisonRow> Comparison(
        CpuTuneResult r, Func<Guid, Guid, uint?> readBaseAcValue)
    {
        var rows = new List<PlanComparisonRow>(r.Overrides.Count);
        foreach (var o in r.Overrides)
        {
            var (name, fmt) = DescribeSetting(o.Setting);
            uint? baseVal = null;
            if (readBaseAcValue is not null)
            {
                try { baseVal = readBaseAcValue(o.Subgroup, o.Setting); }
                catch { baseVal = null; }
            }
            rows.Add(new PlanComparisonRow(
                Setting: name,
                WindowsValue: baseVal is uint w ? fmt(w) : "Windows default",
                GamerGuardianValue: fmt(o.Value),
                Differs: baseVal is uint b && b != o.Value));
        }
        return rows;
    }

    /// <summary>Friendly name + value formatter for each processor power setting the
    /// tunes use. Keyed on the well-known setting GUID so it never drifts from what
    /// the catalog actually writes.</summary>
    private static (string name, Func<uint, string> fmt) DescribeSetting(Guid setting)
    {
        if (setting == Powrprof.SettingBoostMode) return ("Processor boost mode", BoostModeText);
        if (setting == Powrprof.SettingCoreParkingMinCores) return ("Core parking — minimum cores", Percent);
        if (setting == Powrprof.SettingCoreParkingMaxCores) return ("Core parking — maximum cores", Percent);
        if (setting == Powrprof.SettingPerfIncreaseThreshold) return ("Performance-increase threshold", Percent);
        if (setting == Powrprof.SettingIdleDemoteThreshold) return ("Idle-demote threshold", Percent);
        if (setting == Powrprof.SettingMinProcessorState) return ("Minimum processor state", Percent);
        if (setting == Powrprof.SettingMaxProcessorState) return ("Maximum processor state", Percent);
        return ("Processor setting", Raw);
    }

    private static string Percent(uint v) => v.ToString(CultureInfo.InvariantCulture) + "%";
    private static string Raw(uint v) => v.ToString(CultureInfo.InvariantCulture);

    /// <summary>Windows PERFBOOSTMODE value names.</summary>
    private static string BoostModeText(uint v) => v switch
    {
        0 => "Disabled",
        1 => "Enabled",
        2 => "Aggressive",
        3 => "Efficient Enabled",
        4 => "Efficient Aggressive",
        5 => "Aggressive at guaranteed",
        6 => "Efficient Aggressive at guaranteed",
        _ => v.ToString(CultureInfo.InvariantCulture),
    };

    /// <summary>Why this specific recipe is a good fit for the detected CPU. Keyed
    /// on the parking strategy (the one dimension that actually changes the shape of
    /// the tune), with a generic-fallback and a hybrid-Intel special case.</summary>
    public static string Rationale(CpuTuneResult r)
    {
        if (r.Parking == ParkingStrategy.ParkFrequencyCcd)
            return "Your CPU has two core clusters (CCDs) but only one carries the extra 3D V-Cache "
                + "that games benefit from. This plan parks the other, higher-frequency cluster during "
                + "light loads so Windows keeps game threads on the cache cluster, while still unparking "
                + "every core under heavy multi-threaded work. The power plan alone does not decide which "
                + "cluster a game lands on: that comes from the BIOS setting \"CPPC Dynamic Preferred "
                + $"Cores\". Setting it to {CpuTuneCatalog.PreferredCppcValue} pins games to the cache cluster outright; leaving it on "
                + "Driver instead routes them dynamically and depends on the AMD 3D V-Cache Optimizer "
                + "service plus Xbox Game Bar recognising the game (see the dependency checklist above).";

        if (r.IsGeneric)
            return "A conservative, safe tune: it only raises boost aggressiveness and makes no "
                + "core-parking changes, so it is unlikely to hurt any CPU while still favouring "
                + "responsiveness over power saving.";

        if (r.Parking == ParkingStrategy.Default)
            return "This plan raises boost aggressiveness but deliberately leaves core parking to "
                + "Windows, whose Thread Director already routes work across your performance and "
                + "efficiency cores. Forcing parking changes on a hybrid CPU tends to hurt more than "
                + "it helps.";

        // NoParking — split the wording by topology so the reason is accurate.
        return r.Topology == CcdTopology.Single
            ? "All of your CPU's cores sit under the 3D V-Cache, so there is no \"wrong\" cluster to "
                + "avoid. This plan disables core parking so no core is idled mid-game and raises boost "
                + "aggressiveness for steadier frametimes."
            : "This plan keeps every core available (no parking) and raises boost aggressiveness. There "
                + "is no cache-preferred cluster to protect, so parking would only add wake-up latency "
                + "without helping.";
    }
}
