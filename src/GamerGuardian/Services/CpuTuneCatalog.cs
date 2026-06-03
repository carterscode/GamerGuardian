using GamerGuardian.Models;
using GamerGuardian.Native;

namespace GamerGuardian.Services;

/// <summary>
/// Static catalog of per-CPU gaming tunes, prebuilt recommendations, and
/// advisory BIOS guidance. Mirrors the <c>ServiceCatalog</c> pattern. The
/// catalog is the single owner of CCD topology + parking-strategy classification
/// (CpuDetector deliberately does not classify these).
///
/// <para>Key principle: the "park one CCD" rule keys on <b>cache asymmetry</b>
/// (one V-cache CCD among two), NOT raw CCD count. Single-CCD parts, symmetric
/// non-X3D dual-CCD parts, and the dual-V-cache 9950X3D2 all want parking OFF;
/// only asymmetric dual-CCD X3D parks the frequency CCD. No entry uses a High
/// Performance personality — every recipe is a Balanced clone.</para>
/// </summary>
public static class CpuTuneCatalog
{
    // Asymmetric dual-CCD X3D — one V-cache CCD among two. These park the
    // frequency CCD so games stay on the cache CCD.
    private static readonly HashSet<string> ParkFrequencyCcdModels =
        new(StringComparer.OrdinalIgnoreCase) { "9950X3D", "9900X3D", "7950X3D", "7900X3D" };

    // Single-CCD X3D — all cores under the V-cache, no parking wanted.
    private static readonly HashSet<string> SingleCcdX3dModels =
        new(StringComparer.OrdinalIgnoreCase) { "9850X3D", "9800X3D", "7800X3D" };

    // ---- Override builders (each value applied to both AC and DC rails) ----

    private static PowerOverride Ov(Guid setting, uint value, string label) =>
        new(Powrprof.SubProcessor, setting, value, label);

    private const uint BoostAggressive = 2;

    // The single-CCD recipe verified live on the developer's 9850X3D plan.
    private static IReadOnlyList<PowerOverride> SingleCcdRecipe() => new[]
    {
        Ov(Powrprof.SettingBoostMode, BoostAggressive, "Processor boost mode = Aggressive"),
        Ov(Powrprof.SettingCoreParkingMinCores, 100, "Core parking min cores = 100 (no parking)"),
        Ov(Powrprof.SettingPerfIncreaseThreshold, 60, "Perf increase threshold = 60"),
        Ov(Powrprof.SettingIdleDemoteThreshold, 10, "Idle demote threshold = 10"),
        Ov(Powrprof.SettingMinProcessorState, 5, "Minimum processor state = 5"),
        Ov(Powrprof.SettingMaxProcessorState, 100, "Maximum processor state = 100"),
    };

    // Asymmetric dual-CCD X3D: park the frequency CCD (min cores 50), full cores
    // available under load (max 100). Never min=100.
    private static IReadOnlyList<PowerOverride> AsymmetricDualRecipe() => new[]
    {
        Ov(Powrprof.SettingBoostMode, BoostAggressive, "Processor boost mode = Aggressive"),
        Ov(Powrprof.SettingCoreParkingMinCores, 50, "Core parking min cores = 50 (park frequency CCD)"),
        Ov(Powrprof.SettingCoreParkingMaxCores, 100, "Core parking max cores = 100"),
    };

    // Symmetric (non-X3D dual, single-CCD non-X3D, dual-V-cache X3D2): boost +
    // explicitly no parking.
    private static IReadOnlyList<PowerOverride> NoParkRecipe() => new[]
    {
        Ov(Powrprof.SettingBoostMode, BoostAggressive, "Processor boost mode = Aggressive"),
        Ov(Powrprof.SettingCoreParkingMinCores, 100, "Core parking min cores = 100 (no parking)"),
        Ov(Powrprof.SettingCoreParkingMaxCores, 100, "Core parking max cores = 100"),
    };

    // Intel hybrid / generic: aggressive boost only, leave Balanced's parking so
    // Thread Director + Game Bar keep managing P/E cores.
    private static IReadOnlyList<PowerOverride> BoostOnlyRecipe() => new[]
    {
        Ov(Powrprof.SettingBoostMode, BoostAggressive, "Processor boost mode = Aggressive"),
    };

    // ---- BIOS guidance (advisory only) ----

    private static IReadOnlyList<BiosRecommendation> BiosAmdDualCcd() => new[]
    {
        new BiosRecommendation("CPPC Dynamic Preferred Cores", "Driver",
            "Lets the AMD 3D V-Cache Optimizer route game threads to the cache CCD; 'Auto' makes the kernel ignore the optimizer."),
        new BiosRecommendation("EXPO / Memory profile", "Enabled", "Runs RAM at its rated speed/timings."),
        new BiosRecommendation("Global C-States", "Auto / Enabled", "Required for proper boost behavior; do not disable."),
        new BiosRecommendation("Resizable BAR (Smart Access Memory)", "Enabled", "Improves GPU memory access in many games."),
    };

    private static IReadOnlyList<BiosRecommendation> BiosAmdSingle() => new[]
    {
        new BiosRecommendation("EXPO / Memory profile", "Enabled", "Runs RAM at its rated speed/timings."),
        new BiosRecommendation("Global C-States", "Auto / Enabled", "Required for proper boost behavior."),
        new BiosRecommendation("Resizable BAR (Smart Access Memory)", "Enabled", "Improves GPU memory access in many games."),
        new BiosRecommendation("Precision Boost Overdrive", "Auto / Enabled", "Allows higher sustained boost within thermal limits."),
    };

    private static IReadOnlyList<BiosRecommendation> BiosIntel() => new[]
    {
        new BiosRecommendation("XMP / Memory profile", "Enabled", "Runs RAM at its rated speed/timings."),
        new BiosRecommendation("Resizable BAR", "Enabled", "Improves GPU memory access in many games."),
    };

    private static readonly IReadOnlyList<BiosRecommendation> BiosNone = Array.Empty<BiosRecommendation>();

    // ---- Catalog (priority order: exact -> family -> generic) ----

    public static IReadOnlyList<CpuTuneDefinition> All { get; } = BuildAll();

    private static IReadOnlyList<CpuTuneDefinition> BuildAll()
    {
        var list = new List<CpuTuneDefinition>
        {
            // Exact, single-CCD (includes the developer's verified 9850X3D).
            new("amd-single-x3d-exact", TuneTier.Exact,
                c => c.Vendor == CpuVendor.Amd && SingleCcdX3dModels.Contains(c.Model),
                CcdTopology.Single, ParkingStrategy.NoParking,
                SingleCcdRecipe(), PowerPlanChoice.Balanced, false, BiosAmdSingle()),

            // Exact, asymmetric dual-CCD (9950X3D headline).
            new("amd-asym-dual-x3d-exact", TuneTier.Exact,
                c => c.Vendor == CpuVendor.Amd && c.Model.Equals("9950X3D", StringComparison.OrdinalIgnoreCase),
                CcdTopology.Dual, ParkingStrategy.ParkFrequencyCcd,
                AsymmetricDualRecipe(), PowerPlanChoice.Balanced, false, BiosAmdDualCcd()),

            // Exact, dual-V-cache 9950X3D2 — cache on BOTH CCDs => no parking.
            new("amd-dual-vcache-x3d2-exact", TuneTier.Exact,
                c => c.Vendor == CpuVendor.Amd && c.Model.Equals("9950X3D2", StringComparison.OrdinalIgnoreCase),
                CcdTopology.Dual, ParkingStrategy.NoParking,
                NoParkRecipe(), PowerPlanChoice.Balanced, false, BiosAmdSingle()),

            // Family, asymmetric dual-CCD X3D (7950X3D / 7900X3D / 9900X3D).
            new("amd-asym-dual-x3d-family", TuneTier.Family,
                c => c.Vendor == CpuVendor.Amd && ParkFrequencyCcdModels.Contains(c.Model),
                CcdTopology.Dual, ParkingStrategy.ParkFrequencyCcd,
                AsymmetricDualRecipe(), PowerPlanChoice.Balanced, false, BiosAmdDualCcd()),

            // Family, single-CCD X3D (7800X3D and any other single-CCD X3D).
            new("amd-single-x3d-family", TuneTier.Family,
                c => c.Vendor == CpuVendor.Amd && IsSingleCcdX3d(c),
                CcdTopology.Single, ParkingStrategy.NoParking,
                SingleCcdRecipe(), PowerPlanChoice.Balanced, false, BiosAmdSingle()),

            // Family, modern non-X3D Ryzen (Zen4/Zen5) — no parking (covers both
            // single-CCD and symmetric dual-CCD; symmetric dual has no cache CCD
            // to prefer, so it must NOT park).
            new("amd-non-x3d-modern-family", TuneTier.Family,
                c => c.Vendor == CpuVendor.Amd && (c.Family is "Zen4" or "Zen5") && !IsX3d(c),
                CcdTopology.Unknown, ParkingStrategy.NoParking,
                NoParkRecipe(), PowerPlanChoice.Balanced, false, BiosAmdSingle()),

            // Family, modern Intel hybrid (P+E) — boost only, leave parking to
            // Thread Director.
            new("intel-hybrid-family", TuneTier.Family,
                c => c.Vendor == CpuVendor.Intel && c.Family == "IntelHybrid",
                CcdTopology.Unknown, ParkingStrategy.Default,
                BoostOnlyRecipe(), PowerPlanChoice.Balanced, false, BiosIntel()),

            // Generic fallback — always matches.
            new("generic", TuneTier.Generic,
                _ => true,
                CcdTopology.Unknown, ParkingStrategy.Default,
                BoostOnlyRecipe(), PowerPlanChoice.Balanced, true, BiosNone),
        };
        return list;
    }

    private static bool IsX3d(CpuInfo c) =>
        c.Model.Contains("X3D", StringComparison.OrdinalIgnoreCase);

    private static bool IsSingleCcdX3d(CpuInfo c) =>
        SingleCcdX3dModels.Contains(c.Model);

    /// <summary>Resolve the tune for a detected CPU: exact -> family -> generic.</summary>
    public static CpuTuneResult Resolve(CpuInfo cpu)
    {
        var def = All.First(d => d.Matches(cpu));
        return new CpuTuneResult(
            Cpu: cpu,
            Definition: def,
            Topology: def.Topology,
            Parking: def.Parking,
            IsGeneric: def.GenericLabeled,
            RecommendedPrebuilt: def.RecommendedPrebuilt,
            Overrides: def.Overrides,
            Bios: def.Bios,
            ContentHash: CpuTuneResult.ComputeHash(def.Overrides));
    }
}
