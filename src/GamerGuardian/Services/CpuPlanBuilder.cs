using GamerGuardian.Models;
using GamerGuardian.Monitors;
using GamerGuardian.Native;
using Microsoft.Win32;

namespace GamerGuardian.Services;

public enum BuildDecision { Create, ReuseExisting, ReTune }

/// <summary>An installed Windows power scheme (GUID + friendly name).</summary>
public sealed record InstalledPlan(Guid Guid, string Name);

/// <summary>The action the builder will take, and the existing scheme it targets
/// (null for Create).</summary>
public sealed record BuildPlan(BuildDecision Decision, Guid? ExistingGuid);

/// <summary>
/// Builds / re-tunes the GamerGuardian-authored optimized power plan idempotently
/// and resolves the best prebuilt plan. Never duplicates, and never deletes a
/// built-in or a user's custom plan.
///
/// <para>The decision core (<see cref="Decide"/>) and the delete guard
/// (<see cref="MaySafelyDelete"/>) are pure and unit-tested. The OS-mutating
/// <see cref="BuildOrActivate"/> is exercised on real hardware.</para>
/// </summary>
public static class CpuPlanBuilder
{
    public const string PlanNamePrefix = "GamerGuardian Gaming [";

    /// <summary>Pure: decide whether to create, reuse, or re-tune the GG plan.</summary>
    public static BuildPlan Decide(
        CpuPlanPref pref, CpuTuneResult recipe, string machineToken,
        IReadOnlyCollection<InstalledPlan> installed)
    {
        // 1. A valid stored plan on THIS machine that is still installed?
        if (TryGetValidStored(pref, machineToken, installed, out var storedGuid))
        {
            return string.Equals(pref.ContentHash, recipe.ContentHash, StringComparison.Ordinal)
                ? new BuildPlan(BuildDecision.ReuseExisting, storedGuid)
                : new BuildPlan(BuildDecision.ReTune, storedGuid);
        }

        // 2. Adopt an orphaned GG plan with the same name rather than stacking a
        //    duplicate (the root cause of the two identical plans observed live).
        var orphan = installed.FirstOrDefault(p =>
            string.Equals(p.Name, recipe.PlanName, StringComparison.OrdinalIgnoreCase));
        if (orphan is not null)
            return new BuildPlan(BuildDecision.ReTune, orphan.Guid);

        // 3. Nothing to reuse -> create.
        return new BuildPlan(BuildDecision.Create, null);
    }

    private static bool TryGetValidStored(
        CpuPlanPref pref, string machineToken,
        IReadOnlyCollection<InstalledPlan> installed, out Guid guid)
    {
        guid = Guid.Empty;
        if (string.IsNullOrEmpty(pref.BuiltSchemeGuid) || !Guid.TryParse(pref.BuiltSchemeGuid, out var g))
            return false;
        // Foreign machine identity -> not ours to reuse or delete.
        if (!string.IsNullOrEmpty(pref.MachineToken) &&
            !string.Equals(pref.MachineToken, machineToken, StringComparison.OrdinalIgnoreCase))
            return false;
        // Stale (no longer installed) -> treat as no plan.
        if (!installed.Any(p => p.Guid == g))
            return false;
        guid = g;
        return true;
    }

    /// <summary>Pure: may the app delete this scheme? Requires positive identity —
    /// installed, named in the GG format, and NOT a well-known Microsoft plan.</summary>
    public static bool MaySafelyDelete(Guid candidate, IReadOnlyCollection<InstalledPlan> installed)
    {
        if (candidate == Guid.Empty) return false;
        if (IsWellKnownMicrosoft(candidate)) return false;
        var match = installed.FirstOrDefault(p => p.Guid == candidate);
        if (match is null) return false;
        return match.Name.StartsWith(PlanNamePrefix, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsWellKnownMicrosoft(Guid g) =>
        g == PowerPlanMonitor.Balanced || g == PowerPlanMonitor.HighPerformance ||
        g == PowerPlanMonitor.PowerSaver || g == PowerPlanMonitor.UltimatePerformance;

    // ---- OS-mutating surface (not unit-tested) ----

    /// <summary>Read the machine identity (scheme GUIDs are machine-local).</summary>
    public static string MachineToken()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            return k?.GetValue("MachineGuid") as string ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    public static IReadOnlyList<InstalledPlan> InstalledPlans()
    {
        var list = new List<InstalledPlan>();
        foreach (var kvp in PowerPlanMonitor.ListAvailablePlans())
            list.Add(new InstalledPlan(kvp.Key, kvp.Value));
        return list;
    }

    /// <summary>Create or re-tune the GG plan and persist its identity into
    /// <paramref name="pref"/>. Returns the target scheme GUID. Does NOT set the
    /// scheme active — the apply step does that and verifies. Throws on failure
    /// (the caller surfaces the error); a partially-created scheme is cleaned up.</summary>
    public static Guid BuildOrActivate(
        CpuTuneResult recipe, CpuPlanPref pref, string machineToken,
        IReadOnlyCollection<InstalledPlan> installed)
    {
        var plan = Decide(pref, recipe, machineToken, installed);
        Guid target;

        switch (plan.Decision)
        {
            case BuildDecision.ReuseExisting:
                target = plan.ExistingGuid!.Value;
                Powrprof.WriteFriendlyName(target, recipe.PlanName);
                break;

            case BuildDecision.ReTune:
                target = plan.ExistingGuid!.Value;
                ApplyOverrides(target, recipe);
                Powrprof.WriteFriendlyName(target, recipe.PlanName);
                break;

            default: // Create
                var baseGuid = PowerPlanMonitor.ResolveBalancedBase();
                if (baseGuid == Guid.Empty)
                    throw new InvalidOperationException("Balanced base power plan was not found on this machine.");
                target = Powrprof.DuplicateScheme(baseGuid);
                if (target == Guid.Empty)
                    throw new InvalidOperationException("Could not create the optimized plan (duplicate failed — may require elevation).");
                try
                {
                    Powrprof.WriteFriendlyName(target, recipe.PlanName);
                    ApplyOverrides(target, recipe);
                }
                catch
                {
                    // Partial-failure cleanup: remove the just-created scratch and
                    // do not persist its GUID, so a retry is clean.
                    Powrprof.DeleteScheme(target);
                    throw;
                }
                break;
        }

        pref.BuiltSchemeGuid = target.ToString();
        pref.ContentHash = recipe.ContentHash;
        pref.MachineToken = machineToken;
        pref.CpuModel = recipe.Cpu.Model;
        return target;
    }

    private static void ApplyOverrides(Guid scheme, CpuTuneResult recipe)
    {
        foreach (var o in recipe.Overrides)
            if (!Powrprof.WriteValue(scheme, o.Subgroup, o.Setting, o.Value))
                throw new InvalidOperationException($"Failed to write power setting: {o.Label}.");
    }

    /// <summary>Resolve the recommended installed prebuilt plan GUID for the CPU,
    /// or <see cref="Guid.Empty"/> if it isn't installed.</summary>
    public static Guid BestPrebuilt(CpuTuneResult recipe, IReadOnlyCollection<InstalledPlan> installed)
    {
        var wanted = PowerPlanMonitor.ToGuid(recipe.RecommendedPrebuilt);
        return installed.Any(p => p.Guid == wanted) ? wanted : Guid.Empty;
    }
}
