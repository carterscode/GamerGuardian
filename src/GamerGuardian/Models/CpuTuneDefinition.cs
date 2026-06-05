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
public sealed record BiosRecommendation(string Name, string RecommendedValue, string Rationale);

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

    /// <summary>Stable friendly name for the GG-authored scheme.</summary>
    public string PlanName =>
        $"GamerGuardian Gaming [{(string.IsNullOrEmpty(Cpu.Model) ? "Generic" : Cpu.Model)}]";

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
