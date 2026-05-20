using GamerGuardian.Models;

namespace GamerGuardian.Services;

/// <summary>
/// One-click "use GamerGuardian's recommended settings" preset. Applies the
/// gaming-optimized state to every setting it knows about: sets the
/// recommended Want value, turns Monitor on, turns Auto-apply silently on.
///
/// <para><b>Idempotent.</b> Only mutates fields that differ from the
/// recommended state. Re-running the preset after a future update where new
/// settings have been added picks up only the new ones; everything already
/// in the recommended state is reported as "already correct" and skipped.</para>
///
/// <para><b>Conservative.</b> Two categories are intentionally NOT in the
/// preset:
/// <list type="bullet">
///   <item><b>Memory Integrity / VBS</b> -- security toggle. Some anti-cheat
///     requires it on; flipping it off via a one-button preset would surprise
///     users. They can flip it individually if they want.</item>
///   <item><b>UWP AI app removal</b> -- irreversible without the Microsoft
///     Store. Policy toggles are sufficient to disable Copilot; opt-in only
///     for actual uninstall.</item>
/// </list></para>
///
/// <para>The preset mutates the draft directly (no per-field PropertyChanged
/// events fire). Callers are responsible for rebuilding the UI rows from the
/// mutated draft and bumping <c>_pendingCount</c> by <see cref="Result.SettingsChanged"/>
/// so the staged-apply UI shows the right count and Save&amp;close doesn't
/// short-circuit.</para>
/// </summary>
public static class RecommendedPreset
{
    public sealed record Result(
        int SettingsChanged,
        int SettingsAlreadyCorrect,
        IReadOnlyList<string> ChangeDescriptions);

    /// <summary>
    /// Microsoft's well-known plan GUIDs. If the chosen plan isn't installed
    /// on the target machine, the preset leaves the power plan alone.
    /// </summary>
    private const string HighPerformanceGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    private const string BalancedGuid        = "381b4222-f694-41f0-9685-ff5bb260df2e";

    public static Result ApplyToDraft(AppConfig draft)
    {
        if (draft is null) throw new ArgumentNullException(nameof(draft));

        var changes = new List<string>();
        int changed = 0, alreadyCorrect = 0;
        void Count(bool didChange) { if (didChange) changed++; else alreadyCorrect++; }

        // ---- Global gaming toggles (gaming-recommended values) ----
        var g = draft.Global;
        Count(SetToggle(g.GameMode,               "Game Mode",                     desiredOn: true,  changes));
        Count(SetToggle(g.GameDvr,                "Game DVR background recording", desiredOn: false, changes));
        Count(SetToggle(g.Hags,                   "HAGS",                          desiredOn: true,  changes));
        Count(SetToggle(g.Vrr,                    "Variable Refresh Rate",         desiredOn: true,  changes));
        Count(SetToggle(g.SystemResponsiveness,   "System Responsiveness",         desiredOn: true,  changes));
        Count(SetToggle(g.NetworkThrottling,      "Network Throttling",            desiredOn: true,  changes));
        Count(SetToggle(g.UsbSelectiveSuspend,    "USB Selective Suspend",         desiredOn: true,  changes));
        Count(SetToggle(g.GamesTaskProfile,       "Games Task Profile",            desiredOn: true,  changes));
        Count(SetToggle(g.MousePrecision,         "Mouse Precision",               desiredOn: false, changes));
        Count(SetToggle(g.FullscreenOptimizations,"Fullscreen Optimizations",      desiredOn: true,  changes));
        // MemoryIntegrity intentionally omitted (security tradeoff -- see class doc)

        // ---- Windows AI toggles (all off for gaming -- minimize background work) ----
        Count(SetToggle(g.Copilot,         "Windows Copilot",                desiredOn: false, changes));
        Count(SetToggle(g.Recall,          "Windows Recall + AI analysis",   desiredOn: false, changes));
        Count(SetToggle(g.ClickToDo,       "Click-to-Do",                    desiredOn: false, changes));
        Count(SetToggle(g.EdgeAi,          "Edge Copilot / Hubs / GenAI",    desiredOn: false, changes));
        Count(SetToggle(g.NotepadPaintAi,  "Notepad Rewrite + Paint AI",     desiredOn: false, changes));
        Count(SetToggle(g.SettingsSearchAi,"Search box AI + taskbar companion", desiredOn: false, changes));
        Count(SetToggle(g.AiActions,       "Windows AI Actions",             desiredOn: false, changes));
        Count(SetToggle(g.InputInsights,   "Typing / input insights",        desiredOn: false, changes));
        Count(SetToggle(g.OfficeCopilot,   "Office 365 Copilot",             desiredOn: false, changes));

        // ---- Power plan: High Performance if installed; otherwise leave alone ----
        Count(SetPowerPlan(g.PowerPlan, changes));

        // ---- Services with a RecommendedTarget ----
        foreach (var def in ServiceCatalog.All)
        {
            if (def.RecommendedTarget is not { } target) continue;
            if (!draft.Services.TryGetValue(def.Name, out var pref) || pref is null)
            {
                pref = new ServicePref();
                draft.Services[def.Name] = pref;
            }
            Count(SetService(pref, $"Service: {def.DisplayName}", target, changes));
        }

        // ---- Display HDR + Refresh (per-display; pin AutoApply on) ----
        foreach (var (_, displayPref) in draft.Displays)
        {
            var label = string.IsNullOrEmpty(displayPref.DisplayLabel) ? "(display)" : displayPref.DisplayLabel;
            Count(SetHdr(displayPref.Hdr, $"HDR on {label}", changes));
            Count(SetRefresh(displayPref.RefreshRate, $"Refresh rate on {label}", changes));
            // Resolution NOT in preset -- too display-specific to push a default.
        }

        return new Result(changed, alreadyCorrect, changes);
    }

    private static bool SetToggle(ToggleSettingPref pref, string label, bool desiredOn, List<string> changes)
    {
        var (b1, b2, b3) = (pref.DesiredOn, pref.Monitor, pref.AutoApply);
        var a1 = desiredOn; var a2 = true; var a3 = true;
        if (b1 == a1 && b2 == a2 && b3 == a3) return false;
        pref.DesiredOn = a1; pref.Monitor = a2; pref.AutoApply = a3;
        ChangeLogger.LogPreferenceChange($"[Recommended] {label}", "preset",
            $"Want={B(b1)} Monitor={B(b2)} AutoApply={B(b3)}",
            $"Want={B(a1)} Monitor=On AutoApply=On");
        changes.Add($"{label}: Want={(desiredOn ? "On" : "Off")}, Monitor on, Auto-apply on");
        return true;
    }

    private static bool SetService(ServicePref pref, string label, ServiceTargetState target, List<string> changes)
    {
        var (b1, b2, b3) = (pref.Desired, pref.Monitor, pref.AutoApply);
        var a1 = target; var a2 = true; var a3 = true;
        if (b1 == a1 && b2 == a2 && b3 == a3) return false;
        pref.Desired = a1; pref.Monitor = a2; pref.AutoApply = a3;
        ChangeLogger.LogPreferenceChange($"[Recommended] {label}", "preset",
            $"Want={b1} Monitor={B(b2)} AutoApply={B(b3)}",
            $"Want={a1} Monitor=On AutoApply=On");
        changes.Add($"{label}: Want={target}, Monitor on, Auto-apply on");
        return true;
    }

    private static bool SetHdr(HdrPref pref, string label, List<string> changes)
    {
        var (b1, b2, b3) = (pref.DesiredOn, pref.Monitor, pref.AutoApply);
        var a1 = true; var a2 = true; var a3 = true;
        if (b1 == a1 && b2 == a2 && b3 == a3) return false;
        pref.DesiredOn = a1; pref.Monitor = a2; pref.AutoApply = a3;
        ChangeLogger.LogPreferenceChange($"[Recommended] {label}", "preset",
            $"Want={B(b1)} Monitor={B(b2)} AutoApply={B(b3)}",
            $"Want=On Monitor=On AutoApply=On");
        changes.Add($"{label}: HDR On, Monitor on, Auto-apply on");
        return true;
    }

    private static bool SetRefresh(RefreshRatePref pref, string label, List<string> changes)
    {
        var (b1, b2, b3) = (pref.Target, pref.Monitor, pref.AutoApply);
        var a1 = RefreshRateTarget.Maximum; var a2 = true; var a3 = true;
        if (b1 == a1 && b2 == a2 && b3 == a3) return false;
        pref.Target = a1; pref.Monitor = a2; pref.AutoApply = a3;
        ChangeLogger.LogPreferenceChange($"[Recommended] {label}", "preset",
            $"Target={b1} Monitor={B(b2)} AutoApply={B(b3)}",
            $"Target=Maximum Monitor=On AutoApply=On");
        changes.Add($"{label}: Target=Maximum, Monitor on, Auto-apply on");
        return true;
    }

    private static bool SetPowerPlan(PowerPlanPref pref, List<string> changes)
    {
        // CPU-aware recommendation. AMD X3D chips have a V-Cache CCD with a
        // lower max clock; AMD officially recommends Balanced (with their
        // chipset driver's CPPC scheduling) rather than High Performance,
        // which pegs both CCDs to the non-V-Cache CCD's higher clock and can
        // hurt game performance. Everything else gets the historical
        // recommendation (High Performance).
        var cpuName = CpuInfo.GetName();
        var isX3D = CpuInfo.IsAmdX3D(cpuName);
        var recommendedGuid = isX3D ? BalancedGuid : HighPerformanceGuid;
        var recommendedName = isX3D ? "Balanced" : "High performance";
        var recommendedChoice = isX3D ? PowerPlanChoice.Balanced : PowerPlanChoice.HighPerformance;

        // Only switch if the recommended plan is installed locally. If the
        // user has a custom power plan they prefer, leave it alone -- the
        // preset shouldn't second-guess a custom plan choice.
        var plans = SafeListPlans();
        if (!plans.Any(p => string.Equals(p.Key.ToString("D"), recommendedGuid, StringComparison.OrdinalIgnoreCase)))
            return false;

        var (bGuid, bMon, bAuto) = (pref.DesiredGuid, pref.Monitor, pref.AutoApply);
        bool already = string.Equals(bGuid, recommendedGuid, StringComparison.OrdinalIgnoreCase)
                       && bMon && bAuto;
        if (already) return false;

        pref.DesiredGuid = recommendedGuid;
        pref.DesiredName = recommendedName;
        pref.Desired = recommendedChoice;
        pref.Monitor = true;
        pref.AutoApply = true;
        var reason = isX3D ? $"AMD X3D CPU detected ({cpuName})" : (cpuName ?? "default");
        ChangeLogger.LogPreferenceChange($"[Recommended] Power plan ({reason})", "preset",
            $"Want={bGuid ?? "(unset)"} Monitor={B(bMon)} AutoApply={B(bAuto)}",
            $"Want={recommendedName} Monitor=On AutoApply=On");
        changes.Add($"Power plan: {recommendedName} ({reason}), Monitor on, Auto-apply on");
        return true;
    }

    private static IDictionary<Guid, string> SafeListPlans()
    {
        try { return Monitors.PowerPlanMonitor.ListAvailablePlans(); }
        catch { return new Dictionary<Guid, string>(); }
    }

    private static string B(bool x) => x ? "On" : "Off";
}
