using System.Security.Cryptography;
using System.Text;

namespace GamerGuardian.Models;

public enum TuneTier { Exact, Family, Generic }

/// <summary>Physical CCD layout (informational / for the UI and tests).</summary>
public enum CcdTopology { Unknown, Single, Dual }

/// <summary>
/// What the recipe does about core parking. <see cref="ParkFrequencyCcd"/> is
/// the ONLY strategy that parks a CCD — it applies exclusively to asymmetric
/// dual-CCD X3D (one V-cache CCD among two). Everything else leaves parking off
/// or at the Balanced default.
/// </summary>
public enum ParkingStrategy { Default, NoParking, ParkFrequencyCcd }

/// <summary>A single processor power-setting override applied to both rails.</summary>
public sealed record PowerOverride(Guid Subgroup, Guid Setting, uint Value, string Label);

/// <summary>An advisory BIOS recommendation (never read or applied by the app).</summary>
/// <param name="Alternative">A second defensible value and when to prefer it, for
/// settings where there is a real tradeoff rather than one right answer. Null when
/// the setting has a single sensible value.</param>
public sealed record BiosRecommendation(
    string Name, string RecommendedValue, string Rationale, string? Alternative = null);

/// <summary>
/// One catalog entry: how to recognize a CPU class and the gaming-optimized
/// recipe to build for it. Entries are tried in list order (exact, then family,
/// then generic). All recipes are Balanced clones; none use a High Performance
/// personality.
/// </summary>
public sealed record CpuTuneDefinition(
    string Key,
    TuneTier Tier,
    Func<CpuInfo, bool> Matches,
    CcdTopology Topology,
    ParkingStrategy Parking,
    IReadOnlyList<PowerOverride> Overrides,
    PowerPlanChoice RecommendedPrebuilt,
    bool GenericLabeled,
    IReadOnlyList<BiosRecommendation> Bios);

/// <summary>The resolved tune for a detected CPU.</summary>
public sealed record CpuTuneResult(
    CpuInfo Cpu,
    CpuTuneDefinition Definition,
    CcdTopology Topology,
    ParkingStrategy Parking,
    bool IsGeneric,
    PowerPlanChoice RecommendedPrebuilt,
    IReadOnlyList<PowerOverride> Overrides,
    IReadOnlyList<BiosRecommendation> Bios,
    string ContentHash)
{
    /// <summary>True only for the asymmetric dual-CCD X3D case, where the power
    /// plan is necessary-but-not-sufficient and depends on the AMD CCD-routing
    /// stack (BIOS CPPC=Driver, V-Cache Optimizer service, Game Bar).</summary>
    public bool NeedsCcdRoutingStack => Parking == ParkingStrategy.ParkFrequencyCcd;

    /// <summary>The stock Windows scheme the optimized plan is cloned from. Every
    /// recipe clones Balanced today (see <c>CpuTuneCatalog</c> and
    /// <c>CpuPlanBuilder.BuildOrActivate</c>'s Create path, which resolves the
    /// installed Balanced base). Surfaced in the plan name and the plan-details
    /// view so the user knows which base personality their custom plan sits on.</summary>
    public PowerPlanChoice BasePlan => PowerPlanChoice.Balanced;

    /// <summary>Human-facing base-plan label used in the plan name and details view.</summary>
    public string BasePlanDisplayName => BasePlan switch
    {
        PowerPlanChoice.Balanced => "Balanced",
        PowerPlanChoice.HighPerformance => "High Performance",
        PowerPlanChoice.PowerSaver => "Power Saver",
        PowerPlanChoice.UltimatePerformance => "Ultimate Performance",
        _ => "Balanced",
    };

    /// <summary>Stable friendly name for the GG-authored scheme. Includes the base
    /// Windows plan so the user can see what the custom plan is derived from
    /// (e.g. "GamerGuardian Gaming [9950X3D · Balanced]"). Changing this format
    /// only renames the app's own scheme on the next build/re-tune — identity and
    /// reuse are keyed on the stored GUID, not the name.</summary>
    public string PlanName =>
        $"GamerGuardian Gaming [{(string.IsNullOrEmpty(Cpu.Model) ? "Generic" : Cpu.Model)} · {BasePlanDisplayName}]";

    /// <summary>Deterministic hash of the resolved override set (machine- and
    /// run-independent), used by the plan builder to decide reuse vs re-tune.</summary>
    public static string ComputeHash(IEnumerable<PowerOverride> overrides)
    {
        var canonical = string.Join(";", overrides
            .Select(o => $"{o.Subgroup:N}:{o.Setting:N}:{o.Value}")
            .OrderBy(s => s, StringComparer.Ordinal));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes)[..16];
    }
}
