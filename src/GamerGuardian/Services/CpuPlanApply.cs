using GamerGuardian.Models;
using GamerGuardian.Monitors;
using GamerGuardian.Native;

namespace GamerGuardian.Services;

/// <summary>
/// Wraps the imperative "build optimized plan" / "suggest best prebuilt plan"
/// actions as one-off <see cref="DriftItem"/>s run through
/// <see cref="ChangeApplier"/> so they inherit the change log + Apply Results
/// window. Because no monitor emits the <c>cpuplan</c> id, the applier's verify
/// pass cannot confirm correctness — so the <c>Apply</c> lambda verifies its own
/// post-conditions (active scheme == target AND every override reads back) and
/// throws on mismatch, which is what marks the result unverified.
/// </summary>
public static class CpuPlanApply
{
    /// <summary>Build (or re-tune) and activate the GG-authored optimized plan.</summary>
    public static DriftItem BuildOptimizedDriftItem(CpuTuneResult recipe, AppConfig config)
    {
        var (beforeName, beforeGuid) = ActivePlan();
        var overridesSummary = string.Join("; ", recipe.Overrides.Select(o => o.Label));

        return new DriftItem(
            SettingId: "cpuplan",
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: $"Build optimized plan for {Display(recipe.Cpu)}",
            CurrentValue: beforeName,
            DesiredValue: recipe.PlanName,
            AutoApply: false,
            Apply: () => System.Threading.Tasks.Task.Run(() => BuildAndVerify(recipe, config)),
            IsMonitored: false,
            RawBefore: beforeGuid.ToString(),
            RawDesired: $"{recipe.PlanName} :: {overridesSummary}");
    }

    /// <summary>Switch to the best-matching installed prebuilt plan.</summary>
    public static DriftItem SuggestPrebuiltDriftItem(CpuTuneResult recipe, AppConfig config)
    {
        var (beforeName, beforeGuid) = ActivePlan();
        return new DriftItem(
            SettingId: "cpuplan",
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: $"Suggest best prebuilt plan for {Display(recipe.Cpu)}",
            CurrentValue: beforeName,
            DesiredValue: recipe.RecommendedPrebuilt.ToString(),
            AutoApply: false,
            Apply: () => System.Threading.Tasks.Task.Run(() => SuggestAndVerify(recipe, config)),
            IsMonitored: false,
            RawBefore: beforeGuid.ToString(),
            RawDesired: recipe.RecommendedPrebuilt.ToString());
    }

    public static Task<List<ApplyResult>> RunAsync(
        DriftItem item, IReadOnlyList<IMonitoredSetting> monitors, AppConfig config) =>
        ChangeApplier.ApplyAndVerifyAsync(new[] { item }, monitors, config,
            source: "ui-cpuplan", sessionId: Guid.NewGuid().ToString("N")[..8]);

    private static void BuildAndVerify(CpuTuneResult recipe, AppConfig config)
    {
        var installed = CpuPlanBuilder.InstalledPlans();
        var machineToken = CpuPlanBuilder.MachineToken();

        var target = CpuPlanBuilder.BuildOrActivate(recipe, config.Global.CpuPlan, machineToken, installed);

        if (!Powrprof.SetActiveScheme(target))
            throw new InvalidOperationException("Built the optimized plan but could not set it active.");

        // Self-verify: active scheme matches AND every override actually applied
        // on BOTH rails (we write AC and DC).
        if (Powrprof.GetActiveScheme() != target)
            throw new InvalidOperationException("Active scheme does not match the built plan after apply.");
        foreach (var o in recipe.Overrides)
        {
            var ac = Powrprof.ReadAcValue(target, o.Subgroup, o.Setting);
            var dc = Powrprof.ReadDcValue(target, o.Subgroup, o.Setting);
            if (ac != o.Value || dc != o.Value)
                throw new InvalidOperationException(
                    $"Verification failed: '{o.Label}' read back as AC={(ac?.ToString() ?? "unset")}, DC={(dc?.ToString() ?? "unset")}.");
        }

        // Only on success: persist the GG plan identity (idempotency) and pin the
        // existing PowerPlanMonitor to this plan (R17).
        CpuPlanBuilder.PersistIdentity(config.Global.CpuPlan, recipe, target, machineToken);
        config.Global.PowerPlan.DesiredGuid = target.ToString();
        config.Global.PowerPlan.DesiredName = recipe.PlanName;
    }

    private static void SuggestAndVerify(CpuTuneResult recipe, AppConfig config)
    {
        var installed = CpuPlanBuilder.InstalledPlans();
        var target = CpuPlanBuilder.BestPrebuilt(recipe, installed);
        if (target == Guid.Empty)
            throw new InvalidOperationException("The recommended prebuilt plan is not installed on this machine.");

        if (!Powrprof.SetActiveScheme(target) || Powrprof.GetActiveScheme() != target)
            throw new InvalidOperationException("Could not switch to the recommended prebuilt plan.");

        config.Global.PowerPlan.DesiredGuid = target.ToString();
        config.Global.PowerPlan.DesiredName = Powrprof.ReadFriendlyName(target) ?? recipe.RecommendedPrebuilt.ToString();
    }

    private static (string name, Guid guid) ActivePlan()
    {
        var guid = Powrprof.GetActiveScheme();
        if (guid == Guid.Empty)
            return ("(unknown -- could not read the active plan)", Guid.Empty);
        var name = Powrprof.ReadFriendlyName(guid) ?? guid.ToString();
        return (name, guid);
    }

    private static string Display(CpuInfo cpu) =>
        cpu.IsDetected ? cpu.Model : "this CPU";
}
