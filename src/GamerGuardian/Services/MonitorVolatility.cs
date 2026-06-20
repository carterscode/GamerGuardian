using GamerGuardian.Monitors;

namespace GamerGuardian.Services;

/// <summary>How often a monitored setting needs re-checking.</summary>
public enum MonitorTier
{
    /// <summary>Genuinely changes mid-session -- check on every poll.</summary>
    Volatile,
    /// <summary>Only changes across reboots / feature updates / sleep -- check at
    /// startup, on resume / unlock / display-change events, and on a slow backstop
    /// instead of every poll.</summary>
    Stable,
}

/// <summary>
/// Classifies each monitored setting by how often it actually changes while the
/// machine is running, so <see cref="MonitorService"/> can stop polling ~40
/// set-and-forget registry/policy/service settings every 30 seconds.
///
/// <para>Only <b>display</b> settings (HDR, refresh rate, resolution, DRR) are
/// <see cref="MonitorTier.Volatile"/>: Windows silently resets them after sleep,
/// a monitor hot-plug, or a driver/GPU event, so catching that quickly is the
/// whole point of monitoring them. Everything else -- gaming/AI/privacy/debloat
/// registry policies, services, the power plan -- only moves across a reboot or a
/// feature update, both of which are already covered by the startup check, the
/// resume/unlock events, and the slow backstop poll. Polling them every 30 s did
/// nothing but burn cycles and hand a drifting auto-apply a fresh chance to prompt
/// for UAC each cycle.</para>
/// </summary>
public static class MonitorVolatility
{
    // Keyed on the setting id's base (the part before any ':' suffix), so both the
    // monitor id ("hdr") and a per-display drift id ("hdr:DISPLAY1") classify the same.
    private static readonly HashSet<string> VolatileBaseIds =
        new(StringComparer.OrdinalIgnoreCase) { "hdr", "refresh", "resolution", "drr" };

    public static MonitorTier TierFor(string? settingId) =>
        settingId is not null && VolatileBaseIds.Contains(BaseId(settingId))
            ? MonitorTier.Volatile
            : MonitorTier.Stable;

    public static bool IsVolatile(string? settingId) => TierFor(settingId) == MonitorTier.Volatile;

    public static MonitorTier TierFor(IMonitoredSetting monitor) => TierFor(monitor.Id);

    private static string BaseId(string id)
    {
        var i = id.IndexOf(':');
        return i < 0 ? id : id[..i];
    }
}
