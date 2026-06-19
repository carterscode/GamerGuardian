using GamerGuardian.Models;
using GamerGuardian.Monitors;

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
///   <item><b>Memory Integrity and the full VBS-stack toggle</b> -- security
///     toggles. Some anti-cheat requires them on (Riot Vanguard mandates
///     Memory Integrity); flipping them off via a one-button preset would
///     surprise users. They can flip them individually if they want.</item>
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

    // Service-name fragments for the AMD CCD-routing stack + Xbox Game Bar that
    // must never be disabled on an asymmetric dual-CCD X3D CPU (doing so breaks
    // the cache-CCD routing the optimized plan depends on).
    private static readonly string[] DualCcdProtectedFragments =
        { "vcache", "3dcache", "provisioning", "gamebar", "gamingservices" };

    public static Result ApplyToDraft(AppConfig draft) =>
        ApplyToDraft(draft, CpuTuneCatalog.Resolve(CpuDetector.Current));

    public static Result ApplyToDraft(AppConfig draft, CpuTuneResult recipe) =>
        ApplyToDraft(draft, recipe, SafeListPlans());

    // installedPlans is injectable so the power-plan step is testable without a
    // live OS power-scheme enumeration.
    public static Result ApplyToDraft(AppConfig draft, CpuTuneResult recipe, IDictionary<Guid, string> installedPlans)
    {
        if (draft is null) throw new ArgumentNullException(nameof(draft));

        var changes = new List<string>();
        int changed = 0, alreadyCorrect = 0;
        void Count(bool didChange) { if (didChange) changed++; else alreadyCorrect++; }

        // ---- Global gaming toggles (gaming-recommended values) ----
        // desiredOn for each comes from SettingRecommendations (the same map the
        // Settings UI shows as "Recommended"), so the one-click preset and the
        // per-row hint can never disagree.
        var g = draft.Global;
        Count(SetToggle(g.GameMode,               "Game Mode",                     "gamemode",    changes));
        Count(SetToggle(g.GameDvr,                "Game DVR background recording", "gamedvr",     changes));
        Count(SetToggle(g.Hags,                   "HAGS",                          "hags",        changes));
        Count(SetToggle(g.Vrr,                    "Variable Refresh Rate",         "vrr",         changes));
        Count(SetToggle(g.SystemResponsiveness,   "System Responsiveness",         "sysresponse", changes));
        Count(SetToggle(g.NetworkThrottling,      "Network Throttling",            "netthrottle", changes));
        Count(SetToggle(g.UsbSelectiveSuspend,    "USB Selective Suspend",         "usbsuspend",  changes));
        Count(SetToggle(g.GamesTaskProfile,       "Games Task Profile",            "gamestask",   changes));
        Count(SetToggle(g.MousePrecision,         "Mouse Precision",               "mouseaccel",  changes));
        Count(SetToggle(g.FullscreenOptimizations,"Fullscreen Optimizations",      "fso",         changes));
        // MemoryIntegrity + Vbs intentionally omitted (security tradeoff -- see class doc)

        // ---- Windows AI toggles (all off for gaming -- minimize background work) ----
        Count(SetToggle(g.Copilot,         "Windows Copilot",                "ai.copilot",        changes));
        Count(SetToggle(g.Recall,          "Windows Recall + AI analysis",   "ai.recall",         changes));
        Count(SetToggle(g.ClickToDo,       "Click-to-Do",                    "ai.clicktodo",      changes));
        Count(SetToggle(g.EdgeAi,          "Edge Copilot / Hubs / GenAI",    "ai.edge",           changes));
        Count(SetToggle(g.NotepadPaintAi,  "Notepad Rewrite + Paint AI",     "ai.notepadpaint",   changes));
        Count(SetToggle(g.SettingsSearchAi,"Search box AI + taskbar companion", "ai.settingssearch", changes));
        Count(SetToggle(g.AiActions,       "Windows AI Actions",             "ai.actions",        changes));
        Count(SetToggle(g.InputInsights,   "Typing / input insights",        "ai.inputinsights",  changes));
        Count(SetToggle(g.OfficeCopilot,   "Office 365 Copilot",             "ai.office",         changes));

        // ---- Power plan: CPU-aware recommended prebuilt (Balanced for modern) ----
        Count(SetPowerPlan(g.PowerPlan, recipe, installedPlans, changes));

        // ---- Services with a RecommendedTarget ----
        foreach (var def in ServiceCatalog.All)
        {
            if (def.RecommendedTarget is not { } target) continue;
            // Guardrail: never disable the AMD CCD-routing stack / Game Bar on a
            // dual-CCD X3D CPU -- it would break the optimization this app sets up.
            if (target == ServiceTargetState.Disabled && ShouldProtectServiceOnDualCcd(def.Name, recipe))
                continue;
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

    private static bool SetToggle(ToggleSettingPref pref, string label, string settingId, List<string> changes)
    {
        // Recommendation source of truth -- shared with the per-row UI hint.
        var desiredOn = SettingRecommendations.ToggleDesiredOn[settingId];
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

    private static bool SetPowerPlan(PowerPlanPref pref, CpuTuneResult recipe,
        IDictionary<Guid, string> plans, List<string> changes)
    {
        // CPU-aware: recommend the prebuilt plan the catalog picked (Balanced for
        // modern CPUs) -- never blindly High Performance. Building the custom
        // optimized plan stays an explicit action on the CPU / Power tab. If the
        // recommended plan isn't installed, leave the power plan alone.
        var choice = recipe.RecommendedPrebuilt;
        var targetGuid = PowerPlanMonitor.ToGuid(choice);
        if (!plans.TryGetValue(targetGuid, out var name))
            return false;

        var guidStr = targetGuid.ToString();
        var (bGuid, bMon, bAuto) = (pref.DesiredGuid, pref.Monitor, pref.AutoApply);
        bool already = string.Equals(bGuid, guidStr, StringComparison.OrdinalIgnoreCase)
                       && bMon && bAuto && pref.Desired == choice;
        if (already) return false;

        pref.DesiredGuid = guidStr;
        pref.DesiredName = name;
        pref.Desired = choice;
        pref.Monitor = true;
        pref.AutoApply = true;
        ChangeLogger.LogPreferenceChange("[Recommended] Power plan", "preset",
            $"Want={bGuid ?? "(unset)"} Monitor={B(bMon)} AutoApply={B(bAuto)}",
            $"Want={name} Monitor=On AutoApply=On");
        changes.Add($"Power plan: {name} (CPU-aware recommendation), Monitor on, Auto-apply on");
        return true;
    }

    /// <summary>True when the service backs the AMD CCD-routing stack / Game Bar
    /// and the detected CPU is asymmetric dual-CCD X3D (so it must not be disabled).</summary>
    public static bool ShouldProtectServiceOnDualCcd(string serviceName, CpuTuneResult recipe)
    {
        if (!recipe.NeedsCcdRoutingStack || string.IsNullOrEmpty(serviceName)) return false;
        return DualCcdProtectedFragments.Any(f =>
            serviceName.Contains(f, StringComparison.OrdinalIgnoreCase));
    }

    private static IDictionary<Guid, string> SafeListPlans()
    {
        try { return Monitors.PowerPlanMonitor.ListAvailablePlans(); }
        catch { return new Dictionary<Guid, string>(); }
    }

    private static string B(bool x) => x ? "On" : "Off";
}
