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

    // ---- Extreme preset ---------------------------------------------------
    //
    // "Everything that could even remotely improve gaming, on." Covers EVERY
    // toggle (not just the conservative subset) at its most-aggressive gaming
    // value -- including Memory Integrity / VBS OFF and the contested Nagle / NIC
    // tweaks -- plus services, displays and the CPU-aware power plan. Every touched
    // setting gets Monitor + Auto-apply turned on, per the user's request.
    // Still excludes irreversible UWP AI app removal (Store-only to undo).

    public static Result ApplyExtremeToDraft(AppConfig draft) =>
        ApplyExtremeToDraft(draft, CpuTuneCatalog.Resolve(CpuDetector.Current));

    public static Result ApplyExtremeToDraft(AppConfig draft, CpuTuneResult recipe) =>
        ApplyExtremeToDraft(draft, recipe, SafeListPlans());

    public static Result ApplyExtremeToDraft(AppConfig draft, CpuTuneResult recipe, IDictionary<Guid, string> installedPlans)
    {
        if (draft is null) throw new ArgumentNullException(nameof(draft));

        var changes = new List<string>();
        int changed = 0, alreadyCorrect = 0;
        void Count(bool didChange) { if (didChange) changed++; else alreadyCorrect++; }

        var g = draft.Global;
        foreach (var (id, pref, label) in AllToggles(g))
        {
            var desiredOn = SettingRecommendations.ExtremeDesiredOn[id];
            Count(SetToggleTo(pref, label, desiredOn, monitor: true, autoApply: true, "Extreme", changes));
        }

        // Power plan: still the CPU-aware prebuilt (never blindly High Performance).
        // Building the custom optimized scheme stays an explicit CPU/Power action.
        Count(SetPowerPlan(g.PowerPlan, recipe, installedPlans, changes));

        foreach (var def in ServiceCatalog.All)
        {
            if (def.RecommendedTarget is not { } target) continue;
            if (target == ServiceTargetState.Disabled && ShouldProtectServiceOnDualCcd(def.Name, recipe))
                continue;
            if (!draft.Services.TryGetValue(def.Name, out var pref) || pref is null)
            {
                pref = new ServicePref();
                draft.Services[def.Name] = pref;
            }
            Count(SetService(pref, $"Service: {def.DisplayName}", target, changes, tag: "Extreme"));
        }

        foreach (var (_, displayPref) in draft.Displays)
        {
            var label = string.IsNullOrEmpty(displayPref.DisplayLabel) ? "(display)" : displayPref.DisplayLabel;
            Count(SetHdr(displayPref.Hdr, $"HDR on {label}", changes, tag: "Extreme"));
            Count(SetRefresh(displayPref.RefreshRate, $"Refresh rate on {label}", changes, tag: "Extreme"));
        }

        return new Result(changed, alreadyCorrect, changes);
    }

    // ---- Reset-to-defaults preset -----------------------------------------
    //
    // The inverse of the gaming presets: stage every managed setting back to its
    // Windows out-of-box value and turn Monitor + Auto-apply OFF, so a subsequent
    // Apply restores Windows defaults and GamerGuardian stops re-asserting anything.
    // Displays are only un-monitored (their Want is hardware-specific, so it's left
    // alone). UWP AI app removals are not touched (reinstalling is Store-only).

    public static Result ResetToDefaultsToDraft(AppConfig draft) =>
        ResetToDefaultsToDraft(draft, SafeListPlans());

    public static Result ResetToDefaultsToDraft(AppConfig draft, IDictionary<Guid, string> installedPlans)
    {
        if (draft is null) throw new ArgumentNullException(nameof(draft));

        var changes = new List<string>();
        int changed = 0, alreadyCorrect = 0;
        void Count(bool didChange) { if (didChange) changed++; else alreadyCorrect++; }

        var g = draft.Global;
        foreach (var (id, pref, label) in AllToggles(g))
        {
            var desiredOn = SettingRecommendations.WindowsDefaultDesiredOn[id];
            Count(SetToggleTo(pref, label, desiredOn, monitor: false, autoApply: false, "Reset", changes));
        }

        Count(ResetPowerPlan(g.PowerPlan, installedPlans, changes));

        // Reset only services the user is actually managing -- don't materialize a
        // Default pref for every catalog entry the user never touched.
        foreach (var (name, pref) in draft.Services)
            Count(SetService(pref, $"Service: {name}", ServiceTargetState.Default, changes,
                monitor: false, autoApply: false, tag: "Reset"));

        foreach (var (_, displayPref) in draft.Displays)
        {
            var label = string.IsNullOrEmpty(displayPref.DisplayLabel) ? "(display)" : displayPref.DisplayLabel;
            Count(ResetDisplay(displayPref, label, changes));
        }

        return new Result(changed, alreadyCorrect, changes);
    }

    private static bool ResetPowerPlan(PowerPlanPref pref, IDictionary<Guid, string> plans, List<string> changes)
    {
        // Windows default plan is Balanced. Stage it (unmanaged) if it's installed;
        // otherwise just stop monitoring whatever plan is selected.
        var balanced = PowerPlanMonitor.Balanced;
        plans.TryGetValue(balanced, out var name);
        var guidStr = balanced.ToString();

        bool already = !pref.Monitor && !pref.AutoApply
                       && pref.Desired == PowerPlanChoice.Balanced
                       && (name is null || string.Equals(pref.DesiredGuid, guidStr, StringComparison.OrdinalIgnoreCase));
        if (already) return false;

        var before = pref.DesiredName ?? pref.Desired.ToString();
        pref.Desired = PowerPlanChoice.Balanced;
        if (name is not null) { pref.DesiredGuid = guidStr; pref.DesiredName = name; }
        pref.Monitor = false;
        pref.AutoApply = false;
        ChangeLogger.LogPreferenceChange("[Reset] Power plan", "preset",
            $"Want={before}",
            $"Want={name ?? "Balanced"} Monitor=Off AutoApply=Off");
        changes.Add($"Power plan: Balanced (Windows default), Monitor off, Auto-apply off");
        return true;
    }

    private static bool ResetDisplay(DisplayPreference dp, string label, List<string> changes)
    {
        bool changed = false;
        if (dp.Hdr.Monitor || dp.Hdr.AutoApply) { dp.Hdr.Monitor = false; dp.Hdr.AutoApply = false; changed = true; }
        if (dp.RefreshRate.Monitor || dp.RefreshRate.AutoApply) { dp.RefreshRate.Monitor = false; dp.RefreshRate.AutoApply = false; changed = true; }
        if (dp.Drr.Monitor || dp.Drr.AutoApply) { dp.Drr.Monitor = false; dp.Drr.AutoApply = false; changed = true; }
        if (dp.Resolution.Monitor || dp.Resolution.AutoApply) { dp.Resolution.Monitor = false; dp.Resolution.AutoApply = false; changed = true; }
        if (!changed) return false;
        ChangeLogger.LogPreferenceChange($"[Reset] Display {label}", "preset",
            "Monitor=On/various", "Monitor=Off AutoApply=Off (all per-display settings)");
        changes.Add($"{label}: stopped monitoring HDR / refresh / DRR / resolution");
        return true;
    }

    private static bool SetToggle(ToggleSettingPref pref, string label, string settingId, List<string> changes)
    {
        // Recommendation source of truth -- shared with the per-row UI hint.
        var desiredOn = SettingRecommendations.ToggleDesiredOn[settingId];
        return SetToggleTo(pref, label, desiredOn, monitor: true, autoApply: true, "Recommended", changes);
    }

    /// <summary>
    /// Stage a toggle to an explicit (DesiredOn, Monitor, AutoApply) triple, logging
    /// and recording a change only when something actually differs. Shared by the
    /// Recommended, Extreme, and Reset presets so all three behave identically on
    /// idempotency, change-counting, and logging.
    /// </summary>
    private static bool SetToggleTo(ToggleSettingPref pref, string label, bool desiredOn,
        bool monitor, bool autoApply, string tag, List<string> changes)
    {
        var (b1, b2, b3) = (pref.DesiredOn, pref.Monitor, pref.AutoApply);
        if (b1 == desiredOn && b2 == monitor && b3 == autoApply) return false;
        pref.DesiredOn = desiredOn; pref.Monitor = monitor; pref.AutoApply = autoApply;
        ChangeLogger.LogPreferenceChange($"[{tag}] {label}", "preset",
            $"Want={B(b1)} Monitor={B(b2)} AutoApply={B(b3)}",
            $"Want={B(desiredOn)} Monitor={B(monitor)} AutoApply={B(autoApply)}");
        changes.Add($"{label}: Want={(desiredOn ? "On" : "Off")}, Monitor {OnOff(monitor)}, Auto-apply {OnOff(autoApply)}");
        return true;
    }

    private static string OnOff(bool x) => x ? "on" : "off";

    /// <summary>
    /// Every global toggle GamerGuardian manages, paired with its settingId and a
    /// friendly label, in a stable order. The Extreme and Reset presets iterate this
    /// (the Recommended preset keeps its own narrower, hand-picked subset).
    /// </summary>
    private static IEnumerable<(string id, ToggleSettingPref pref, string label)> AllToggles(GlobalPreferences g) =>
        new (string, ToggleSettingPref, string)[]
        {
            ("gamemode",                 g.GameMode,                "Game Mode"),
            ("gamedvr",                  g.GameDvr,                 "Game DVR background recording"),
            ("hags",                     g.Hags,                    "HAGS"),
            ("vrr",                      g.Vrr,                     "Variable Refresh Rate"),
            ("sysresponse",              g.SystemResponsiveness,    "System Responsiveness"),
            ("netthrottle",              g.NetworkThrottling,       "Network Throttling"),
            ("usbsuspend",               g.UsbSelectiveSuspend,     "USB Selective Suspend"),
            ("gamestask",                g.GamesTaskProfile,        "Games Task Profile"),
            ("mouseaccel",               g.MousePrecision,          "Mouse Precision"),
            ("fso",                      g.FullscreenOptimizations, "Fullscreen Optimizations"),
            ("memintegrity",             g.MemoryIntegrity,         "Memory Integrity"),
            ("vbs",                      g.Vbs,                     "Virtualization-Based Security"),
            ("powerthrottling",          g.PowerThrottling,         "Power Throttling"),
            ("faststartup",              g.FastStartup,             "Fast Startup"),
            ("visualfx",                 g.VisualFx,                "Visual Effects"),
            ("ai.copilot",               g.Copilot,                 "Windows Copilot"),
            ("ai.recall",                g.Recall,                  "Windows Recall + AI analysis"),
            ("ai.clicktodo",             g.ClickToDo,               "Click-to-Do"),
            ("ai.edge",                  g.EdgeAi,                  "Edge Copilot / Hubs / GenAI"),
            ("ai.notepadpaint",          g.NotepadPaintAi,          "Notepad Rewrite + Paint AI"),
            ("ai.settingssearch",        g.SettingsSearchAi,        "Search box AI + taskbar companion"),
            ("ai.actions",               g.AiActions,               "Windows AI Actions"),
            ("ai.inputinsights",         g.InputInsights,           "Typing / input insights"),
            ("ai.office",                g.OfficeCopilot,           "Office 365 Copilot"),
            ("privacy.advertisingid",    g.AdvertisingId,           "Advertising ID"),
            ("privacy.tailoredexp",      g.TailoredExperiences,     "Tailored experiences"),
            ("privacy.cdp",              g.Cdp,                     "Cross-Device Platform"),
            ("privacy.activityhistory",  g.ActivityHistory,         "Activity History"),
            ("privacy.speech",           g.OnlineSpeech,            "Online speech recognition"),
            ("privacy.inking",           g.InkingTyping,            "Inking & typing personalization"),
            ("debloat.suggestedcontent", g.SuggestedContent,        "Suggested content"),
            ("debloat.spotlight",        g.LockScreenSpotlight,     "Lock screen tips & ads"),
            ("debloat.finishsetup",      g.FinishSetupNag,          "Finish setup nag"),
            ("debloat.startrecommend",   g.StartRecommendations,    "Start recommendations"),
            ("debloat.explorerads",      g.ExplorerAds,             "File Explorer ads"),
            ("debloat.feedback",         g.FeedbackNag,             "Feedback popups"),
            ("debloat.widgets",          g.Widgets,                 "Widgets"),
            ("debloat.edge",             g.EdgeBackground,          "Edge startup boost & background"),
            ("network.nagle",            g.Nagle,                   "Nagle's algorithm"),
            ("network.nicpower",         g.NicPower,                "NIC power management"),
        };

    private static bool SetService(ServicePref pref, string label, ServiceTargetState target,
        List<string> changes, bool monitor = true, bool autoApply = true, string tag = "Recommended")
    {
        var (b1, b2, b3) = (pref.Desired, pref.Monitor, pref.AutoApply);
        if (b1 == target && b2 == monitor && b3 == autoApply) return false;
        pref.Desired = target; pref.Monitor = monitor; pref.AutoApply = autoApply;
        ChangeLogger.LogPreferenceChange($"[{tag}] {label}", "preset",
            $"Want={b1} Monitor={B(b2)} AutoApply={B(b3)}",
            $"Want={target} Monitor={B(monitor)} AutoApply={B(autoApply)}");
        changes.Add($"{label}: Want={target}, Monitor {OnOff(monitor)}, Auto-apply {OnOff(autoApply)}");
        return true;
    }

    private static bool SetHdr(HdrPref pref, string label, List<string> changes,
        bool monitor = true, bool autoApply = true, string tag = "Recommended")
    {
        var (b1, b2, b3) = (pref.DesiredOn, pref.Monitor, pref.AutoApply);
        if (b1 == true && b2 == monitor && b3 == autoApply) return false;
        pref.DesiredOn = true; pref.Monitor = monitor; pref.AutoApply = autoApply;
        ChangeLogger.LogPreferenceChange($"[{tag}] {label}", "preset",
            $"Want={B(b1)} Monitor={B(b2)} AutoApply={B(b3)}",
            $"Want=On Monitor={B(monitor)} AutoApply={B(autoApply)}");
        changes.Add($"{label}: HDR On, Monitor {OnOff(monitor)}, Auto-apply {OnOff(autoApply)}");
        return true;
    }

    private static bool SetRefresh(RefreshRatePref pref, string label, List<string> changes,
        bool monitor = true, bool autoApply = true, string tag = "Recommended")
    {
        var (b1, b2, b3) = (pref.Target, pref.Monitor, pref.AutoApply);
        if (b1 == RefreshRateTarget.Maximum && b2 == monitor && b3 == autoApply) return false;
        pref.Target = RefreshRateTarget.Maximum; pref.Monitor = monitor; pref.AutoApply = autoApply;
        ChangeLogger.LogPreferenceChange($"[{tag}] {label}", "preset",
            $"Target={b1} Monitor={B(b2)} AutoApply={B(b3)}",
            $"Target=Maximum Monitor={B(monitor)} AutoApply={B(autoApply)}");
        changes.Add($"{label}: Target=Maximum, Monitor {OnOff(monitor)}, Auto-apply {OnOff(autoApply)}");
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
