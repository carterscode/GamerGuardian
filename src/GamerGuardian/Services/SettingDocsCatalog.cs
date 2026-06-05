using GamerGuardian.Models;

namespace GamerGuardian.Services;

/// <summary>
/// Per-setting long-form documentation. Looked up by <c>settingId</c>
/// (same id used in <see cref="IMonitoredSetting"/>, <see cref="SettingDocs"/>,
/// and the change log). Surfaced in the UI's "Learn more" expander and also
/// dumped into <c>docs/SETTINGS-REFERENCE.md</c>.
///
/// <para>If a setting id has no entry here, the UI falls back to the one-line
/// Description on the row and the "Learn more" expander is hidden -- that's
/// the marker for "we owe this setting some real docs."</para>
///
/// <para>The content here is intentionally opinionated -- it reflects what
/// GamerGuardian recommends, not a neutral encyclopedia of every possible
/// Windows knob. Stay honest about risks; users trust the doc more than the
/// recommendation. Reversibility is included on every entry so a worried
/// user can see how to undo any change before they make it.</para>
/// </summary>
public static class SettingDocsCatalog
{
    public static SettingDetails? Get(string settingId)
    {
        if (settingId is null) return null;

        if (settingId.StartsWith("service:"))
        {
            var name = settingId["service:".Length..];
            return Services.TryGetValue(name, out var d) ? d : null;
        }
        if (settingId.StartsWith("ai.app:"))
        {
            var pkg = settingId["ai.app:".Length..];
            return AiApps.TryGetValue(pkg, out var d) ? d : null;
        }
        if (settingId.StartsWith("hdr:")) return Hdr;
        if (settingId.StartsWith("refresh:")) return RefreshRate;
        if (settingId.StartsWith("resolution:")) return Resolution;

        return Globals.TryGetValue(settingId, out var g) ? g : null;
    }

    /// <summary>Every documented setting. Used by docs generation and tests.</summary>
    public static IEnumerable<SettingDetails> All =>
        Globals.Values.Concat(Services.Values).Concat(AiApps.Values)
               .Append(Hdr).Append(RefreshRate).Append(Resolution);

    /// <summary>
    /// Plain-text rendering of a SettingDetails suitable for the in-app
    /// "Learn more" expander. Same content as the markdown reference but
    /// without markdown markers so a single &lt;TextBlock TextWrapping="Wrap"/&gt;
    /// can render it. Returns empty string for null input (UI hides the expander).
    /// </summary>
    public static string FormatForExpander(string settingId)
    {
        var d = Get(settingId);
        if (d is null) return string.Empty;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Recommended: {d.Recommended}");
        sb.AppendLine();
        sb.AppendLine("What it does");
        sb.AppendLine(d.What);
        sb.AppendLine();
        sb.AppendLine("Why you'd change it");
        sb.AppendLine(d.Why);
        sb.AppendLine();
        sb.AppendLine("How it helps");
        sb.AppendLine(d.HowItHelps);
        sb.AppendLine();
        sb.AppendLine("Per-scenario recommendation");
        foreach (var (scenario, rec) in d.Scenarios)
            sb.AppendLine($"  - {scenario}: {rec}");
        sb.AppendLine();
        sb.AppendLine("Risks");
        sb.AppendLine(d.Risks);
        sb.AppendLine();
        sb.AppendLine("Reversible via");
        sb.Append(d.ReversibleVia);
        return sb.ToString();
    }

    // ---- Helpers ----------------------------------------------------------

    private static IReadOnlyDictionary<string, string> Scenarios(params (string scenario, string recommendation)[] entries)
    {
        var d = new Dictionary<string, string>(entries.Length);
        foreach (var (s, r) in entries) d[s] = r;
        return d;
    }

    // ---- Global toggles ---------------------------------------------------

    private static readonly Dictionary<string, SettingDetails> Globals = new()
    {
        ["gamemode"] = new(
            SettingId: "gamemode",
            DisplayName: "Windows Game Mode",
            What: "Windows 10/11 feature that tells the OS to prioritize the foreground app when it detects a game: CPU/GPU resources are biased toward the game, Windows Update reboots are deferred during gameplay, and background app push notifications are paused.",
            Why: "Game Mode is essentially free on modern Windows -- it's been the default since 1809. The only reason to think about it is if a specific game shows stuttering that goes away when Game Mode is off (rare, but documented for some GPU+driver combos).",
            HowItHelps: "Small but measurable input-latency reduction on systems with background work happening. Suppresses Windows Update mid-game reboots.",
            Scenarios: Scenarios(
                ("Competitive FPS", "On -- no measurable downside; consistent frame pacing"),
                ("Streaming + game", "On -- but verify your encoder isn't being deprioritized (rare)"),
                ("Casual single-player", "On"),
                ("Productivity / not gaming", "Doesn't matter; Windows ignores Game Mode for non-game foreground apps")),
            Recommended: "On (Windows default)",
            Risks: "Some users report frame-rate stuttering or capture glitches on specific GPU/driver/game combos. If you only see stuttering with Game Mode on, turn it off and re-test.",
            ReversibleVia: "Set HKCU\\Software\\Microsoft\\GameBar\\AutoGameModeEnabled = 1 (or delete the value)."),

        ["gamedvr"] = new(
            SettingId: "gamedvr",
            DisplayName: "Game DVR background recording",
            What: "Windows Game Bar's continuous rolling-buffer recording of the active game. While enabled, the OS encodes and buffers game video so you can press Win+Alt+G to save the last X seconds.",
            Why: "Continuous encoding is a constant tax on framerate and GPU. On older systems it's noticeable (5-10%). On modern GPUs the cost is small but nonzero. Most serious players already use NVIDIA App / OBS for clips and don't need the OS buffer.",
            HowItHelps: "Frees the GPU's video encoder and removes a constant background overhead. Lets third-party capture tools claim the encoder exclusively (NVENC, AMD Re-Live, etc.).",
            Scenarios: Scenarios(
                ("Competitive FPS", "Off"),
                ("Streaming + game", "Off (use OBS / NVIDIA App for capture)"),
                ("Casual single-player", "Personal taste; leave On if you use Win+Alt+G clips"),
                ("Productivity / not gaming", "Off")),
            Recommended: "Off",
            Risks: "You lose the 'save last 30s' shortcut. Game Bar itself (overlay, FPS counter, performance widgets) still works.",
            ReversibleVia: "Set HKCU\\System\\GameConfigStore\\GameDVR_Enabled = 1 and HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR\\AppCaptureEnabled = 1."),

        ["hags"] = new(
            SettingId: "hags",
            DisplayName: "Hardware-accelerated GPU Scheduling",
            What: "Lets the GPU's own scheduling processor own VRAM allocation and command submission instead of the CPU-side Windows display driver. Requires a supported GPU (NVIDIA Pascal+ / AMD Polaris+) and a reboot to switch.",
            Why: "On supported GPUs it reduces CPU overhead per frame and can lower input latency. Required for DLSS Frame Generation and some other features that rely on GPU-managed queues.",
            HowItHelps: "1-5% framerate improvement in CPU-bound games. Smoother frame pacing under variable load. Enables modern GPU features that won't work without it.",
            Scenarios: Scenarios(
                ("Competitive FPS", "On -- especially helpful for CPU-bound titles like CS2 / Valorant"),
                ("Streaming + game", "On"),
                ("Casual single-player", "On"),
                ("Productivity / not gaming", "On (Windows 11 default)"),
                ("Professional GPU work (rendering, ML)", "Off -- some workloads prefer driver-side scheduling")),
            Recommended: "On",
            Risks: "Rare driver instability on first-generation HAGS-supported GPUs. Some professional/emulation apps prefer it off. Toggle requires a reboot.",
            ReversibleVia: "Set HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\\HwSchMode = 1 and reboot."),

        ["memintegrity"] = new(
            SettingId: "memintegrity",
            DisplayName: "Memory Integrity / VBS (Core Isolation)",
            What: "Hypervisor-Enforced Code Integrity. Runs the Windows kernel inside a Hyper-V-protected memory region so unsigned or compromised kernel drivers can't write to protected code. Part of the broader Virtualization-Based Security stack.",
            Why: "Real security feature -- meaningfully reduces certain malware classes' ability to load kernel drivers. But the hypervisor's transitions cost CPU on every kernel call, which shows up as worse 1% lows in many games.",
            HowItHelps: "Disabling can recover 5-15% framerate in CPU-bound games (especially 1% lows). On Ryzen, the win can be larger. Tradeoff is security: think hard before flipping this.",
            Scenarios: Scenarios(
                ("Competitive FPS where every percent matters", "Off (accept the security tradeoff knowingly)"),
                ("Casual / mixed-use", "On -- security beats the framerate"),
                ("Productivity", "On"),
                ("Streaming + game", "On -- the difference under stream encoding load is minor"),
                ("Anti-cheat-protected games", "On -- Vanguard, BattlEye, EAC may refuse to launch with it off")),
            Recommended: "On (default)",
            Risks: "Major: reduced kernel-driver protection. Some anti-cheat (Riot Vanguard especially) requires it on. Some kernel-mode hardware (cheap KVMs, old drivers) won't load with it on -- that's the tradeoff in the other direction.",
            ReversibleVia: "Set HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\\Enabled = 1 and reboot."),

        ["sysresponse"] = new(
            SettingId: "sysresponse",
            DisplayName: "System Responsiveness",
            What: "Registry knob that reserves a percentage of CPU time for non-multimedia tasks. Default value 20 = 20% reserved. Lower = more CPU available for multimedia tasks (which includes games registered via MMCSS).",
            Why: "Drops the reservation from 20% to 10% so games tagged as multimedia get more CPU during contention.",
            HowItHelps: "Tiny but measurable improvement on CPU-bound games. Most useful on lower-core-count CPUs where 20% is a lot of reserved time.",
            Scenarios: Scenarios(
                ("Competitive FPS", "10 (gaming)"),
                ("Pro audio", "0 (audio guides usually recommend 0; gives the audio scheduler full priority)"),
                ("Casual gaming", "10 or default"),
                ("Productivity", "20 (default)")),
            Recommended: "10",
            Risks: "Very low at 10. At 0, rare audio glitches under sustained CPU load. Reboot is required for the value to take effect.",
            ReversibleVia: "Set HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\SystemResponsiveness = 20."),

        ["netthrottle"] = new(
            SettingId: "netthrottle",
            DisplayName: "Network Throttling",
            What: "Rate-limits outbound network packets during multimedia tasks to prevent network I/O from starving them. Default value 10 = throttled. FFFFFFFF (4294967295) = disabled.",
            Why: "For online games, this throttling can introduce micro-stutter in netcode. Removing it lets netcode run at full rate.",
            HowItHelps: "Smoother online experience in competitive games. Removes a known source of input-to-server latency variability.",
            Scenarios: Scenarios(
                ("Competitive online (CS2, Valorant, Apex, etc.)", "Disabled"),
                ("Casual online", "Disabled"),
                ("Single-player offline", "Doesn't matter"),
                ("Streaming", "Disabled (your encoder paces itself)")),
            Recommended: "Disabled (FFFFFFFF)",
            Risks: "Very low. In theory multimedia apps could see slightly less reliable timing if your network is saturated -- in practice not observable.",
            ReversibleVia: "Set HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\NetworkThrottlingIndex = 10."),

        ["usbsuspend"] = new(
            SettingId: "usbsuspend",
            DisplayName: "USB Selective Suspend (global)",
            What: "Windows power feature that suspends idle USB devices to save power. The device wakes when Windows touches it again. Applies per-device but flipping this global flag disables the default-suspend behavior.",
            Why: "For HID devices (gaming mice, keyboards, headsets), the wake-from-suspend introduces a noticeable first-input delay -- the cursor pauses for a moment, the first keystroke after a long idle is dropped, or a USB headset pops.",
            HowItHelps: "Eliminates the first-input lag on cold mouse/keyboard input. Removes random audio pops on cheap USB DACs/headsets that are sensitive to suspend cycles.",
            Scenarios: Scenarios(
                ("Desktop gaming PC", "Disabled"),
                ("Laptop on battery", "Enabled -- the power saving matters more than first-input lag"),
                ("Laptop plugged in / docked", "Disabled"),
                ("USB audio interface (streaming / recording)", "Disabled")),
            Recommended: "Disabled (for desktops)",
            Risks: "Slightly higher idle power draw (typically 1-3 W). Negligible heat. On laptops, observably faster battery drain.",
            ReversibleVia: "Set HKLM\\SYSTEM\\CurrentControlSet\\Services\\USB\\DisableSelectiveSuspend = 0 and reboot."),

        ["gamestask"] = new(
            SettingId: "gamestask",
            DisplayName: "Games multimedia task profile",
            What: "The Multimedia Class Scheduler Service (MMCSS) has named task profiles. The Games profile controls Priority, Scheduling Category, and SFIO Priority for processes that register against it. Most modern games register here when they call AvSetMmThreadCharacteristics(\"Games\").",
            Why: "The Games profile defaults aren't the most aggressive Windows can do. Boosting them (Priority=2, Scheduling Category=High, SFIO Priority=High) gives game threads a stronger claim on CPU and I/O during contention.",
            HowItHelps: "More consistent frame pacing on busy systems. Better behavior when streaming/encoding alongside the game. Lower 1% lows under contention.",
            Scenarios: Scenarios(
                ("Competitive FPS", "Gaming (boosted)"),
                ("Casual single-player", "Gaming"),
                ("Streaming + game", "Gaming (OBS uses its own multimedia profile; doesn't conflict)"),
                ("Productivity", "Default")),
            Recommended: "Gaming (boosted)",
            Risks: "Very low. Background tasks deprioritized slightly further -- in practice not observable on a system with any CPU headroom.",
            ReversibleVia: "Restore default values for Priority / Scheduling Category / SFIO Priority under HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games."),

        ["mouseaccel"] = new(
            SettingId: "mouseaccel",
            DisplayName: "Mouse \"Enhance pointer precision\"",
            What: "A cursor acceleration curve applied to all mouse movement. Moving the mouse faster makes the cursor travel disproportionately further than the same distance moved slowly.",
            Why: "Breaks 1:1 muscle memory between mouse and cursor. Every competitive FPS disables acceleration in-game; mismatching the OS-level setting means your desktop pointer behaves differently from your in-game crosshair.",
            HowItHelps: "Consistent 1:1 mouse-to-cursor mapping. Aim feels the same in-game and out of game. Easier to dial in pointer speed by DPI alone.",
            Scenarios: Scenarios(
                ("Competitive FPS", "Off"),
                ("Casual gaming", "Off (just because consistency helps)"),
                ("Productivity / office work", "Default On is fine; acceleration helps with quick navigation across large displays"),
                ("Touchscreen / pen / tablet primary", "Doesn't matter")),
            Recommended: "Off",
            Risks: "Cursor feels 'slower' at low DPI when you first turn it off. Counter: bump your Mouse pointer speed slider or your DPI.",
            ReversibleVia: "Settings > Mouse > Additional mouse settings > Pointer Options > re-check 'Enhance pointer precision'."),

        ["fso"] = new(
            SettingId: "fso",
            DisplayName: "Fullscreen optimizations (global)",
            What: "A Windows feature that runs games requesting true Fullscreen Exclusive in a borderless-windowed mode wrapped by the DWM compositor. This lets the OS draw overlays (Win+G, notifications) on top of the game without an alt-tab.",
            Why: "FSO is a quality-of-life feature -- faster alt-tab, working overlays, no display-mode-change flicker. But true FSE can be marginally faster (lower input latency) for some titles, which is why some pros disable it globally.",
            HowItHelps: "Disabling globally forces true FSE where the game supports it. Saves 1-2 frames of latency on some titles by skipping the DWM compositor pass.",
            Scenarios: Scenarios(
                ("Competitive FPS chasing every ms", "Off (forces true FSE)"),
                ("Casual single-player", "On (default; better QoL)"),
                ("Streaming + game", "On (FSE breaks some capture modes -- Display capture, Game capture with anti-cheat)"),
                ("Productivity", "Doesn't matter")),
            Recommended: "On (Windows default)",
            Risks: "Some games crash or render incorrectly without FSO. Some overlays (Discord, NVIDIA App) can't draw over true FSE. Alt-tab is slower / triggers a mode switch.",
            ReversibleVia: "Delete GameDVR_FSEBehaviorMode and the related values from HKCU\\System\\GameConfigStore."),

        ["vrr"] = new(
            SettingId: "vrr",
            DisplayName: "Variable Refresh Rate (DirectX)",
            What: "Windows Settings > Display > Graphics > Variable Refresh Rate. Tells Windows to expose VRR to DirectX games even when the game doesn't explicitly request it. NOT the same as Dynamic Refresh Rate (DRR) in Advanced Display, which scales refresh based on content.",
            Why: "Allows VRR (G-Sync / FreeSync) to work in games that don't have a VRR / G-Sync toggle of their own.",
            HowItHelps: "Smooth frame delivery between the display's min and max refresh -- no tearing, no V-Sync input latency.",
            Scenarios: Scenarios(
                ("VRR-capable display + supported GPU", "On"),
                ("Display without VRR", "Doesn't matter -- no-op"),
                ("Multi-monitor with one VRR display", "On (Windows handles per-monitor)"),
                ("Competitive FPS with V-Sync off as standard", "On (still benefits from VRR-paced delivery up to the FPS cap)")),
            Recommended: "On if you have VRR hardware",
            Risks: "Very low. Some older driver+game combos can flicker -- if you see it, turn off in-game V-Sync, leave VRR on.",
            ReversibleVia: "Delete VRROptimizeEnable from HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers."),

        ["powerplan"] = new(
            SettingId: "powerplan",
            DisplayName: "Active Windows power plan",
            What: "The active Windows power scheme. Controls CPU throttling thresholds, sleep timers, hard-drive spindown, USB selective suspend, and dozens of other power-related defaults.",
            Why: "Balanced (the default) lets the OS dynamically scale CPU clocks to save power, which costs you a few ms of latency at the start of any CPU-bound burst. High Performance / Ultimate Performance keeps CPU clocks pegged at the top of the curve for predictable response.",
            HowItHelps: "Eliminates CPU clock-ramp latency. First-frame and first-input responses feel snappier. Background tasks finish faster.",
            Scenarios: Scenarios(
                ("Competitive FPS / Streaming", "High Performance or a tuned custom plan"),
                ("Casual single-player on a desktop", "High Performance"),
                ("Laptop on battery", "Balanced (saves power)"),
                ("Laptop plugged in", "High Performance"),
                ("Idle workstation", "Balanced (drops back to power-saving when idle)")),
            Recommended: "High Performance",
            Risks: "Higher idle power draw -- typically 10-30 W on desktop, more on high-end. Components run a few degrees warmer. Fan noise slightly higher. On laptops on battery: noticeably worse battery life.",
            ReversibleVia: "powercfg /setactive SCHEME_BALANCED (or pick another plan from Settings > System > Power)."),

        ["cpuplan"] = new(
            SettingId: "cpuplan",
            DisplayName: "CPU-optimized gaming power plan",
            What: "A GamerGuardian-authored power plan, built by cloning Balanced and writing a small set of processor overrides tuned for your detected CPU. The app detects the CPU at startup and offers either the best-matching prebuilt Windows plan or this custom optimized plan. The optimized recipe is tiered: an exact model match uses a precise recipe, a recognized family uses a family recipe, and an unknown CPU gets a safe generic tune (clearly labeled).",
            Why: "The right gaming power plan is CPU-dependent. Single-CCD X3D wants core parking OFF; asymmetric dual-CCD X3D (e.g. 9950X3D) wants the frequency CCD PARKED so games stay on the cache CCD; symmetric and non-X3D parts want no parking. High Performance is wrong in both directions for these chips -- it pins clocks and disables the parking modern schedulers rely on. The optimized plan is always a Balanced clone (never a High Performance personality) with aggressive boost.",
            HowItHelps: "Aggressive boost lets the CPU reach and hold its gaming clocks; correct parking keeps game threads on the right cores; faster ramp thresholds reduce clock-up latency. All without the heat/boost-headroom cost of High Performance.",
            Scenarios: Scenarios(
                ("Single-CCD X3D (9850X3D / 9800X3D / 7800X3D)", "Build optimized -- no parking, aggressive boost"),
                ("Asymmetric dual-CCD X3D (9950X3D / 7950X3D)", "Build optimized -- parks frequency CCD; also verify BIOS CPPC=Driver + AMD V-Cache service + Game Bar"),
                ("Non-X3D / Intel hybrid", "Build optimized (no parking / leave Thread Director) or suggest Balanced"),
                ("Unknown CPU", "Build optimized uses a labeled generic tune, or suggest the best prebuilt plan")),
            Recommended: "Build optimized for your CPU (or suggest Balanced)",
            Risks: "Low. The plan is additive -- your existing Windows plans are never modified or deleted, and you can switch back at any time. For asymmetric dual-CCD X3D the power plan alone is not sufficient: it depends on the AMD CCD-routing stack (BIOS CPPC=Driver, the 3D V-Cache Optimizer service, and Xbox Game Bar), which the app surfaces but cannot set.",
            ReversibleVia: "Switch the active plan back via Settings > System > Power, or 'powercfg /setactive SCHEME_BALANCED'. The GamerGuardian plan can be deleted from the legacy Power control panel if you no longer want it."),

        // ---- Windows AI ---------------------------------------------------

        ["ai.copilot"] = new(
            SettingId: "ai.copilot",
            DisplayName: "Windows Copilot",
            What: "The system-wide Copilot taskbar button and the Win+C keyboard shortcut. Setting Off writes the TurnOffWindowsCopilot policy in both HKLM and HKCU and hides the taskbar button.",
            Why: "Copilot calls Microsoft cloud endpoints, runs background processes, and consumes resources when invoked. Some users prefer not to send page or document context to cloud AI services.",
            HowItHelps: "Removes the always-present taskbar button so it can't be invoked accidentally; blocks Win+C from launching it; prevents the policy from being unset by routine Windows configuration changes.",
            Scenarios: Scenarios(
                ("Privacy-conscious users", "Off"),
                ("Gaming setup", "Off -- no benefit, removes one more background subsystem"),
                ("Active Copilot user", "On"),
                ("Enterprise with separate compliance", "Whatever your IT policy says")),
            Recommended: "Off (GamerGuardian default for users who specifically open this tab)",
            Risks: "None for performance. You lose access to Copilot if you change your mind -- toggle back on or delete the policy values to restore.",
            ReversibleVia: "Delete TurnOffWindowsCopilot from HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsCopilot and HKCU\\Software\\Policies\\Microsoft\\Windows\\WindowsCopilot."),

        ["ai.recall"] = new(
            SettingId: "ai.recall",
            DisplayName: "Windows Recall + AI data analysis",
            What: "Recall captures snapshots of your screen every few seconds and indexes them with on-device AI so you can later search 'what was that thing I had open last Tuesday.' Currently rolling out on Copilot+ PCs (Snapdragon X, recent Intel Core Ultra, AMD Ryzen AI). Setting Off writes AllowRecallEnablement=0 and DisableAIDataAnalysis=1 in the HKLM WindowsAI policy key.",
            Why: "Two distinct concerns: (1) privacy -- continuous screen capture, even local-only, is a meaningful new surface; (2) performance -- the NPU and disk I/O have nonzero cost. The policy block stops new snapshotting; it does NOT delete existing snapshots.",
            HowItHelps: "Stops Recall snapshotting at the policy level (Windows honors this without question, unlike a per-app toggle). Blocks the broader Windows AI Data Analysis surface that future features may opt into.",
            Scenarios: Scenarios(
                ("Anyone who doesn't actively want Recall", "Off"),
                ("Copilot+ PC user who specifically wants Recall", "On (and also delete this app's policy block)"),
                ("Privacy-conscious", "Off"),
                ("Gaming setup", "Off")),
            Recommended: "Off",
            Risks: "None for security or stability. You lose Recall if you change your mind. Existing Recall snapshots are not deleted by this toggle -- to remove them, go to Settings > Privacy & security > Recall & snapshots > Delete all snapshots.",
            ReversibleVia: "Delete AllowRecallEnablement and DisableAIDataAnalysis from HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI."),

        ["ai.clicktodo"] = new(
            SettingId: "ai.clicktodo",
            DisplayName: "Click-to-Do (Snipping Tool AI)",
            What: "An AI action layer in the Snipping Tool. After capturing a screenshot, an AI button appears offering 'summarize this,' 'rewrite,' 'search the web for this,' etc. Setting Off writes DisableClickToDo in both the HKLM WindowsAI policy and the per-user HKCU Shell\\ClickToDo key.",
            Why: "AI actions hit Microsoft cloud services. Removes a feature most users don't use anyway.",
            HowItHelps: "Standard Snipping Tool screenshot functionality is completely unaffected; only the AI actions panel is hidden.",
            Scenarios: Scenarios(
                ("Anyone who doesn't use Click-to-Do", "Off"),
                ("Active Click-to-Do user", "On"),
                ("Privacy-conscious", "Off")),
            Recommended: "Off",
            Risks: "None. You lose the AI actions panel from screenshots.",
            ReversibleVia: "Delete DisableClickToDo from HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI and HKCU\\Software\\Microsoft\\Windows\\Shell\\ClickToDo."),

        ["ai.edge"] = new(
            SettingId: "ai.edge",
            DisplayName: "Microsoft Edge Copilot / Hubs / GenAI",
            What: "Three Edge enterprise policies flipped together: HubsSidebarEnabled (the always-present right-edge Copilot icon), CopilotPageContext (sending current-page contents to Copilot for processing), and GenAILocalFoundationalModelSettings (Edge's in-browser local generative AI).",
            Why: "Hides the persistent Copilot icon, blocks page contents from leaving the browser for AI processing, and disables in-browser AI generation.",
            HowItHelps: "Cleaner Edge UI; less background AI activity in the browser; no accidental page-context shares with cloud AI.",
            Scenarios: Scenarios(
                ("Privacy-conscious", "Off"),
                ("Anyone who doesn't actively use Edge Copilot", "Off"),
                ("Active Edge Copilot user", "On"),
                ("Enterprise environments", "Whatever your IT policy says")),
            Recommended: "Off",
            Risks: "You lose Edge's built-in Copilot sidebar and AI features. Standard browsing is unaffected.",
            ReversibleVia: "Delete HubsSidebarEnabled, CopilotPageContext, and GenAILocalFoundationalModelSettings from HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge."),

        ["ai.notepadpaint"] = new(
            SettingId: "ai.notepadpaint",
            DisplayName: "Notepad Rewrite + Paint AI features",
            What: "Per-user disable of Notepad Rewrite, Paint Cocreator, Paint Image Creator, and Paint Generative Erase. Plus a per-user opt-out of Paint's experiment-targeting service and the HKLM machine-wide Paint policy that stops Image Creator from offering itself before per-user toggle. Combined HKCU + HKLM writes.",
            Why: "These features bolt cloud AI onto otherwise simple apps. Users who don't use the AI features may prefer Notepad and Paint without the buttons. v0.1.39 added the targeting opt-out + HKLM policy so the disable holds across new Paint experiments rolling out under feature flags.",
            HowItHelps: "Notepad and Paint behave like classic versions; no AI action buttons; no cloud calls when you open a document or image; no opt-in prompts when MS rolls out new AI experiments.",
            Scenarios: Scenarios(
                ("Anyone who doesn't use AI in Notepad / Paint", "Off"),
                ("Active user of Paint Cocreator / Image Creator", "On"),
                ("Privacy-conscious", "Off")),
            Recommended: "Off",
            Risks: "None. AI features disappear from those two apps.",
            ReversibleVia: "Delete the registry values under HKCU\\Software\\Microsoft\\Notepad (RewriteEnabled), HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Paint (DisableCocreator, DisableImageCreator, DisableGenerativeErase), HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Applets\\Paint\\View (IsSignedUpForTargetingService), and HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Paint (DisableImageCreator)."),

        ["ai.settingssearch"] = new(
            SettingId: "ai.settingssearch",
            DisplayName: "Search box AI suggestions + taskbar companion",
            What: "Three HKCU values: BingSearchEnabled=0 (the value Windows 11 actually honors for the AI/web suggestion layer in the search box -- this is the authoritative one), IsDynamicSearchBoxEnabled=0 (search highlights / the companion content), and the legacy DisableSearchBoxSuggestions=1 policy (best-effort -- unreliable on Win11). HKCU only -- no UAC.",
            Why: "The search box's AI suggestion layer calls Microsoft web endpoints to suggest answers as you type. The taskbar companion is a floating overlay some Windows 11 builds enable by default. Both are noise for users who use the search box for files and apps.",
            HowItHelps: "Search box returns local files / apps only -- no web suggestions, no Copilot answers inline, no taskbar companion widget. Indexing itself (Start menu, Explorer, Outlook) is untouched.",
            Scenarios: Scenarios(
                ("Anyone who uses Windows Search for local files only", "Off"),
                ("Active user of search box web/Copilot suggestions", "On"),
                ("Privacy-conscious", "Off")),
            Recommended: "Off",
            Risks: "You lose the web-suggestion layer and the taskbar companion. Search itself works exactly as before.",
            ReversibleVia: "Delete BingSearchEnabled from HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search, IsDynamicSearchBoxEnabled from HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings, and DisableSearchBoxSuggestions from HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer (or set them back to 1 / 1 / absent)."),

        ["ai.actions"] = new(
            SettingId: "ai.actions",
            DisplayName: "Windows AI Actions",
            What: "Windows' shell-level AI Actions surface (right-click \"rewrite with AI / summarize / search the web for this\" on selected text, images, etc.). Toggled via the FeatureManagement override hive -- two numeric feature IDs (1853569164 and 4098520719) get EnabledState = 1 (force-disabled).",
            Why: "AI Actions is a 24H2-era Windows feature that adds AI suggestions to right-click menus and similar surfaces. The FeatureManagement override is the documented kill switch (zoicware uses the same IDs).",
            HowItHelps: "Right-click menus, image picker dialogs, and other shell surfaces stop showing AI action options. No cloud calls when you right-click an image or selected text.",
            Scenarios: Scenarios(
                ("Anyone who doesn't use AI right-click actions", "Off"),
                ("Active user of AI Actions", "On"),
                ("Privacy-conscious", "Off")),
            Recommended: "Off",
            Risks: "You lose the AI options in right-click / image-context menus. The right-click menus themselves still work for everything else.",
            ReversibleVia: "Delete EnabledState from HKLM\\SYSTEM\\ControlSet001\\Control\\FeatureManagement\\Overrides\\8\\1853569164 and 4098520719. Feature IDs may change in future Windows builds -- if you see new AI Actions surfaces after a Windows Update, GamerGuardian's existing overrides will still hold for these two but new feature IDs would need a new monitor entry."),

        ["ai.inputinsights"] = new(
            SettingId: "ai.inputinsights",
            DisplayName: "Typing / input insights data collection",
            What: "Two HKCU settings that disable Windows' typing-data and ink-data harvesting: RestrictImplicitTextCollection (blocks the OS from saving the plain text you type for personalized suggestions) and InsightsEnabled (the per-user master switch in the Input settings panel). HKCU only -- no UAC.",
            Why: "By default, Windows builds a per-user typing model from text you've typed in apps. That data feeds personalized suggestions, autocorrect, and (in some Insider builds) AI features. Users who don't want their typing harvested can opt out at the OS level.",
            HowItHelps: "Stops the OS from saving samples of what you type. Personalized typing suggestions degrade slightly (Windows falls back to the global suggestion model); everything else works as normal.",
            Scenarios: Scenarios(
                ("Privacy-conscious", "Off"),
                ("Anyone who doesn't notice typing suggestions getting better over time", "Off"),
                ("Active user of personalized typing suggestions on a touch keyboard", "On")),
            Recommended: "Off",
            Risks: "Typing suggestions become slightly less personalized over time. No effect on autocorrect or basic spell-check.",
            ReversibleVia: "Delete RestrictImplicitTextCollection from HKCU\\Software\\Microsoft\\InputPersonalization and InsightsEnabled from HKCU\\Software\\Microsoft\\input\\Settings."),

        ["ai.office"] = new(
            SettingId: "ai.office",
            DisplayName: "Microsoft 365 Copilot in Word / Excel / OneNote",
            What: "Disables the Copilot button + ribbon entries inside the desktop Word, Excel, and OneNote apps; also opts the machine out of Microsoft's AI model training on document contents (HKLM\\Policies\\office admin template).",
            Why: "Microsoft 365 Copilot is opt-in by license, but the UI affordances still show up in every Word document; disabling cleanly removes them. The training opt-out is a separate policy that prevents document text from being used to train Microsoft's models even if a user happens to invoke Copilot.",
            HowItHelps: "No Copilot ribbon. No suggestions panel. No accidental cloud calls. No document-text contribution to model training.",
            Scenarios: Scenarios(
                ("Office user who doesn't have a Copilot license", "Off -- the buttons are just dead weight"),
                ("Office user with Copilot license, occasional use", "On (or selectively per app)"),
                ("Privacy-conscious / regulated workflows", "Off"),
                ("Office not installed", "Doesn't matter -- the toggle is a no-op")),
            Recommended: "Off",
            Risks: "If you do have a Copilot license and want to use it, you lose the in-app entry points. Reverse by deleting the keys.",
            ReversibleVia: "Delete EnableCopilot from HKCU\\Software\\Microsoft\\Office\\16.0\\Word\\Options and Excel\\Options, CopilotEnabled from HKCU\\...\\OneNote\\Options\\Copilot, and disabletraining from HKLM\\SOFTWARE\\Policies\\Microsoft\\office\\16.0\\common\\ai\\training\\general."),
    };

    // ---- Display settings (single entry per kind) -------------------------

    private static readonly SettingDetails Hdr = new(
        SettingId: "hdr",
        DisplayName: "HDR (High Dynamic Range)",
        What: "Per-display HDR toggle. Enables 10-bit color depth, the wider Rec.2020 / DCI-P3 gamut, and PQ EOTF for HDR-capable monitors. Backed by the Windows DisplayConfig CCD API (the same API the OS Settings page uses).",
        Why: "HDR is genuinely better picture quality in supported games and movies -- but Windows is notorious for silently turning HDR off after sleep, driver updates, or display reconnects. Monitoring this catches the regression automatically.",
        HowItHelps: "Keeps HDR enabled so games that detect it use HDR rendering paths. Catches silent OS regressions and auto-restores.",
        Scenarios: Scenarios(
            ("HDR monitor, gaming/movies focus", "On"),
            ("HDR monitor, SDR-only content", "Off (Windows SDR-in-HDR is often visually worse than native SDR)"),
            ("SDR-only monitor", "Doesn't matter; the toggle will be ignored"),
            ("Multi-monitor with mixed HDR support", "On for HDR displays only; per-display managed")),
        Recommended: "On (for HDR-capable displays where you watch HDR content)",
        Risks: "Some games look wrong in HDR (washed out, oversaturated) due to game-side tone mapping bugs -- a per-game preference. Windows SDR-in-HDR rendering is often visually worse than native SDR for desktop work.",
        ReversibleVia: "Settings > System > Display > select the display > toggle HDR off.");

    private static readonly SettingDetails RefreshRate = new(
        SettingId: "refresh",
        DisplayName: "Display refresh rate",
        What: "Per-display refresh rate. GamerGuardian's recommended target is the display's maximum supported rate at the current resolution. Backed by ChangeDisplaySettingsEx (DEVMODE.dmDisplayFrequency).",
        Why: "Higher refresh = lower input-to-photon latency and smoother motion. Windows sometimes silently drops the refresh rate after sleep, driver updates, or external display disconnects -- monitoring catches this.",
        HowItHelps: "Keeps your display at its full rated refresh rate for both desktop and games (some games respect the desktop rate, some override).",
        Scenarios: Scenarios(
            ("Any monitor above 60 Hz", "Maximum supported -- always"),
            ("60 Hz display", "Doesn't matter; 60 is your max"),
            ("Multi-monitor with mixed rates", "Maximum per display"),
            ("Power-saving / laptop on battery", "Consider Fixed at a lower rate to save power -- the cost is real on high-Hz panels")),
        Recommended: "Maximum supported",
        Risks: "Very low. Some VRR displays produce eye-noticeable flicker at certain refresh rates in dark scenes -- if you see it, try the next rate down.",
        ReversibleVia: "Settings > System > Display > Advanced display > Choose a refresh rate.");

    private static readonly SettingDetails Resolution = new(
        SettingId: "resolution",
        DisplayName: "Display resolution",
        What: "Per-display resolution. Optional preference -- only enforced when the user explicitly pins a resolution. Backed by ChangeDisplaySettingsEx.",
        Why: "Lets you pin a specific resolution per display. Useful for users who run games at the desktop resolution and want absolute stability against Windows occasionally changing it after driver updates.",
        HowItHelps: "Catches the case where Windows downgrades you to a lower resolution after a display reconnect or driver update.",
        Scenarios: Scenarios(
            ("Single fixed-resolution setup", "Pin to native resolution"),
            ("Multiple display configurations (docked / undocked laptop)", "Don't pin -- let Windows handle"),
            ("Variable resolution gaming (different per game)", "Don't pin")),
        Recommended: "Don't enforce unless you have a specific reason",
        Risks: "Pinning can fight legitimate display changes (docking a laptop, plugging in a different monitor).",
        ReversibleVia: "Uncheck 'Monitor this setting' for Resolution on the display tab.");

    // ---- UWP AI apps ------------------------------------------------------

    private static readonly Dictionary<string, SettingDetails> AiApps = new()
    {
        ["Microsoft.Copilot"] = new(
            SettingId: "ai.app:Microsoft.Copilot",
            DisplayName: "Microsoft Copilot (UWP)",
            What: "The standalone Copilot UWP app that Windows installs alongside the system-wide Copilot integration. Hundreds of MB on disk.",
            Why: "If you've blocked Copilot via the system policy toggle above, the standalone app is dead weight. Removing it reclaims disk and removes the launcher entry.",
            HowItHelps: "Reclaims disk space. No more Copilot app launcher in Start.",
            Scenarios: Scenarios(
                ("Already disabled Copilot policy", "Remove"),
                ("Active Copilot user", "Don't remove"),
                ("Worried Windows Update might re-provision it", "Remove + tick Auto-apply silently")),
            Recommended: "Remove (only after the system policy is set to Off)",
            Risks: "Reinstalling requires the Microsoft Store. Windows Update may re-provision the app after major updates -- the AutoApply tick handles that.",
            ReversibleVia: "Install 'Microsoft Copilot' from the Microsoft Store."),

        ["Microsoft.Windows.Ai.Copilot.Provider"] = new(
            SettingId: "ai.app:Microsoft.Windows.Ai.Copilot.Provider",
            DisplayName: "Windows AI Copilot Provider",
            What: "Background provider package that backs the Windows AI Copilot surface (the in-OS Copilot integration, not the standalone app).",
            Why: "Pairs with the Copilot system policy block. With the policy off, the provider is unused.",
            HowItHelps: "Removes the background provider; small reduction in installed-app surface.",
            Scenarios: Scenarios(
                ("Already disabled Copilot policy", "Remove"),
                ("Active Copilot user", "Don't remove"),
                ("Privacy-conscious", "Remove")),
            Recommended: "Remove (only after the system policy is Off)",
            Risks: "Re-provisioned by Windows Update; tick AutoApply to keep it removed. Reinstall requires the Microsoft Store.",
            ReversibleVia: "Install via Microsoft Store or wait for Windows Update to re-provision."),

        ["MicrosoftWindows.Client.AIX"] = new(
            SettingId: "ai.app:MicrosoftWindows.Client.AIX",
            DisplayName: "Windows AI Experience",
            What: "AI Experience component shipped on Copilot+ PCs. Backs the AI settings panel and assorted shell AI integrations.",
            Why: "On non-Copilot+ PCs the component is often unused. On Copilot+ PCs, removing it deletes the AI settings UI.",
            HowItHelps: "Reclaims disk; removes the AI settings panel from Settings.",
            Scenarios: Scenarios(
                ("Non-Copilot+ PC", "Remove if you don't use any Windows AI"),
                ("Copilot+ PC with Recall / Click-to-Do disabled", "Remove"),
                ("Active AI user on Copilot+ PC", "Don't remove")),
            Recommended: "Remove if you don't use Windows AI features",
            Risks: "AI Settings panel disappears. Re-provisioned by Windows Update.",
            ReversibleVia: "Install via Microsoft Store or wait for Windows Update to re-provision."),
    };

    // ---- Services (one entry per ServiceCatalog.Name) ---------------------
    //
    // Recommended values mirror ServiceDefinition.RecommendedTarget where
    // present, and Default elsewhere. Risk language is honest -- some of these
    // (Spooler, IPHelper) are genuinely user-dependent.

    private static readonly Dictionary<string, SettingDetails> Services = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DiagTrack"] = SvcRec(
            "DiagTrack",
            "Connected User Experiences and Telemetry",
            "Collects diagnostic and usage data and sends it to Microsoft. Always-on background sender.",
            "Constant background CPU + network for telemetry you didn't ask for. Disabling is safe on consumer Windows.",
            "Removes a constant low-level background sender. Small CPU and bandwidth saving.",
            recommended: "Disabled",
            risks: "Microsoft loses diagnostic data from your machine. Rare reports of Windows Update issues in unusual configurations; never observed on a desktop with a normal update cadence.",
            reversibleVia: "Set-Service -Name DiagTrack -StartupType Automatic"),

        ["MapsBroker"] = SvcRec(
            "MapsBroker",
            "Downloaded Maps Manager",
            "Background service that downloads and updates offline maps for the Windows Maps app.",
            "If you never use the Maps app, this service does nothing useful and downloads map data you'll never look at.",
            "Cuts background disk I/O and reclaims a small amount of memory.",
            recommended: "Disabled",
            risks: "If you do open the Maps app later, offline map functionality won't work until you re-enable.",
            reversibleVia: "Set-Service -Name MapsBroker -StartupType AutomaticDelayed"),

        ["WSearch"] = SvcRec(
            "WSearch",
            "Windows Search",
            "Indexes file contents, properties, and Start-menu app names. Powers Start search, Explorer search, and Outlook search.",
            "Indexing is heavy on slow disks and during initial scan. On a fast NVMe with SSD-friendly index location, the cost is minor.",
            "Disabling stops indexing entirely. Start menu app search still works (uses a separate cache); file-content search degrades to slow scan.",
            recommended: "Default (don't manage)",
            risks: "Major: Start search becomes much worse, Explorer search slows to a crawl, Outlook search may stop working entirely. Only disable on machines where you never search.",
            reversibleVia: "Set-Service -Name WSearch -StartupType AutomaticDelayed"),

        ["SysMain"] = SvcRec(
            "SysMain",
            "Superfetch / SysMain",
            "Tracks app usage patterns and preloads code into RAM before you launch the app. On HDDs this provides large startup-time improvements; on NVMe SSDs the benefit is marginal.",
            "Hotly debated. On NVMe systems with abundant RAM, the cost is minor and the benefit is small -- Microsoft now recommends leaving it on. On slower drives or tight-RAM systems, the I/O cost can be more visible than the prefetch benefit.",
            "Slightly lower idle disk I/O.",
            recommended: "Default (leave on -- current Microsoft guidance)",
            risks: "Disabling can slow first-launch of frequently-used apps. On HDDs the slowdown is severe.",
            reversibleVia: "Set-Service -Name SysMain -StartupType Automatic"),

        ["dosvc"] = SvcRec(
            "DoSvc",
            "Delivery Optimization",
            "Peer-to-peer Windows Update downloads. Lets your PC download update bits from other LAN/Internet peers and lets your PC contribute uplink to other peers.",
            "Background bandwidth use, both upload and download, that you didn't authorize per-update. Especially impactful on metered or asymmetric connections.",
            "Stops the bandwidth contribution entirely. Updates still install normally; they just come from Microsoft directly.",
            recommended: "Disabled (via Group Policy override -- the SCM start type is reverted by WaaSMedicSvc)",
            risks: "Slightly slower update downloads on networks with many other Windows PCs. None observable on a single-PC household.",
            reversibleVia: "Delete the DODownloadMode value from HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DeliveryOptimization."),

        ["WerSvc"] = SvcRec(
            "WerSvc",
            "Windows Error Reporting Service",
            "Collects crash dumps and reports them to Microsoft.",
            "If you don't send crash reports, the service has nothing useful to do.",
            "Removes background CPU spent on crash data collection.",
            recommended: "Disabled",
            risks: "Crash dump collection stops. If you ever need to share a crash report with Microsoft support, re-enable first.",
            reversibleVia: "Set-Service -Name WerSvc -StartupType Manual"),

        ["RetailDemo"] = SvcRec(
            "RetailDemo",
            "Retail Demo Service",
            "Supports the in-store retail demo mode for Windows.",
            "Useless outside retail kiosks.",
            "Removes a useless service from the running list.",
            recommended: "Disabled",
            risks: "None.",
            reversibleVia: "Set-Service -Name RetailDemo -StartupType Manual"),

        ["XboxGipSvc"] = SvcRec(
            "XboxGipSvc",
            "Xbox Accessory Management",
            "Backs Xbox-branded accessories (Xbox One controllers, Elite Series 2, etc.) for updates and configuration.",
            "If you don't use Xbox-branded controllers via the Xbox Accessories app, this service has nothing to do.",
            "Removes a constantly-running USB-watching service.",
            recommended: "Default (Manual -- don't manage)",
            risks: "Xbox Accessories app won't be able to update controllers or change controller profiles. Game-pad input itself works regardless (handled by xinput).",
            reversibleVia: "Set-Service -Name XboxGipSvc -StartupType Manual"),

        ["XblAuthManager"] = SvcRec(
            "XblAuthManager",
            "Xbox Live Auth Manager",
            "Authentication broker for Xbox Live. Required by Microsoft Store games, Game Pass, and the Xbox app.",
            "If you don't use Microsoft Store games or Game Pass, this service is unused.",
            "Removes a constantly-running auth-broker service.",
            recommended: "Default (Manual)",
            risks: "Microsoft Store games and Game Pass titles will fail to launch (authentication error).",
            reversibleVia: "Set-Service -Name XblAuthManager -StartupType Manual"),

        ["XblGameSave"] = SvcRec(
            "XblGameSave",
            "Xbox Live Game Save",
            "Cloud save sync for Microsoft Store / Game Pass titles.",
            "If you don't use Microsoft Store games or Game Pass, this service is unused.",
            "Removes a small background sync service.",
            recommended: "Default (Manual)",
            risks: "Cloud saves stop syncing for affected titles.",
            reversibleVia: "Set-Service -Name XblGameSave -StartupType Manual"),

        ["XboxNetApiSvc"] = SvcRec(
            "XboxNetApiSvc",
            "Xbox Live Networking Service",
            "Multiplayer and networking glue for Microsoft Store games.",
            "If you don't use Microsoft Store games online, this service is unused.",
            "Removes a small background service.",
            recommended: "Default (Manual)",
            risks: "Microsoft Store multiplayer titles will fail to find lobbies or connect.",
            reversibleVia: "Set-Service -Name XboxNetApiSvc -StartupType Manual"),

        ["WSAIFabricSvc"] = SvcRec(
            "WSAIFabricSvc",
            "Windows AI Fabric Service",
            "Backs the on-device AI runtime that Copilot+ features (Copilot, Recall, Click-to-Do) call into.",
            "If you've disabled the Windows AI policy toggles in the Windows AI tab, the AI features won't be invoked and the service is unused.",
            "Removes a process backing AI features you've already disabled. Pairs naturally with the policy toggles in the Windows AI tab.",
            recommended: "Default (Manual) -- only Disable if you've also flipped the AI policy toggles",
            risks: "If you re-enable any AI feature later, it will fail to launch until you re-enable this service.",
            reversibleVia: "Set-Service -Name WSAIFabricSvc -StartupType Manual"),

        ["AarSvc"] = SvcRec(
            "AarSvc",
            "Agent Activation Runtime Service",
            "Per-user service that backs Windows AI agent activations -- the runtime Copilot voice, Cortana legacy hooks, and certain shell AI surfaces call into when they want to launch in the background.",
            "Like WSAIFabricSvc, this service is paired with the Windows AI policy toggles. If you've disabled Copilot, Recall, etc. at the policy level, AarSvc has nothing useful to do; if any AI feature is still enabled, leave it on.",
            "Removes a per-user service backing AI features you've already disabled. Pairs naturally with WSAIFabricSvc + the Windows AI policy toggles.",
            recommended: "Default (Manual) -- only Disable if you've also flipped the AI policy toggles + disabled WSAIFabricSvc",
            risks: "Per-user services use a generated suffix on the actual service name (AarSvc_<hex>). GamerGuardian disables the template definition so every new per-user instance starts disabled, but existing user sessions may need a logoff/logon to pick up the change. If you re-enable any AI feature later, it will fail to launch until you re-enable this service.",
            reversibleVia: "Set-Service -Name AarSvc -StartupType Manual"),
    };

    private static SettingDetails SvcRec(
        string name, string display, string what, string why, string howItHelps,
        string recommended, string risks, string reversibleVia)
    {
        return new SettingDetails(
            SettingId: $"service:{name}",
            DisplayName: display,
            What: what,
            Why: why,
            HowItHelps: howItHelps,
            Scenarios: Scenarios(
                ("Competitive FPS", recommended),
                ("Streaming + game", recommended),
                ("Casual single-player", recommended),
                ("Productivity / mixed-use", recommended)),
            Recommended: recommended,
            Risks: risks,
            ReversibleVia: reversibleVia);
    }
}
