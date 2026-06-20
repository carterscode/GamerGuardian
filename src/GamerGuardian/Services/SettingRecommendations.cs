namespace GamerGuardian.Services;

/// <summary>
/// Single source of truth for GamerGuardian's per-setting recommendation,
/// expressed as the <c>DesiredOn</c> value a toggle row should land on. The
/// Settings UI renders this in each row's own vocabulary (the row's
/// <c>OnLabel</c>/<c>OffLabel</c>), so the "Recommended: X" hint always matches
/// the actual Want options on that row -- "Enabled"/"Disabled",
/// "Gaming"/"Default", or "On"/"Off" -- never a generic On/Off. The one-click
/// preset reads the same map for the gaming-core subset it applies, so the hint
/// and the preset can never disagree.
///
/// <para><b>Tuning philosophy -- the typical desktop gamer.</b> Recommendations
/// are the best safe choice for the common case, not the most aggressive
/// possible tweak:
/// <list type="bullet">
///   <item>Clear, broadly-safe performance wins are recommended (Game Mode on,
///     Game DVR off, mouse acceleration off, full clocks, snappy desktop).</item>
///   <item>Security toggles (Memory Integrity / VBS) are recommended to stay
///     <b>ON</b> -- the perf gain from disabling is small and some anti-cheat
///     (e.g. Riot Vanguard) requires Memory Integrity. Only flip them if you
///     understand the trade-off.</item>
///   <item>Genuinely contested, per-hardware tweaks (Nagle's algorithm, NIC
///     power management) are recommended to stay at the Windows <b>Default</b>
///     -- they can make some connections worse.</item>
///   <item>Privacy and debloat settings lean toward the privacy-respecting /
///     clutter-free value, since those carry little to no downside.</item>
/// </list></para>
///
/// <para>Only toggles live here. Services use
/// <see cref="GamerGuardian.Models.ServiceDefinition.RecommendedTarget"/> and the
/// power plan is CPU-aware (see
/// <see cref="GamerGuardian.Models.CpuTuneResult.RecommendedPrebuilt"/>).</para>
/// </summary>
public static class SettingRecommendations
{
    /// <summary>
    /// settingId -> recommended <c>DesiredOn</c>. <c>true</c> selects the row's
    /// <c>OnLabel</c>, <c>false</c> selects its <c>OffLabel</c>. The comment on
    /// each line names the label the user actually sees so this map can be
    /// reviewed without cross-referencing the row construction.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, bool> ToggleDesiredOn =
        new Dictionary<string, bool>
        {
            // ---- Global gaming: clear, broadly-safe performance wins ----
            ["gamemode"]        = true,   // Enabled  -- prioritize the running game
            ["gamedvr"]         = false,  // Disabled -- stop always-on capture overhead
            ["hags"]            = true,   // Enabled  -- lower GPU-scheduling latency on Win11
            ["vrr"]             = true,   // Enabled  -- G-Sync/FreeSync compatibility flag
            ["sysresponse"]     = true,   // Gaming   -- free reserved CPU for games
            ["usbsuspend"]      = true,   // Gaming   -- keep peripherals always responsive
            ["gamestask"]       = true,   // Gaming   -- boosted Games multimedia task profile
            ["mouseaccel"]      = false,  // Disabled -- consistent aim, no acceleration curve
            ["fso"]             = true,   // Enabled  -- modern fullscreen-optimizations path
            ["powerthrottling"] = true,   // Gaming   -- full clocks on a desktop (leave Default on battery)
            ["faststartup"]     = true,   // Gaming   -- true cold boots, fewer stale driver/USB quirks
            ["visualfx"]        = true,   // Gaming   -- snappiest desktop

            // ---- Security: recommend keeping protection ON for a typical gamer ----
            ["memintegrity"]    = true,   // Enabled  -- keep Core Isolation on unless you need the perf
            ["vbs"]             = true,   // Enabled  -- keep the VBS stack on (anti-cheat depends on it)

            // ---- Windows AI: off trims background work (rows use "On"/"Off") ----
            ["ai.copilot"]        = false, // Off
            ["ai.recall"]         = false, // Off
            ["ai.clicktodo"]      = false, // Off
            ["ai.edge"]           = false, // Off
            ["ai.notepadpaint"]   = false, // Off
            ["ai.settingssearch"] = false, // Off
            ["ai.actions"]        = false, // Off
            ["ai.inputinsights"]  = false, // Off
            ["ai.office"]         = false, // Off

            // ---- Privacy: lean privacy-respecting (little downside) ----
            ["privacy.advertisingid"]   = false, // Disabled
            ["privacy.tailoredexp"]     = false, // Disabled
            ["privacy.cdp"]             = true,  // Gaming (disabled via policy)
            ["privacy.activityhistory"] = true,  // Gaming (disabled via policy)
            ["privacy.speech"]          = false, // Disabled (offline recognition still works)
            ["privacy.inking"]          = false, // Disabled

            // ---- Debloat: lean clutter-free (no ads/nags) ----
            ["debloat.suggestedcontent"] = false, // Disabled
            ["debloat.spotlight"]        = false, // Disabled
            ["debloat.finishsetup"]      = false, // Disabled
            ["debloat.startrecommend"]   = false, // Disabled
            ["debloat.explorerads"]      = false, // Disabled
            ["debloat.feedback"]         = false, // Disabled
            ["debloat.widgets"]          = false, // Disabled
            ["debloat.edge"]             = false, // Disabled

            // ---- Network: throttling is a safe win; Nagle/NIC are contested ----
            ["netthrottle"]      = true,   // Gaming  -- steadier online-game netcode, well established
            ["network.nagle"]    = false,  // Default -- contested, per-hardware; can hurt some links
            ["network.nicpower"] = false,  // Default -- contested, per-hardware; matters more on laptops
        };

    /// <summary>
    /// "Extreme" gaming target -- everything that could even remotely help gaming,
    /// turned on. Same as <see cref="ToggleDesiredOn"/> for every clear-win toggle,
    /// but flips the four settings the standard recommendation deliberately softens:
    /// Memory Integrity and the full VBS stack go <b>off</b> (the contested 5-15%
    /// FPS gain), and Nagle's algorithm + NIC power management go to their
    /// <b>aggressive</b> (disabled) state. Used by the "Apply Extreme" preset, which
    /// also turns Monitor + Auto-apply on for every setting.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, bool> ExtremeDesiredOn = BuildExtreme();

    private static Dictionary<string, bool> BuildExtreme()
    {
        var d = new Dictionary<string, bool>(ToggleDesiredOn);
        d["memintegrity"]    = false; // Disabled -- accept the security tradeoff for FPS
        d["vbs"]             = false; // Disabled -- full VBS stack off
        d["network.nagle"]   = true;  // Gaming   -- disable Nagle (send small packets immediately)
        d["network.nicpower"] = true; // Gaming   -- never let the NIC sleep
        return d;
    }

    /// <summary>
    /// The Windows out-of-box <c>DesiredOn</c> for each toggle -- the value "Reset
    /// all to defaults" stages (alongside Monitor = off and Auto-apply = off) so a
    /// subsequent Apply restores the machine to Windows' shipped behavior. For
    /// intuitive Enabled/Disabled features the default is "feature on" (true); for
    /// the inverted Gaming/Default registry knobs the default is the non-gaming
    /// state (false); security toggles default on; opt-in data collection (speech,
    /// inking) defaults off because Windows ships it off until the user accepts.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, bool> WindowsDefaultDesiredOn =
        new Dictionary<string, bool>
        {
            // ---- Global / system (true = the Windows-shipped state) ----
            ["gamemode"]        = true,   // On by default
            ["gamedvr"]         = true,   // Capture on by default
            ["hags"]            = true,   // On by default on Win11
            ["vrr"]             = false,  // VRROptimizeEnable flag not set by default
            ["sysresponse"]     = false,  // Default reservation (20)
            ["netthrottle"]     = false,  // Throttling on by default
            ["usbsuspend"]      = false,  // Windows-managed selective suspend on
            ["gamestask"]       = false,  // Stock Games MMCSS profile
            ["mouseaccel"]      = true,   // Enhance pointer precision on by default
            ["fso"]             = true,   // Fullscreen optimizations on by default
            ["powerthrottling"] = false,  // Throttling on by default
            ["faststartup"]     = false,  // Fast Startup on by default
            ["visualfx"]        = false,  // "Let Windows choose" by default

            // ---- Security: on by default ----
            ["memintegrity"]    = true,
            ["vbs"]             = true,

            // ---- Windows AI: feature on by default ----
            ["ai.copilot"]        = true,
            ["ai.recall"]         = true,
            ["ai.clicktodo"]      = true,
            ["ai.edge"]           = true,
            ["ai.notepadpaint"]   = true,
            ["ai.settingssearch"] = true,
            ["ai.actions"]        = true,
            ["ai.inputinsights"]  = true,
            ["ai.office"]         = true,

            // ---- Privacy ----
            ["privacy.advertisingid"]   = true,  // ad ID on by default
            ["privacy.tailoredexp"]     = true,  // tailored experiences on by default
            ["privacy.cdp"]             = false, // CDP on by default (DesiredOn=true = disabled-by-policy)
            ["privacy.activityhistory"] = false, // Activity feed on by default
            ["privacy.speech"]          = false, // Online speech is opt-in (off until accepted)
            ["privacy.inking"]          = false, // Inking personalization is opt-in (off until accepted)

            // ---- Debloat: the bloat feature is on by default ----
            ["debloat.suggestedcontent"] = true,
            ["debloat.spotlight"]        = true,
            ["debloat.finishsetup"]      = true,
            ["debloat.startrecommend"]   = true,
            ["debloat.explorerads"]      = true,
            ["debloat.feedback"]         = true,
            ["debloat.widgets"]          = true,
            ["debloat.edge"]             = true,

            // ---- Network: contested tweaks default to off (Windows pacing on) ----
            ["network.nagle"]    = false,
            ["network.nicpower"] = false,
        };

    /// <summary>
    /// Recommended <c>DesiredOn</c> for a toggle setting, or <c>null</c> when the
    /// setting has no documented recommendation (the UI hides the hint).
    /// </summary>
    public static bool? ForToggle(string settingId) =>
        settingId is not null && ToggleDesiredOn.TryGetValue(settingId, out var on)
            ? on
            : null;

    /// <summary>
    /// The "Recommended: X" hint for a toggle row, rendered in the row's own
    /// vocabulary -- <paramref name="onLabel"/> when the recommendation is the
    /// On state, <paramref name="offLabel"/> otherwise. Returns an empty string
    /// when the setting has no recommendation (caller hides the hint). Kept
    /// separate from the WPF row so it can be unit-tested without a UI thread.
    /// </summary>
    public static string FormatToggleHint(string settingId, string onLabel, string offLabel)
    {
        var rec = ForToggle(settingId);
        return rec is null ? string.Empty : $"Recommended: {(rec.Value ? onLabel : offLabel)}";
    }
}
