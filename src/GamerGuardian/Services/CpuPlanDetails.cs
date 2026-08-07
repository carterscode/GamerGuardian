using GamerGuardian.Models;

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

    /// <summary>Why this specific recipe is a good fit for the detected CPU. Keyed
    /// on the parking strategy (the one dimension that actually changes the shape of
    /// the tune), with a generic-fallback and a hybrid-Intel special case.</summary>
    public static string Rationale(CpuTuneResult r)
    {
        if (r.Parking == ParkingStrategy.ParkFrequencyCcd)
            return "Your CPU has two core clusters (CCDs) but only one carries the extra 3D V-Cache "
                + "that games benefit from. This plan parks the other, higher-frequency cluster during "
                + "light loads so Windows keeps game threads on the cache cluster, while still unparking "
                + "every core under heavy multi-threaded work. It depends on the AMD 3D V-Cache Optimizer "
                + "service, Xbox Game Bar game-detection, and BIOS \"CPPC = Driver\" to route correctly "
                + "(see the dependency checklist above).";

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
