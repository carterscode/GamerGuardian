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
/// <summary>
/// The family a setting id belongs to, as determined by its prefix. See
/// <see cref="SettingDocsCatalog.ParseId"/> — that method is the only place the
/// prefix convention is implemented.
/// </summary>
public enum SettingIdKind
{
    /// <summary>No recognized prefix — a global setting id such as "hags", and also
    /// the bare, colon-less display ids ("hdr", "refresh", "resolution", "drr").</summary>
    Global,
    Service,
    AiApp,
    ScheduledTask,
    Hdr,
    RefreshRate,
    Resolution,
    Drr,
}

public static class SettingDocsCatalog
{
    /// <summary>
    /// Splits a setting id into the family its prefix names and the remainder after
    /// that prefix. This is the single implementation of the id-prefix convention
    /// (<c>service:</c>, <c>ai.app:</c>, <c>task:</c>, <c>hdr:</c>, <c>refresh:</c>,
    /// <c>resolution:</c>, <c>drr:</c>) shared by <see cref="Get"/> and
    /// <see cref="SettingSectionMap"/> so the two can never drift apart.
    ///
    /// <para>An id with no recognized prefix is <see cref="SettingIdKind.Global"/>
    /// and the remainder is the whole id. Note the bare display ids ("hdr",
    /// "refresh", "resolution", "drr") carry no colon and so parse as Global —
    /// callers that care must handle both spellings.</para>
    /// </summary>
    public static (SettingIdKind Kind, string Remainder) ParseId(string? settingId)
    {
        if (settingId is null) return (SettingIdKind.Global, string.Empty);
        if (settingId.StartsWith("service:")) return (SettingIdKind.Service, settingId["service:".Length..]);
        if (settingId.StartsWith("ai.app:")) return (SettingIdKind.AiApp, settingId["ai.app:".Length..]);
        if (settingId.StartsWith("task:")) return (SettingIdKind.ScheduledTask, settingId["task:".Length..]);
        if (settingId.StartsWith("hdr:")) return (SettingIdKind.Hdr, settingId["hdr:".Length..]);
        if (settingId.StartsWith("refresh:")) return (SettingIdKind.RefreshRate, settingId["refresh:".Length..]);
        if (settingId.StartsWith("resolution:")) return (SettingIdKind.Resolution, settingId["resolution:".Length..]);
        if (settingId.StartsWith("drr:")) return (SettingIdKind.Drr, settingId["drr:".Length..]);
        return (SettingIdKind.Global, settingId);
    }

    public static SettingDetails? Get(string settingId)
    {
        if (settingId is null) return null;

        var (kind, rest) = ParseId(settingId);
        return kind switch
        {
            SettingIdKind.Service => Services.TryGetValue(rest, out var s) ? s : null,
            SettingIdKind.AiApp => AiApps.TryGetValue(rest, out var a) ? a : null,
            SettingIdKind.ScheduledTask => ScheduledTasks.TryGetValue(rest, out var t) ? t : null,
            SettingIdKind.Hdr => Hdr,
            SettingIdKind.RefreshRate => RefreshRate,
            SettingIdKind.Resolution => Resolution,
            SettingIdKind.Drr => Drr,
            _ => Globals.TryGetValue(rest, out var g) ? g : null,
        };
    }

    /// <summary>Every documented setting. Used by docs generation and tests.</summary>
    public static IEnumerable<SettingDetails> All =>
        Globals.Values.Concat(Services.Values).Concat(AiApps.Values).Concat(ScheduledTasks.Values)
               .Append(Hdr).Append(RefreshRate).Append(Resolution).Append(Drr);

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
        sb.AppendLine("Why this is the recommendation");
        sb.AppendLine(d.Why);
        sb.AppendLine();
        sb.AppendLine("What it does");
        sb.AppendLine(d.What);
        sb.AppendLine();
        sb.AppendLine("How it helps");
        sb.AppendLine(d.HowItHelps);
        sb.AppendLine();
        sb.AppendLine("Pros & cons of each choice");
        foreach (var t in ProsConsFor(settingId))
        {
            sb.AppendLine($"  {t.Choice}");
            sb.AppendLine($"    Pro: {t.Pro}");
            sb.AppendLine($"    Con: {t.Con}");
        }
        sb.AppendLine();
        sb.AppendLine("Per-scenario recommendation");
        foreach (var (scenario, rec) in d.Scenarios)
            sb.AppendLine($"  - {scenario}: {rec}");
        sb.AppendLine();
        sb.AppendLine("Risks");
        sb.AppendLine(d.Risks);
        sb.AppendLine();
        sb.AppendLine("Command line (PowerShell)");
        var verify = SettingDocs.VerifyCommandFor(settingId);
        var apply = SettingDocs.ApplyCommandFor(settingId, GamingRawFor(settingId));
        var reverse = SettingDocs.ReverseCommandFor(settingId);
        if (!string.IsNullOrWhiteSpace(verify))
        {
            sb.AppendLine("  Check the current value:");
            sb.AppendLine($"    {verify}");
        }
        if (!string.IsNullOrWhiteSpace(apply))
        {
            sb.AppendLine("  Apply the gaming-optimized value:");
            sb.AppendLine($"    {apply}");
        }
        sb.AppendLine("  Reverse it (restore the Windows default):");
        sb.AppendLine($"    {(string.IsNullOrWhiteSpace(reverse) ? d.ReversibleVia : reverse)}");
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

    private static IReadOnlyList<ChoiceTradeoff> Pc(params (string choice, string pro, string con)[] entries)
    {
        var list = new List<ChoiceTradeoff>(entries.Length);
        foreach (var (c, p, n) in entries) list.Add(new ChoiceTradeoff(c, p, n));
        return list;
    }

    /// <summary>
    /// The raw value to embed in the "apply the gaming-optimized value" command in
    /// the expander. Empty means "let <see cref="SettingDocs.ApplyCommandFor"/> use
    /// its own gaming-optimized fallback" -- correct for every setting except
    /// Memory Integrity, whose apply fallback is the safe (On) value, not the
    /// gaming (Off) one.
    /// </summary>
    private static string GamingRawFor(string settingId) => settingId switch
    {
        "memintegrity" => "0",
        _ => string.Empty,
    };

    /// <summary>
    /// Pro/con of each choice for a setting, written for someone who knows nothing
    /// about it. Hand-authored entries live in <see cref="ProsConsById"/>; service
    /// and UWP-app rows (uniform "disable to save vs keep the feature" shape) are
    /// synthesized from the catalog entry so every documented setting has one.
    /// </summary>
    public static IReadOnlyList<ChoiceTradeoff> ProsConsFor(string settingId)
    {
        if (settingId is null) return Array.Empty<ChoiceTradeoff>();
        if (ProsConsById.TryGetValue(settingId, out var pc)) return pc;

        var d = Get(settingId);
        if (d is null) return Array.Empty<ChoiceTradeoff>();

        // Synthesized fallback (services + anything not hand-authored): the choice
        // is always "turn the feature/service off" vs "leave it as Windows ships
        // it". Pro/con of disabling come straight from the entry's own honest
        // HowItHelps / Risks text.
        bool recommendDisable = !d.Recommended.StartsWith("Default", StringComparison.OrdinalIgnoreCase)
            && !d.Recommended.StartsWith("Don't", StringComparison.OrdinalIgnoreCase)
            && !d.Recommended.StartsWith("On (", StringComparison.OrdinalIgnoreCase);
        return Pc(
            (recommendDisable ? "Disable / remove it (recommended)" : "Disable / remove it",
             d.HowItHelps,
             d.Risks),
            (recommendDisable ? "Leave it as Windows ships it" : "Leave it as Windows ships it (recommended)",
             "The feature it backs keeps working exactly as before -- nothing to re-enable later.",
             "Keeps the background work running, so you don't get the resource/idle saving above."));
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
            What: "Windows Game Bar's continuous rolling-buffer recording of the active game. While enabled, the OS encodes and buffers game video so you can press Win+Alt+G to save the last X seconds. GamerGuardian covers the two per-user capture toggles AND the machine-wide AllowGameDVR policy -- the part Windows re-enables after feature updates -- so the lockdown holds.",
            Why: "Continuous encoding is a constant tax on framerate and GPU. On older systems it's noticeable (5-10%). On modern GPUs the cost is small but nonzero. Most serious players already use NVIDIA App / OBS for clips and don't need the OS buffer.",
            HowItHelps: "Frees the GPU's video encoder and removes a constant background overhead. Lets third-party capture tools claim the encoder exclusively (NVENC, AMD Re-Live, etc.).",
            Scenarios: Scenarios(
                ("Competitive FPS", "Off"),
                ("Streaming + game", "Off (use OBS / NVIDIA App for capture)"),
                ("Casual single-player", "Personal taste; leave On if you use Win+Alt+G clips"),
                ("Productivity / not gaming", "Off")),
            Recommended: "Off",
            Risks: "You lose the 'save last 30s' shortcut. Game Bar itself (overlay, FPS counter, performance widgets) still works.",
            ReversibleVia: "Set HKCU\\System\\GameConfigStore\\GameDVR_Enabled = 1 and HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR\\AppCaptureEnabled = 1, and delete AllowGameDVR from HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR (the app does all three when you set it back to On)."),

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

        ["vbs"] = new(
            SettingId: "vbs",
            DisplayName: "Virtualization-Based Security (full stack)",
            What: "The complete VBS disable -- a superset of the Memory Integrity toggle. VBS runs a Hyper-V micro-hypervisor under Windows to host security services: Memory Integrity (HVCI), Credential Guard, System Guard Secure Launch, kernel-mode Hardware-enforced Stack Protection, and (on 24H2+, community-reported rather than formally documented) a Windows Hello sign-in scenario. Disabling only Memory Integrity leaves VBS itself running if any other scenario is active. This toggle writes an explicit 0 to the DeviceGuard master switch, EVERY scenario subkey (including ones future Windows versions add), Credential Guard's LsaCfgFlags, and the Group Policy mirror keys -- explicit zeros, not deletions, because Microsoft documents that absent values get re-defaulted by feature updates while explicit zeros survive them. It also deletes the per-scenario re-enable metadata (WasEnabledBy / EnabledBootId / ChangedInBootCycle) wherever present -- the values Windows uses to restore HVCI after upgrades.",
            Why: "Every VBS service pays the hypervisor transition cost on kernel calls. Disabling only Memory Integrity recovers most of it, but Credential Guard (default-on for domain-joined 22H2+ Enterprise/Education machines and Pro machines that previously ran it) and the other scenarios keep the hypervisor resident and keep re-enabling paths open. This is the 'I want it actually, durably off' switch.",
            HowItHelps: "5-15% better framerate and 1% lows in CPU-bound games (Tom's Hardware: up to 10% average, up to 15% better 1% lows on a 13900K + RTX 4090; ~5% on post-2018 CPUs with MBEC, more on older CPUs). Registry-only: WSL2, Docker and Hyper-V keep working -- the hypervisor itself is untouched (the optional bcdedit hypervisorlaunchtype step that breaks them is deliberately NOT automated; see Risks).",
            Scenarios: Scenarios(
                ("Competitive FPS where every percent matters", "Off -- accept the security tradeoff knowingly"),
                ("Valorant / Riot Vanguard players", "On -- Vanguard REQUIRES Memory Integrity since July 2024; this toggle breaks Valorant"),
                ("Casual / mixed-use", "On -- security beats the framerate"),
                ("Work PC under corporate management (Intune/GPO)", "On -- domain policy will fight the change; the drift monitor will show the tug-of-war"),
                ("Dedicated gaming rig, no sensitive credentials", "Off is a defensible choice")),
            Recommended: "On (default) -- only disable if you understand the tradeoff",
            Risks: "Major: disables kernel-driver tamper protection, credential isolation (pass-the-hash defenses) and boot-time firmware protection in one move. Breaks Valorant (Vanguard requires HVCI). Windows Security shows Memory Integrity greyed out ('managed by your administrator') while disabled -- that's this app's policy keys closing the re-enable loophole; flipping the toggle back to Enabled removes them (do that BEFORE uninstalling GamerGuardian, or the grey-out persists until you delete the SOFTWARE\\Policies\\Microsoft\\Windows\\DeviceGuard values yourself). If VBS was enabled with UEFI lock (Locked=1 / LsaCfgFlags=1), firmware keeps VBS running after these writes: clearing it needs Microsoft's SecConfig.efi opt-out (mountvol the EFI partition, bcdedit a boot entry with 'loadoptions DISABLE-LSA-ISO' -- older DG_Readiness_Tool releases also passed DISABLE-VBS -- then reboot and confirm at the physical-presence prompt) -- the app detects and reports the lock but will not automate firmware surgery. Going further with 'bcdedit /set hypervisorlaunchtype off' is optional and NOT done by the app: it breaks WSL2, Docker, Windows Sandbox, Hyper-V and Windows Hello ESS in exchange for a further, smaller gain on top of the zeroed scenarios. Note: msinfo32 saying 'a hypervisor has been detected' does NOT mean VBS is on -- verify with the WMI command in the verify snippet (VirtualizationBasedSecurityStatus 0 = off).",
            ReversibleVia: "Flip the toggle back to Enabled: the app sets EnableVirtualizationBasedSecurity = 1, restores Scenarios\\HypervisorEnforcedCodeIntegrity Enabled = 1 + WasEnabledBy = 2 (un-greys the Windows Security toggle), and deletes the policy-mirror zeros and LsaCfgFlags = 0 so Windows defaults take over again. Reboot required."),

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
            Recommended: "CPU-aware -- the best-matching prebuilt for your CPU (Balanced on modern CPUs, whose boost algorithm beats a pegged High Performance plan), or build the custom optimized plan on the CPU / Power tab",
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

        // ---- System toggles -----------------------------------------------

        ["powerthrottling"] = new(
            SettingId: "powerthrottling",
            DisplayName: "Power Throttling",
            What: "Windows Power Throttling reduces the clock/power of threads it considers background or idle to save energy. Disabled via HKLM\\...\\Power\\PowerThrottling\\PowerThrottlingOff=1 (requires elevation). Absence means the Windows default (throttling on). This is a registry setting, not a power-scheme change.",
            Why: "On a desktop chasing sustained performance, throttling can clip background/helper threads a game relies on. Turning it off keeps all threads at full clock.",
            HowItHelps: "More consistent performance for multi-threaded games and background helpers; no surprise downclocking under the OS's idle heuristics.",
            Scenarios: Scenarios(
                ("Desktop / plugged-in gaming", "Disabled (gaming)"),
                ("Laptop on battery", "Default -- throttling saves real battery"),
                ("Streaming + game", "Disabled (gaming)")),
            Recommended: "Disabled (gaming) on a desktop; Default on battery",
            Risks: "Higher power draw and heat, especially on laptops on battery. No stability risk.",
            ReversibleVia: "Delete PowerThrottlingOff from HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling to restore the Windows default."),

        ["faststartup"] = new(
            SettingId: "faststartup",
            DisplayName: "Fast Startup (hybrid boot)",
            What: "Saves the kernel session to the hiberfile on shutdown so the next boot skips part of initialization. Driven by HKLM\\...\\Session Manager\\Power\\HiberbootEnabled=0 to disable (requires elevation and a reboot to take effect).",
            Why: "Fast Startup means 'shutdown' isn't a true cold boot -- drivers and hardware can carry stale state across restarts, which occasionally causes USB/GPU/peripheral quirks. Turning it off makes every shutdown a clean boot.",
            HowItHelps: "Cleaner, more predictable boots; resolves a class of intermittent driver/peripheral issues that 'a real restart fixes'. Low drift -- mostly a set-once toggle.",
            Scenarios: Scenarios(
                ("Troubleshooting flaky USB/GPU state", "Disabled (gaming)"),
                ("Wants the fastest possible boot, no quirks", "Default (leave on)"),
                ("Dual-boot with another OS", "Disabled (gaming) -- Fast Startup locks the disk")),
            Recommended: "Disabled (gaming)",
            Risks: "Boots are slightly slower (a true cold boot). No stability risk -- this is the pre-Win8 default behavior.",
            ReversibleVia: "Set HiberbootEnabled = 1 in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power (Control Panel > Power Options > Choose what the power buttons do > Turn on fast startup)."),

        ["visualfx"] = new(
            SettingId: "visualfx",
            DisplayName: "Visual effects (best performance)",
            What: "The Windows UI animation/effects profile. 'Adjust for best performance' (VisualFXSetting=2) disables window animations, menu fades, smooth-scrolling, and shadows. GamerGuardian writes VisualFXSetting=2 plus the matching best-performance UserPreferencesMask; the per-effect changes finish applying on the next sign-out.",
            Why: "Disabling desktop animations removes compositor work and makes window/menu interactions instant. The gain is mostly desktop snappiness rather than in-game FPS, but some users prefer the zero-animation feel.",
            HowItHelps: "Instant window/menu response, no animation delays, slightly less idle GPU compositor work.",
            Scenarios: Scenarios(
                ("Wants the snappiest desktop", "Best performance (gaming)"),
                ("Likes Windows animations / fluent effects", "Default"),
                ("Low-end / integrated GPU", "Best performance (gaming)")),
            Recommended: "Best performance (gaming) for a snappy desktop; Default if you like the animations",
            Risks: "Purely cosmetic -- the desktop looks flatter (no fades/animations). No stability or functionality impact. Full effect applies after sign-out.",
            ReversibleVia: "Set VisualFXSetting = 0 in HKCU\\...\\Explorer\\VisualEffects (System Properties > Performance > 'Let Windows choose' or 'Adjust for best appearance'). GamerGuardian sets it to 0 when you choose Default."),

        // ---- Privacy / telemetry ------------------------------------------

        ["privacy.advertisingid"] = new(
            SettingId: "privacy.advertisingid",
            DisplayName: "Advertising ID",
            What: "A per-user identifier (HKCU\\...\\AdvertisingInfo\\Enabled) that apps can read to build a cross-session advertising profile of you. Direct HKCU value -- no elevation needed.",
            Why: "There's no gaming or functionality reason to keep the advertising ID on. Disabling it stops apps from correlating your activity under a stable ad identity.",
            HowItHelps: "Apps fall back to requesting a fresh, non-correlatable ID (or none). No effect on app functionality.",
            Scenarios: Scenarios(
                ("Privacy-conscious", "Disabled"),
                ("Gaming setup", "Disabled -- no downside"),
                ("Doesn't care about ad targeting", "Either; Disabled is the safe default")),
            Recommended: "Disabled",
            Risks: "None functional. Ads you see may be slightly less 'relevant' -- which is the point.",
            ReversibleVia: "Set HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo\\Enabled = 1 (Settings > Privacy & security > General > 'Let apps show me personalized ads')."),

        ["privacy.tailoredexp"] = new(
            SettingId: "privacy.tailoredexp",
            DisplayName: "Tailored experiences",
            What: "Lets Windows use your diagnostic data to personalize tips, ads, and recommendations (HKCU\\...\\Privacy\\TailoredExperiencesWithDiagnosticDataEnabled). Direct HKCU value -- no elevation.",
            Why: "Removes Microsoft's use of your diagnostic data to target suggestions and promotional content in the Start menu, Settings, and lock screen.",
            HowItHelps: "Fewer suggested/promoted items surfaced by the OS. No effect on app or game functionality.",
            Scenarios: Scenarios(
                ("Privacy-conscious", "Disabled"),
                ("Gaming setup", "Disabled -- no downside"),
                ("Likes Windows tips/suggestions", "Enabled")),
            Recommended: "Disabled",
            Risks: "None functional. You stop seeing personalized Windows tips and suggestions.",
            ReversibleVia: "Set HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy\\TailoredExperiencesWithDiagnosticDataEnabled = 1 (Settings > Privacy & security > Diagnostics & feedback)."),

        ["privacy.cdp"] = new(
            SettingId: "privacy.cdp",
            DisplayName: "Cross-Device Platform (CDP)",
            What: "The 'Continue experiences on this device' / shared-experiences subsystem that lets nearby and account-linked devices hand off activities, share the clipboard, and discover each other. Disabled via the HKLM policy EnableCdp=0 (requires elevation). Absence of the value means the Windows default (CDP on).",
            Why: "CDP runs background discovery/sync that most desktop gamers don't use, and Windows re-enables it after feature updates -- exactly the drift the monitor re-asserts.",
            HowItHelps: "Stops the cross-device discovery/sync background activity. Reasserted automatically if a feature update turns it back on.",
            Scenarios: Scenarios(
                ("Single desktop, no device handoff", "Disabled (gaming)"),
                ("Uses Phone Link / cross-device clipboard", "Default (leave on)"),
                ("Privacy-conscious", "Disabled (gaming)")),
            Recommended: "Disabled (gaming) if you don't use cross-device features",
            Risks: "Cross-device features (handoff, shared clipboard with phones/other PCs, nearby-device discovery) stop working. Phone Link's deeper integrations may be affected.",
            ReversibleVia: "Delete EnableCdp from HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System to restore the Windows default."),

        ["privacy.activityhistory"] = new(
            SettingId: "privacy.activityhistory",
            DisplayName: "Activity History / Timeline",
            What: "Collection and publishing of your activity feed (Timeline). Disabled via three HKLM policy values set to 0 together: EnableActivityFeed, PublishUserActivities, UploadUserActivities (requires elevation, one prompt). Absence means the Windows default (on).",
            Why: "Activity History records what you do across apps and (when signed in) uploads it. Most gamers don't use Timeline, and Windows can re-enable the feed after feature updates.",
            HowItHelps: "Stops the activity feed from collecting and publishing. Reasserted automatically after updates that turn it back on.",
            Scenarios: Scenarios(
                ("Doesn't use Timeline", "Disabled (gaming)"),
                ("Uses Timeline / cross-device activity resume", "Default (leave on)"),
                ("Privacy-conscious", "Disabled (gaming)")),
            Recommended: "Disabled (gaming)",
            Risks: "Timeline stops showing your recent activities and cross-device resume won't work. No effect on app/game functionality.",
            ReversibleVia: "Delete EnableActivityFeed, PublishUserActivities, and UploadUserActivities from HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System to restore the Windows default."),

        // ---- Network ------------------------------------------------------

        ["network.nagle"] = new(
            SettingId: "network.nagle",
            DisplayName: "Nagle's algorithm (TCP no-delay)",
            What: "Nagle's algorithm batches small outgoing TCP packets to reduce overhead. Disabling it (TcpAckFrequency=1, TCPNoDelay=1 under each network adapter's interface key) sends small packets immediately. GamerGuardian asserts this on every active physical adapter in one elevation prompt; reversal deletes the values to restore the Windows default.",
            Why: "For latency-sensitive online games, batching can add a few ms of delay to small input/state packets. Turning Nagle off can shave that -- but the benefit is genuinely contested and per-hardware.",
            HowItHelps: "Potentially lower, more consistent latency for small-packet game netcode. On many setups the difference is unmeasurable; on a few it helps.",
            Scenarios: Scenarios(
                ("Competitive online shooter", "Disabled (gaming) -- try it, measure, revert if worse"),
                ("Stable connection, no latency issues", "Default -- don't fix what isn't broken"),
                ("Wi-Fi / high-latency link", "Default -- more likely to hurt than help here")),
            Recommended: "Default unless you've measured a benefit -- this is a contested, per-hardware tweak",
            Risks: "Real: disabling Nagle can INCREASE bufferbloat-related latency or harm throughput on some links (especially Wi-Fi or congested connections). It is not a guaranteed win. Revert if your latency or stability gets worse.",
            ReversibleVia: "Delete TcpAckFrequency and TCPNoDelay from each HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\\{GUID} (GamerGuardian does this across all adapters when you choose Default)."),

        ["network.nicpower"] = new(
            SettingId: "network.nicpower",
            DisplayName: "NIC power management",
            What: "The per-adapter 'Allow the computer to turn off this device to save power' setting (PnPCapabilities under the adapter's network-class instance). Disabling it keeps the NIC fully powered. GamerGuardian asserts this on every active physical adapter in one elevation prompt; reversal clears the bits to restore the default. A reboot (or adapter disable/enable) is needed for it to take effect.",
            Why: "Letting Windows power down the NIC can cause brief stalls or micro-disconnects when it wakes -- noticeable as a hitch in online games. Keeping it powered avoids that.",
            HowItHelps: "No NIC sleep/wake cycles, so no wake-from-idle network stalls. Most useful on desktops.",
            Scenarios: Scenarios(
                ("Desktop online gaming", "Disabled (gaming)"),
                ("Laptop on battery", "Default -- the NIC power saving matters more"),
                ("Stable wired connection with no hitches", "Personal taste; Default is fine")),
            Recommended: "Default -- GamerGuardian treats this as a contested, per-hardware tweak. Desktops on wired Ethernet may gain from Disabled (no NIC wake stalls); test it and keep it only if your latency/hitching improves. Leave Default on a laptop on battery.",
            Risks: "Slightly higher idle power draw. On laptops on battery, measurably worse battery life. Contested per-hardware -- some adapters are unaffected either way. Needs a reboot to apply.",
            ReversibleVia: "Clear the 0x18 bits from PnPCapabilities under the adapter's class instance, or check 'Allow the computer to turn off this device' in Device Manager > the adapter > Power Management (GamerGuardian clears the bits across all adapters when you choose Default)."),

        // ---- Privacy data collection --------------------------------------

        ["privacy.speech"] = new(
            SettingId: "privacy.speech",
            DisplayName: "Online (cloud) speech recognition",
            What: "When enabled, Windows sends your voice audio to Microsoft's cloud for recognition (used by some dictation and voice features). Controlled by the per-user HasAccepted flag.",
            Why: "It's a privacy trade-off: your audio leaves the machine. Offline recognition / Voice Access keeps working without it.",
            HowItHelps: "Keeps voice audio on-device. No functional loss for offline voice typing and Voice Access.",
            Scenarios: Scenarios(
                ("Privacy-conscious / don't use voice", "Disabled"),
                ("Use cloud dictation heavily", "Enabled"),
                ("Mixed use", "Disabled -- offline recognition still works")),
            Recommended: "Disabled (privacy)",
            Risks: "Cloud-powered voice features lose accuracy or stop working. Offline Windows speech / Voice Access is unaffected.",
            ReversibleVia: "Settings > Privacy & security > Speech > Online speech recognition (or set HasAccepted = 1)."),

        ["privacy.inking"] = new(
            SettingId: "privacy.inking",
            DisplayName: "Inking & typing personalization",
            What: "Windows building a personal dictionary from your handwriting samples and contact names to improve suggestions -- and uploading some of it. Covers the master AcceptedPrivacyPolicy opt-in plus implicit ink collection and contact harvesting. (The typing-text side is the separate 'Typing / input insights' toggle on the Windows AI tab.)",
            Why: "It's a data-collection feature; turning it off stops the harvesting. Autocorrect still works, just less personalized.",
            HowItHelps: "Stops handwriting/contact data collection. Minimal day-to-day impact.",
            Scenarios: Scenarios(
                ("Privacy-conscious", "Disabled"),
                ("Heavy pen / handwriting user who wants better recognition", "Enabled"),
                ("Typical keyboard user", "Disabled")),
            Recommended: "Disabled (privacy)",
            Risks: "Handwriting recognition and word suggestions become less personalized. No functional breakage.",
            ReversibleVia: "Settings > Privacy & security > Inking & typing personalization (or set AcceptedPrivacyPolicy = 1)."),

        // ---- Debloat: ads, nags & suggested content -----------------------

        ["debloat.suggestedcontent"] = new(
            SettingId: "debloat.suggestedcontent",
            DisplayName: "Suggested content & silent app installs",
            What: "Windows 11's 'suggested content' machinery: silently installed promo apps (the Candy-Crush-style installs), Start-menu app suggestions, and 'tips, tricks & suggestions' cards. All live under the per-user ContentDeliveryManager key.",
            Why: "These are ads and unsolicited installs, not features. They cost disk, clutter Start, and re-appear after major updates.",
            HowItHelps: "Stops silent third-party app installs and removes Start/Settings suggestion cards.",
            Scenarios: Scenarios(
                ("Anyone who dislikes ads in the OS", "Disabled"),
                ("Want Microsoft's app suggestions", "Enabled"),
                ("Clean/minimal setup", "Disabled")),
            Recommended: "Disabled",
            Risks: "You stop seeing Microsoft's app/feature suggestions. No functional impact. Windows may re-enable some after a feature update -- tick Auto-apply to hold it.",
            ReversibleVia: "Settings > Personalization > Start and > Privacy > General toggles (or delete the ContentDeliveryManager values GamerGuardian set to 0)."),

        ["debloat.spotlight"] = new(
            SettingId: "debloat.spotlight",
            DisplayName: "Lock screen tips, fun facts & ads",
            What: "The Windows Spotlight overlay that shows 'fun facts', tips, and ad-like captions on the lock screen. Controlled by per-user ContentDeliveryManager flags.",
            Why: "Many users find the lock-screen captions and tips intrusive or ad-like.",
            HowItHelps: "Removes the tips/ad overlay from the lock screen.",
            Scenarios: Scenarios(
                ("Dislike lock-screen tips/ads", "Disabled"),
                ("Enjoy the Spotlight facts", "Enabled"),
                ("Use a custom lock-screen image", "Disabled")),
            Recommended: "Disabled",
            Risks: "Only suppresses the tips/ads overlay. If your lock-screen background is set to 'Windows Spotlight', switch it to Picture/Slideshow in Settings for a full opt-out.",
            ReversibleVia: "Settings > Personalization > Lock screen (or delete the ContentDeliveryManager overlay values)."),

        ["debloat.finishsetup"] = new(
            SettingId: "debloat.finishsetup",
            DisplayName: "\"Finish setting up your device\" nag",
            What: "The full-screen / notification SCOOBE prompts that nag you to set up OneDrive, a Microsoft account, or a Microsoft 365 subscription -- and resurface after feature updates. Controlled by UserProfileEngagement + a ContentDeliveryManager notification flag.",
            Why: "It's a recurring nag screen, not a feature. Most users have already decided and don't want to be asked again.",
            HowItHelps: "Suppresses the post-update 'finish setup' interruption.",
            Scenarios: Scenarios(
                ("Annoyed by the setup nag", "Disabled"),
                ("Want Windows' setup reminders", "Enabled"),
                ("Managed/clean setup", "Disabled")),
            Recommended: "Disabled",
            Risks: "You won't be prompted to finish optional account/OneDrive setup. Some newer build variants add prompt types this doesn't fully cover.",
            ReversibleVia: "Settings > System > Notifications > 'Suggest ways to get the most out of Windows' (or set ScoobeSystemSettingEnabled = 1)."),

        ["debloat.startrecommend"] = new(
            SettingId: "debloat.startrecommend",
            DisplayName: "Start menu recommendations & recent files",
            What: "The Start menu 'Recommended' section: AI/Iris-driven app and web suggestions plus the list of recently opened files. Per-user Explorer\\Advanced flags.",
            Why: "The recommendations are often ads/suggestions, and the recent-files list is a privacy leak on a shared screen.",
            HowItHelps: "Quiets the Recommended section and stops surfacing recently opened files in Start/jump lists.",
            Scenarios: Scenarios(
                ("Privacy on a shared screen", "Disabled"),
                ("Rely on recent files in Start", "Enabled"),
                ("Minimal Start menu", "Disabled")),
            Recommended: "Disabled",
            Risks: "Recently opened files stop appearing in Start and jump lists. On Windows 11 Home the Recommended section can't be fully emptied -- this removes the suggestions/recents that it can.",
            ReversibleVia: "Settings > Personalization > Start (toggles for recommendations and recently opened items), or delete the two Explorer\\Advanced values."),

        ["debloat.explorerads"] = new(
            SettingId: "debloat.explorerads",
            DisplayName: "File Explorer ad banners",
            What: "The 'sync provider notifications' in File Explorer -- the OneDrive / Microsoft 365 upsell banners shown in the navigation pane and status bar. Single per-user Explorer\\Advanced flag.",
            Why: "They're advertising inside the file manager. Disabling them is purely cosmetic with no downside.",
            HowItHelps: "Removes the promo banners from File Explorer.",
            Scenarios: Scenarios(
                ("Dislike ads in Explorer", "Disabled"),
                ("Want OneDrive sync prompts", "Enabled")),
            Recommended: "Disabled",
            Risks: "You won't see OneDrive/Office promotional banners. Genuine sync-status icons on files are unaffected.",
            ReversibleVia: "File Explorer > View > Options > View tab > 'Show sync provider notifications' (or set ShowSyncProviderNotifications = 1)."),

        ["debloat.feedback"] = new(
            SettingId: "debloat.feedback",
            DisplayName: "Windows feedback request popups",
            What: "The periodic 'rate your experience' dialogs Windows pops. Controlled by the per-user Siuf\\Rules\\NumberOfSIUFInPeriod count (0 = never).",
            Why: "On fresh installs these can fire frequently and interrupt you. Most users never want to be asked.",
            HowItHelps: "Stops the periodic feedback-request dialogs.",
            Scenarios: Scenarios(
                ("Don't want to be asked for feedback", "Disabled"),
                ("Windows Insider who submits feedback", "Enabled")),
            Recommended: "Disabled",
            Risks: "Windows stops prompting for feedback. You can still open Feedback Hub manually any time. Telemetry level is unaffected.",
            ReversibleVia: "Settings > Privacy & security > Diagnostics & feedback > Feedback frequency (or delete NumberOfSIUFInPeriod)."),

        // ---- Debloat: background bloat ------------------------------------

        ["debloat.widgets"] = new(
            SettingId: "debloat.widgets",
            DisplayName: "Widgets / News and interests",
            What: "The Windows 11 Widgets board (the left-edge weather button) that opens a web-connected MSN feed and fetches data in the background. Disabled machine-wide via the HKLM Dsh policy plus the per-user taskbar button flag.",
            Why: "It's a background web feed many users never open; the panel and its updater consume RAM/CPU and bandwidth.",
            HowItHelps: "Stops the Widgets process/feed and removes the taskbar button. Frees idle resources.",
            Scenarios: Scenarios(
                ("Never use Widgets", "Disabled"),
                ("Use the weather/news board daily", "Enabled"),
                ("Latency-sensitive gaming", "Disabled")),
            Recommended: "Disabled",
            Risks: "The Widgets board and its taskbar button disappear. The machine-wide policy write needs one UAC prompt.",
            ReversibleVia: "Settings > Personalization > Taskbar > Widgets (or delete the Dsh\\AllowNewsAndInterests policy value)."),

        ["debloat.edge"] = new(
            SettingId: "debloat.edge",
            DisplayName: "Edge startup boost & background mode",
            What: "Two Microsoft Edge behaviors: 'startup boost' keeps Edge processes resident from boot, and 'background mode' keeps it running after every window is closed. Set via HKLM Edge enterprise policies that survive Edge updates.",
            Why: "On a machine where Edge isn't the daily browser, these keep 150-500 MB of Edge resident for no benefit.",
            HowItHelps: "Edge stops pre-launching at boot and exits when you close it, freeing idle RAM/CPU. Edge still opens on demand.",
            Scenarios: Scenarios(
                ("Edge isn't your main browser", "Disabled"),
                ("Edge is your daily driver and you want fast launches", "Enabled"),
                ("Minimal background processes", "Disabled")),
            Recommended: "Disabled",
            Risks: "Edge cold-starts a little slower (no prelaunch). Does NOT block Edge or WebView2 -- apps that embed WebView2 keep working. Policy write needs one UAC prompt.",
            ReversibleVia: "Edge > Settings > System and performance (Startup boost / 'Continue running background extensions'), or delete the two Edge policy values."),

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

    private static readonly SettingDetails Drr = new(
        SettingId: "drr",
        DisplayName: "Dynamic Refresh Rate (DRR)",
        What: "Per-display Win11 22H2+ feature that dynamically boosts the refresh rate between a low 'virtual' rate (e.g. 60 Hz for static content, saving power) and the panel's physical max (e.g. 120/144 Hz for scrolling/ink). Read/written via the DisplayConfig CCD API (the BOOST_REFRESH_RATE path flag) -- user-mode, no elevation. Distinct from VRR (G-Sync/FreeSync).",
        Why: "DRR is mostly a laptop power feature. Some gamers prefer a fixed maximum refresh for consistent latency and disable DRR; others keep it on for battery. It needs a VRR-capable panel and a recent driver, so the toggle only appears on displays that actually support it.",
        HowItHelps: "Monitoring keeps DRR at your chosen state -- Windows can reset it after driver updates, sleep, or display reconnects. The drift-guard re-asserts your preference.",
        Scenarios: Scenarios(
            ("Laptop, wants battery savings", "Enabled"),
            ("Wants a fixed predictable refresh", "Disabled"),
            ("Display without DRR support", "n/a -- the control is hidden"),
            ("Desktop high-refresh gaming", "Personal taste; many leave it off for consistency")),
        Recommended: "Personal preference -- Enabled saves power, Disabled is the most predictable",
        Risks: "Very low -- DRR only engages on supported panels. Verify reflects the path flag, not a guarantee the boost engaged in every app (same honest limitation as VRR).",
        ReversibleVia: "Settings > System > Display > Advanced display > 'Choose a refresh rate' > pick Dynamic / a fixed rate. GamerGuardian toggles the same DisplayConfig flag.");

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

        ["Microsoft.MicrosoftOfficeHub"] = new(
            SettingId: "ai.app:Microsoft.MicrosoftOfficeHub",
            DisplayName: "Microsoft 365 Copilot (launcher app)",
            What: "The standalone 'Microsoft 365 Copilot' Store app -- formerly the 'Office' / 'Microsoft 365' hub launcher (package Microsoft.MicrosoftOfficeHub). Microsoft renamed it and auto-pushed it onto Windows 11 machines in 2025, prompting a wave of 'why is this here' complaints. It's a thin web wrapper that promotes Copilot and the Office suite; it is NOT Word/Excel/PowerPoint themselves.",
            Why: "If you don't use the launcher tile -- and most people open Word/Excel directly -- it's dead weight that re-pins itself to Start and nags about Copilot. Removing it reclaims the tile and the background app.",
            HowItHelps: "Removes the launcher from Start and stops its Copilot promotion. Your actual Office programs keep working.",
            Scenarios: Scenarios(
                ("Open Word/Excel directly, never use the hub", "Remove"),
                ("Use the Microsoft 365 launcher to find docs", "Don't remove"),
                ("Worried Windows Update re-pins it", "Remove + tick Auto-apply")),
            Recommended: "Remove (it does not affect installed Office apps)",
            Risks: "The Microsoft 365 launcher tile disappears. Windows Update / Store may re-provision it after major updates -- the AutoApply tick re-removes it. Reinstall via the Microsoft Store ('Microsoft 365 Copilot').",
            ReversibleVia: "Install 'Microsoft 365 Copilot' from the Microsoft Store."),
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

        ["wisvc"] = SvcRec(
            "wisvc",
            "Windows Insider Service",
            "Backs the Windows Insider Program: preview-build enrollment, flighting configuration, and the diagnostic flow Insider builds use. Idle on a machine not enrolled in the Insider Program.",
            "If you're on the stable channel (the vast majority of users), this service has nothing to do. Disabling removes one more idle background service.",
            "Removes an idle service. No effect on stable Windows.",
            recommended: "Disabled (Manual if you run Insider builds)",
            risks: "If you are an Insider or plan to enroll, leave it on -- with it disabled, the Insider Program settings page won't enroll or flight new builds. Re-enable before joining.",
            reversibleVia: "Set-Service -Name wisvc -StartupType Manual"),

        ["SEMgrSvc"] = SvcRec(
            "SEMgrSvc",
            "Payments and NFC/SE Manager",
            "Manages tap-to-pay and the NFC secure element used for contactless payments.",
            "A gaming desktop almost never has NFC payment hardware, so this service has nothing to manage.",
            "Removes an idle background service on machines without NFC.",
            recommended: "Disabled (if you have no NFC reader on this PC)",
            risks: "If you do use tap-to-pay / NFC on this machine (some laptops), leave it on -- payments and NFC apps will fail without it.",
            reversibleVia: "Set-Service -Name SEMgrSvc -StartupType Manual"),

        ["PhoneSvc"] = SvcRec(
            "PhoneSvc",
            "Phone Service",
            "Manages the telephony/cellular device state for machines with a cellular modem or phone-calling integration.",
            "On a desktop with no cellular hardware this service is idle.",
            "Removes an idle background service on non-cellular machines.",
            recommended: "Disabled (no cellular hardware) / Manual otherwise",
            risks: "If you make calls through Windows or use a cellular modem, leave it on.",
            reversibleVia: "Set-Service -Name PhoneSvc -StartupType Manual"),

        ["stisvc"] = SvcRec(
            "stisvc",
            "Windows Image Acquisition (WIA)",
            "Provides image-acquisition services for scanners and digital still cameras.",
            "If you don't own a scanner or a WIA-class camera, nothing ever calls this service.",
            "Removes an idle service on machines with no imaging hardware.",
            recommended: "Disabled (no scanner/camera) / Manual otherwise",
            risks: "Scanning software and some camera-import flows will fail to acquire images with this disabled. Re-enable before scanning.",
            reversibleVia: "Set-Service -Name stisvc -StartupType Manual"),

        ["WpcMonSvc"] = SvcRec(
            "WpcMonSvc",
            "Parental Controls",
            "Enforces Microsoft Family Safety parental-control restrictions (time limits, content filters).",
            "If you don't have child accounts or Family Safety configured on this PC, the service has nothing to enforce.",
            "Removes an idle service on machines with no parental controls.",
            recommended: "Disabled (no Family Safety on this PC)",
            risks: "If a child account on this PC relies on Family Safety enforcement, do NOT disable -- restrictions would stop applying.",
            reversibleVia: "Set-Service -Name WpcMonSvc -StartupType Manual"),

        ["AssignedAccessManagerSvc"] = SvcRec(
            "AssignedAccessManagerSvc",
            "Kiosk Mode (Assigned Access)",
            "Backs single-app 'kiosk' / assigned-access mode used on shared or public terminals.",
            "A personal gaming PC is not a kiosk, so this service is unused.",
            "Removes an idle service that personal machines never use.",
            recommended: "Disabled",
            risks: "Only relevant if you actually configure Assigned Access / kiosk mode -- rare on a home PC.",
            reversibleVia: "Set-Service -Name AssignedAccessManagerSvc -StartupType Manual"),

        ["TrkWks"] = SvcRec(
            "TrkWks",
            "Distributed Link Tracking Client",
            "Maintains links between NTFS files when their targets move across volumes or a domain (e.g. keeping a shortcut valid after the file moves).",
            "Runs automatically but is rarely exercised on a standalone home PC; most users never notice it being off.",
            "Removes a small always-on background service.",
            recommended: "Disabled (Manual if you rely on shortcut auto-repair across drives)",
            risks: "Shortcuts/links won't auto-repair if their target moves between volumes. Minor and rarely noticed.",
            reversibleVia: "Set-Service -Name TrkWks -StartupType Automatic"),

        ["RemoteAccess"] = SvcRec(
            "RemoteAccess",
            "Routing and Remote Access",
            "Provides LAN/WAN routing and dial-up/VPN server functionality. Disabled by default on client Windows.",
            "Already disabled on a default install -- this entry is a drift-guard so you can confirm nothing silently re-enables it.",
            "No change on a default machine; catches an unexpected re-enable.",
            recommended: "Default (stays Disabled)",
            risks: "If you intentionally run the Windows routing / RRAS VPN server role (rare on a gaming desktop), leave it alone.",
            reversibleVia: "Set-Service -Name RemoteAccess -StartupType Disabled (its default), or Manual if you need it."),
    };

    // ---- Pros & cons of each choice (hand-authored) -----------------------
    //
    // For a user who knows nothing about the setting: one plain upside and one
    // downside of each option, in the same words the Settings window offers.
    // Service and UWP-app rows are synthesized in ProsConsFor() from the catalog
    // entry, so they don't appear here.

    private static readonly Dictionary<string, IReadOnlyList<ChoiceTradeoff>> ProsConsById = new()
    {
        ["gamemode"] = Pc(
            ("On (recommended)", "Slightly lower input latency and no Windows Update reboots mid-game, at zero cost.", "On a few GPU/driver/game combos it can cause stutter or capture glitches."),
            ("Off", "Rules Game Mode out as the cause if you're chasing a specific stutter.", "You give up the small latency win and the mid-game update-reboot suppression.")),

        ["gamedvr"] = Pc(
            ("Off (recommended)", "Frees the GPU's video encoder and removes constant background recording overhead.", "You lose the Win+Alt+G 'save the last 30 seconds' clip shortcut."),
            ("On", "Press Win+Alt+G any time to save a clip of what just happened.", "Constant background encoding costs framerate (more on older GPUs) and ties up the encoder.")),

        ["hags"] = Pc(
            ("On (recommended)", "1-5% more FPS in CPU-bound games, and unlocks DLSS Frame Generation and other GPU-managed features.", "Rare driver instability on first-gen HAGS GPUs; some pro render/ML/emulation apps prefer it off; needs a reboot."),
            ("Off", "Safest for the few professional GPU workloads that prefer driver-side scheduling.", "Leaves per-frame CPU scheduling overhead and disables features (like DLSS Frame Gen) that require GPU scheduling.")),

        ["memintegrity"] = Pc(
            ("On (recommended)", "Keeps kernel-driver tamper protection on, and is required by some anti-cheat (Riot Vanguard).", "Hypervisor transitions cost CPU -- typically 5-15% worse 1% lows in CPU-bound games."),
            ("Off", "Recovers 5-15% framerate (especially 1% lows) in CPU-bound games.", "Weakens kernel-driver malware protection and breaks games whose anti-cheat requires it (Valorant); needs a reboot.")),

        ["vbs"] = Pc(
            ("On (recommended)", "Keeps the full security stack (HVCI, Credential Guard, boot protection) and Valorant/Vanguard working.", "Every VBS service keeps paying the hypervisor cost on kernel calls."),
            ("Off", "Durably recovers 5-15% framerate/1% lows by zeroing every VBS scenario so updates can't silently re-enable them.", "Disables kernel-driver, credential, and boot-time protections at once and breaks Valorant; reboot required, and a UEFI lock can keep it on.")),

        ["sysresponse"] = Pc(
            ("Gaming, 10 (recommended)", "Frees ~10% more CPU for games tagged as multimedia, helping low-core-count CPUs most.", "Effect is small, and the value only takes effect after a reboot."),
            ("Default, 20", "Windows' shipped balance; guaranteed headroom for background tasks.", "Reserves 20% of CPU time away from games during contention.")),

        ["netthrottle"] = Pc(
            ("Disabled (recommended)", "Removes packet pacing that can add micro-stutter to online-game netcode.", "Practically none -- in theory multimedia apps could see slightly less reliable timing on a saturated network."),
            ("Default (on)", "Windows' shipped pacing protects multimedia playback under heavy network load.", "Can introduce small, inconsistent latency in competitive online games.")),

        ["usbsuspend"] = Pc(
            ("Disabled (recommended for desktops)", "Kills first-input lag and random USB audio pops by never suspending idle mice/keyboards/headsets.", "Slightly higher idle power (1-3 W) -- measurably worse battery on a laptop. Reboot to apply."),
            ("Enabled (default)", "Saves power by letting Windows sleep idle USB devices -- the right call on battery.", "First mouse move or keypress after idle can drop or stutter; cheap USB DACs may pop.")),

        ["gamestask"] = Pc(
            ("Gaming, boosted (recommended)", "Gives game threads a stronger claim on CPU and I/O, smoothing frame pacing under load.", "Background tasks are deprioritized a little further (not observable with any CPU headroom)."),
            ("Default", "Windows' shipped balance between games and everything else.", "Game threads get a weaker claim during contention, so 1% lows can suffer on busy systems.")),

        ["mouseaccel"] = Pc(
            ("Off (recommended)", "1:1 mouse-to-cursor movement that matches every competitive FPS's in-game feel.", "The cursor feels slower at low DPI until you bump pointer speed or DPI."),
            ("On (default)", "Acceleration helps cover large/high-res desktops with small movements.", "Breaks 1:1 aim consistency between desktop and in-game.")),

        ["fso"] = Pc(
            ("On (default, recommended)", "Faster alt-tab, working overlays (Discord/NVIDIA), and no display-mode flicker.", "A couple frames of extra latency vs true exclusive fullscreen on some titles."),
            ("Off", "Forces true exclusive fullscreen where supported, shaving 1-2 frames of latency.", "Some games crash or render wrong, some overlays can't draw, and alt-tab is slower.")),

        ["vrr"] = Pc(
            ("On (recommended if you have VRR hardware)", "Smooth, tear-free frame delivery (G-Sync/FreeSync) even in games without their own VRR toggle.", "A few old driver+game combos can flicker -- fixable by turning off in-game V-Sync."),
            ("Off", "Avoids the rare VRR flicker on problem displays.", "Games without a VRR toggle won't get variable refresh, so you're back to tearing or V-Sync latency.")),

        ["powerplan"] = Pc(
            ("High Performance / tuned plan (gaming)", "CPU clocks stay pegged, so there's no ramp-up latency at the start of a CPU burst.", "10-30 W more idle draw, warmer components, more fan noise; worse battery on a laptop."),
            ("Balanced (default)", "Lets modern CPUs' boost algorithms run (often better than a pegged plan) and saves power when idle.", "A few ms of clock-ramp latency at the start of bursts on older CPUs.")),

        ["cpuplan"] = Pc(
            ("Build the optimized plan (recommended)", "A Balanced clone tuned to your CPU -- aggressive boost and the right core-parking -- without High Performance's heat/boost cost.", "For asymmetric dual-CCD X3D it isn't enough alone; it relies on BIOS CPPC=Driver + the AMD V-Cache service the app can't set."),
            ("Keep Balanced / a stock plan", "Zero setup, and fine on most modern CPUs whose own boost is already good.", "Misses the per-CPU parking/boost tuning (notably for X3D chips).")),

        ["powerthrottling"] = Pc(
            ("Disabled (gaming, recommended on desktop)", "All threads stay at full clock -- no surprise downclocking of helper threads a game relies on.", "Higher power/heat; on a laptop on battery it costs real runtime."),
            ("Default", "Throttling saves real battery by clocking down idle/background threads.", "Can clip background/helper threads a game uses, hurting consistency on a desktop.")),

        ["faststartup"] = Pc(
            ("Disabled (gaming, recommended)", "Every shutdown becomes a true cold boot, clearing the stale driver/USB/GPU state that 'a real restart fixes'.", "Boots are slightly slower."),
            ("Default (on)", "Faster boots by restoring a saved kernel session.", "'Shutdown' isn't a clean boot, so driver/peripheral quirks can carry across restarts; also locks the disk for dual-boot.")),

        ["visualfx"] = Pc(
            ("Best performance (gaming)", "Instant window/menu response and a touch less idle GPU compositor work.", "Purely cosmetic loss -- the desktop looks flatter; full effect needs a sign-out."),
            ("Default", "Keeps the Fluent animations and fades many people prefer.", "Animations add a small delay to every window/menu interaction.")),

        ["privacy.advertisingid"] = Pc(
            ("Disabled (recommended)", "Apps can't build a stable cross-session ad profile of you, with no functional downside.", "Ads you see may be less 'relevant' (which is the point)."),
            ("Enabled (default)", "Personalized ads across apps.", "Gives apps a persistent identifier to correlate your activity.")),

        ["privacy.tailoredexp"] = Pc(
            ("Disabled (recommended)", "Windows stops mining your diagnostic data for targeted tips and promos.", "You stop seeing personalized Windows tips and suggestions."),
            ("Enabled (default)", "Personalized tips and recommendations in Start/Settings/lock screen.", "Uses your diagnostic data to target suggestions and promotional content.")),

        ["privacy.cdp"] = Pc(
            ("Disabled (gaming, recommended if unused)", "Stops cross-device discovery/sync background activity, reasserted after updates.", "Handoff, shared clipboard, and nearby-device discovery stop working (Phone Link integrations may be affected)."),
            ("Default (on)", "Cross-device handoff and shared clipboard work.", "Background discovery/sync runs even if you never use those features.")),

        ["privacy.activityhistory"] = Pc(
            ("Disabled (gaming, recommended)", "Stops the activity feed collecting and uploading what you do, reasserted after updates.", "Timeline and cross-device activity resume stop working."),
            ("Default (on)", "Timeline shows recent activities and can resume them across devices.", "Records -- and, when signed in, uploads -- your cross-app activity.")),

        ["privacy.speech"] = Pc(
            ("Disabled (recommended)", "Your voice audio never leaves the machine; offline speech and Voice Access still work.", "Cloud-powered dictation loses accuracy or stops working."),
            ("Enabled", "Most accurate cloud dictation and voice features.", "Sends your voice audio to Microsoft's cloud.")),

        ["privacy.inking"] = Pc(
            ("Disabled (recommended)", "Stops Windows harvesting your handwriting samples and contact names.", "Handwriting recognition and suggestions become less personalized."),
            ("Enabled", "Better personalized handwriting recognition and word suggestions.", "Builds and uploads a personal dictionary from your ink and contacts.")),

        ["network.nagle"] = Pc(
            ("Default (recommended unless you've measured a gain)", "The safe choice -- no risk of making latency or throughput worse.", "You might leave a few ms on the table on the rare setup where disabling helps."),
            ("Disabled", "Sends small game packets immediately, which can lower latency on some setups.", "Contested -- can increase bufferbloat latency or hurt throughput, especially on Wi-Fi/congested links; revert if worse.")),

        ["network.nicpower"] = Pc(
            ("Default (recommended)", "No battery cost and no per-hardware guesswork.", "On a wired desktop you might miss a small win from preventing NIC wake stalls."),
            ("Disabled", "NIC never sleeps, so no wake-from-idle network hitches (best on wired desktops).", "Higher idle power, worse laptop battery, needs a reboot, and many adapters are unaffected either way.")),

        ["debloat.suggestedcontent"] = Pc(
            ("Disabled (recommended)", "No silent promo-app installs and no Start/Settings suggestion cards.", "You stop seeing Microsoft's app/feature suggestions; a feature update may re-enable some (Auto-apply holds it)."),
            ("Enabled (default)", "Microsoft surfaces app and feature suggestions.", "Silently installs promo apps and clutters Start with ad-like cards.")),

        ["debloat.spotlight"] = Pc(
            ("Disabled (recommended)", "Removes the tips/fun-facts/ad overlay from the lock screen.", "If your lock-screen background is Spotlight, also switch it to Picture/Slideshow for a full opt-out."),
            ("Enabled (default)", "Rotating Spotlight images with fun facts and tips.", "Shows ad-like captions and tips on your lock screen.")),

        ["debloat.finishsetup"] = Pc(
            ("Disabled (recommended)", "No more post-update 'finish setting up your device' interruption.", "You won't be prompted to finish optional account/OneDrive setup."),
            ("Enabled (default)", "Windows reminds you to finish optional setup steps.", "A recurring full-screen nag that resurfaces after feature updates.")),

        ["debloat.startrecommend"] = Pc(
            ("Disabled (recommended)", "Quiets Start 'Recommended' suggestions and hides recently opened files (a privacy win on a shared screen).", "Recent files stop appearing in Start/jump lists; on Win11 Home the section can't be fully emptied."),
            ("Enabled (default)", "Quick access to recent files and app/web suggestions in Start.", "Suggestions are often ads, and recent files are visible to anyone at your screen.")),

        ["debloat.explorerads"] = Pc(
            ("Disabled (recommended)", "Removes the OneDrive/Microsoft 365 upsell banners from File Explorer.", "None -- genuine sync-status icons on files are unaffected."),
            ("Enabled (default)", "Shows OneDrive/Office sync prompts in Explorer.", "Advertising inside your file manager.")),

        ["debloat.feedback"] = Pc(
            ("Disabled (recommended)", "Windows stops popping 'rate your experience' dialogs (Feedback Hub still opens manually).", "None -- your telemetry level is unaffected."),
            ("Enabled (default)", "You can answer Microsoft's periodic feedback prompts.", "Interrupting popups, sometimes frequent on fresh installs.")),

        ["debloat.widgets"] = Pc(
            ("Disabled (recommended)", "Stops the Widgets process/MSN feed and removes the taskbar button, freeing idle RAM/CPU/bandwidth.", "The Widgets board and its button disappear (one UAC prompt to apply)."),
            ("Enabled (default)", "One-click weather/news board on the taskbar.", "A background web feed that uses RAM/CPU/bandwidth even if you rarely open it.")),

        ["debloat.edge"] = Pc(
            ("Disabled (recommended)", "Edge stops pre-launching at boot and exits when closed, freeing 150-500 MB of idle RAM.", "Edge cold-starts a little slower (WebView2 apps are unaffected; one UAC prompt)."),
            ("Enabled (default)", "Edge launches instantly and stays warm in the background.", "Keeps Edge resident from boot for no benefit if it isn't your daily browser.")),

        ["ai.copilot"] = Pc(
            ("Off (recommended)", "Removes the taskbar button and Win+C, and stops background Copilot processes and cloud calls.", "You lose quick access to Copilot unless you turn it back on."),
            ("On (default)", "One-click and Win+C access to Windows Copilot.", "Always-present button, background processes, and page/context sent to cloud AI.")),

        ["ai.recall"] = Pc(
            ("Off (recommended)", "Stops Recall snapshotting your screen at the policy level (no NPU/disk cost, smaller privacy surface).", "You lose Recall's 'find what I had open' search; existing snapshots aren't deleted by this toggle."),
            ("On (default on Copilot+ PCs)", "Search your past screen activity with on-device AI.", "Continuous screen capture plus NPU/disk cost, even though it's processed locally.")),

        ["ai.clicktodo"] = Pc(
            ("Off (recommended)", "Hides the Snipping Tool AI actions panel; normal screenshots are unaffected.", "You lose the AI 'summarize/rewrite/search' actions on captures."),
            ("On (default)", "AI actions appear after you take a screenshot.", "Those actions call Microsoft cloud services.")),

        ["ai.edge"] = Pc(
            ("Off (recommended)", "Cleaner Edge UI, no page contents sent to Copilot, and no in-browser AI generation.", "You lose Edge's built-in Copilot sidebar and AI features (normal browsing is unaffected)."),
            ("On (default)", "Edge Copilot sidebar, page-aware help, and in-browser generative AI.", "Persistent Copilot icon and page-context sharing with cloud AI.")),

        ["ai.notepadpaint"] = Pc(
            ("Off (recommended)", "Notepad and Paint behave like the classic apps -- no AI buttons, cloud calls, or opt-in prompts.", "You lose Notepad Rewrite and Paint Cocreator/Image Creator/Generative Erase."),
            ("On (default)", "AI writing and image tools built into Notepad and Paint.", "Bolts cloud AI (and opt-in prompts) onto otherwise simple apps.")),

        ["ai.settingssearch"] = Pc(
            ("Off (recommended)", "The search box returns local files/apps only -- no web/Copilot suggestions or taskbar companion.", "You lose inline web answers in the search box (indexing and search itself are unchanged)."),
            ("On (default)", "Web and Copilot answers suggested as you type in the search box.", "Calls Microsoft web endpoints on your keystrokes; some builds add a floating companion.")),

        ["ai.actions"] = Pc(
            ("Off (recommended)", "Right-click and image menus stop offering AI actions; the menus otherwise work normally.", "You lose the 'rewrite/summarize/search the web for this' shell actions."),
            ("On (default)", "AI actions available from right-click and image context menus.", "Those actions send selected text/images to cloud AI.")),

        ["ai.inputinsights"] = Pc(
            ("Off (recommended)", "Windows stops saving samples of what you type for personalization.", "Typing suggestions get slightly less personalized over time (autocorrect/spell-check unaffected)."),
            ("On (default)", "Personalized typing suggestions that improve as you type.", "The OS saves a per-user model of the text you type.")),

        ["ai.office"] = Pc(
            ("Off (recommended)", "Removes the Copilot ribbon/buttons from Word/Excel/OneNote and opts out of training on your document text.", "If you have a Copilot license you lose the in-app entry points."),
            ("On (default)", "In-app Copilot in Word/Excel/OneNote (with a license).", "Copilot affordances in every document and potential document-text use for training.")),

        ["hdr"] = Pc(
            ("On (recommended for HDR display + HDR content)", "Genuinely better picture in HDR games/movies; monitoring auto-restores it after Windows silently turns it off.", "Some games tone-map badly in HDR, and SDR desktop content can look worse than native SDR."),
            ("Off", "Native SDR is often cleaner for desktop work and SDR-only content.", "HDR games won't use their HDR rendering paths.")),

        ["refresh"] = Pc(
            ("Maximum supported (recommended)", "Lowest input-to-photon latency and smoothest motion; monitoring catches Windows silently dropping it.", "On a laptop on battery a high rate costs real power."),
            ("A lower / fixed rate", "Saves power on high-Hz panels (useful on battery).", "Higher latency and less smooth motion than your panel can do.")),

        ["drr"] = Pc(
            ("Enabled", "Saves power by dropping to a low virtual refresh for static content and boosting to max for scrolling/ink.", "Refresh isn't fixed, which a few gamers find less predictable."),
            ("Disabled", "A fixed, predictable maximum refresh for consistent latency.", "Loses the battery saving DRR gives on static content.")),

        ["resolution"] = Pc(
            ("Don't enforce (recommended)", "Windows can switch resolution freely when you dock/undock or change monitors.", "Without pinning, Windows could occasionally drop you to a lower resolution after a driver update."),
            ("Pin a resolution", "Absolute stability -- the app re-asserts your chosen resolution after drift.", "Fights legitimate display changes (docking a laptop, plugging in a different monitor).")),

        ["ai.app:Microsoft.Copilot"] = Pc(
            ("Remove (after the Copilot policy is Off)", "Reclaims hundreds of MB and removes the Copilot launcher from Start.", "Reinstalling needs the Microsoft Store; Windows Update may re-provision it (Auto-apply re-removes)."),
            ("Keep", "The Copilot app stays one click away.", "Dead weight on disk if you've already blocked Copilot via policy.")),

        ["ai.app:Microsoft.Windows.Ai.Copilot.Provider"] = Pc(
            ("Remove (after the Copilot policy is Off)", "Removes the unused background provider; smaller installed-app surface.", "Re-provisioned by Windows Update; reinstall needs the Store."),
            ("Keep", "Nothing to re-provision later.", "Keeps a provider that does nothing once Copilot is blocked.")),

        ["ai.app:MicrosoftWindows.Client.AIX"] = Pc(
            ("Remove (if you don't use Windows AI)", "Reclaims disk and removes the AI settings panel.", "The AI Settings UI disappears; re-provisioned by Windows Update."),
            ("Keep", "Keeps the AI settings panel and shell AI integrations.", "Unused weight on non-Copilot+ PCs.")),

        ["ai.app:Microsoft.MicrosoftOfficeHub"] = Pc(
            ("Remove", "Removes the Microsoft 365 launcher tile and its Copilot promotion -- your actual Office apps keep working.", "Windows Update/Store may re-provision it (Auto-apply re-removes); reinstall via the Store."),
            ("Keep", "Keeps the hub tile for finding docs.", "Re-pins itself to Start and nags about Copilot if you never use it.")),
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

    // ---- Scheduled tasks (one entry per ScheduledTaskCatalog.TaskPath) ----
    //
    // The Application Experience tasks that drive CompatTelRunner.exe. Keyed by
    // full task path (OrdinalIgnoreCase) so Get("task:<lowercased path>") resolves.
    // SettingId echoes the monitor Id ("task:" + lowercased path).

    private static readonly Dictionary<string, SettingDetails> ScheduledTasks = new(StringComparer.OrdinalIgnoreCase)
    {
        [@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"] = TaskRec(
            @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
            "Microsoft Compatibility Appraiser",
            "A Windows scheduled task that runs CompatTelRunner.exe to scan installed apps, drivers, and files, then writes compatibility + Windows-upgrade-readiness markers and sends telemetry to Microsoft. It's the engine behind the 'Microsoft Compatibility Telemetry' process you see in Task Manager.",
            "The appraiser scan is well documented for spiking CPU and disk to ~100% while it runs (sometimes at startup) -- an unpredictable hitch source mid-game. It runs on a daily trigger whether or not you ever upgrade Windows.",
            "Disabling the task stops the periodic compatibility scan and its CPU/disk spike. Windows Update still works; you only lose pre-update compatibility checks and Win11 upgrade-readiness signals, which don't matter on a gaming PC.",
            "You give up automatic pre-update app-compatibility checks and Windows 11 upgrade-readiness data collection. A Windows feature update may re-enable the task; GamerGuardian's Auto-apply re-disables it. Re-enable any time with the reverse command."),

        [@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser Exp"] = TaskRec(
            @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser Exp",
            "Microsoft Compatibility Appraiser (Exp)",
            "An experimental variant of the Compatibility Appraiser present on some newer Windows 11 builds. It performs the same compatibility/telemetry scan as the main appraiser task.",
            "Same periodic CPU/disk scan cost as the main appraiser. Absent on many builds, in which case there is nothing to disable.",
            "Disabling it stops the experimental appraiser scan. No user-facing functionality is lost. If the task isn't present on your build, GamerGuardian simply reports it as not present.",
            "Same as the main appraiser: only pre-update compatibility data collection is lost, and a feature update may re-enable it (Auto-apply re-disables)."),

        [@"\Microsoft\Windows\Application Experience\ProgramDataUpdater"] = TaskRec(
            @"\Microsoft\Windows\Application Experience\ProgramDataUpdater",
            "ProgramDataUpdater",
            "A scheduled task in the Application Experience pipeline that collects program-inventory data (which apps are installed and used) for compatibility telemetry.",
            "Pure background data collection with no user-facing function -- another contributor to the Application Experience scan load.",
            "Disabling it stops the program-inventory telemetry collection and its background work. Nothing you interact with changes.",
            "Microsoft loses program-inventory telemetry from your machine. A feature update may re-enable the task; Auto-apply re-disables it."),

        [@"\Microsoft\Windows\Application Experience\StartupAppTask"] = TaskRec(
            @"\Microsoft\Windows\Application Experience\StartupAppTask",
            "StartupAppTask",
            "A scheduled task that scans your startup apps for the Application Experience pipeline. Unlike the appraiser tasks, this one has a minor functional role (startup-app impact data), not pure telemetry.",
            "It contributes to the periodic Application Experience scan. The functional cost of disabling it is small: Windows may not refresh startup-impact data shown in Task Manager's Startup tab.",
            "Disabling it stops the periodic startup-app scan. Your startup apps still launch normally; only the background scan and its data refresh stop.",
            "Minor: Windows may show stale or missing 'startup impact' ratings for your startup apps. A feature update may re-enable the task; Auto-apply re-disables it. Disable this one only if you're comfortable with the whole Application Experience folder off."),

        [@"\Microsoft\Windows\Application Experience\PcaPatchDbTask"] = TaskRec(
            @"\Microsoft\Windows\Application Experience\PcaPatchDbTask",
            "PcaPatchDbTask",
            "A scheduled task that updates the Program Compatibility Assistant (PCA) patch database. Like StartupAppTask, it has a minor functional role rather than being pure telemetry.",
            "It contributes to the Application Experience scan load. The functional cost of disabling it is small: the PCA patch database stops refreshing.",
            "Disabling it stops PCA database refreshes and the associated background work. The Program Compatibility Assistant still runs; it just stops pulling new compatibility-shim data.",
            "Minor: the Program Compatibility Assistant may apply fewer automatic app-compatibility shims for old software. A feature update may re-enable the task; Auto-apply re-disables it. Disable this one only if you want the whole Application Experience folder off."),
    };

    private static SettingDetails TaskRec(
        string taskPath, string display, string what, string why, string howItHelps, string risks)
    {
        return new SettingDetails(
            SettingId: $"task:{taskPath.ToLowerInvariant()}",
            DisplayName: display,
            What: what,
            Why: why,
            HowItHelps: howItHelps,
            Scenarios: Scenarios(
                ("Competitive FPS", "Disabled"),
                ("Streaming + game", "Disabled"),
                ("Casual single-player", "Disabled"),
                ("Productivity / mixed-use", "Disabled")),
            Recommended: "Disabled",
            Risks: risks,
            ReversibleVia: $"schtasks /Change /TN \"{taskPath}\" /Enable  (verify: schtasks /Query /TN \"{taskPath}\" /FO LIST)");
    }
}
