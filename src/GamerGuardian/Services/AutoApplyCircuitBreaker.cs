namespace GamerGuardian.Services;

/// <summary>
/// Stops GamerGuardian from re-applying a setting that Windows (or a Group
/// Policy, or another tool) keeps reverting. Auto-apply on a setting that won't
/// stay put is the worst case for the user: every poll the app re-applies it,
/// and if that apply needs elevation (an HKLM write goes through a UAC
/// secure-desktop prompt) or reconfigures the display (HDR / refresh rate), the
/// screen flashes and the mouse + keyboard are seized every 30 seconds.
///
/// <para>The 15-minute backoff in <see cref="MonitorService"/> only catches the
/// case where an apply <i>fails to verify</i>. This breaker catches the nastier
/// case: the apply <i>verifies</i> each time, but the value is externally reset
/// again before the next poll -- an unbounded re-apply loop with no natural
/// stop. After <see cref="_tripThreshold"/> reverts the breaker "trips" and the
/// setting is skipped for a cooldown window; one retry is allowed after the
/// window, and if it reverts again the breaker re-trips -- so the worst case
/// settles to one interruption per cooldown instead of one every poll.</para>
///
/// <para>Pure, deterministic, and clock-injected (every method takes
/// <c>now</c>), so the whole policy is unit-testable without a timer or the
/// registry.</para>
/// </summary>
public sealed class AutoApplyCircuitBreaker
{
    private readonly int _tripThreshold;
    private readonly TimeSpan _cooldown;

    /// <summary>Consecutive external resets seen for each setting since it last held.</summary>
    private readonly Dictionary<string, int> _resetStreak = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>While now &lt; this time, the setting is tripped (skip auto-apply).</summary>
    private readonly Dictionary<string, DateTimeOffset> _trippedUntil = new(StringComparer.OrdinalIgnoreCase);

    public AutoApplyCircuitBreaker(int tripThreshold = 3, TimeSpan? cooldown = null)
    {
        _tripThreshold = Math.Max(1, tripThreshold);
        _cooldown = cooldown ?? TimeSpan.FromMinutes(15);
    }

    /// <summary>The trip threshold this breaker was constructed with.</summary>
    public int TripThreshold => _tripThreshold;

    /// <summary>The cooldown window opened when a setting trips.</summary>
    public TimeSpan Cooldown => _cooldown;

    /// <summary>How many consecutive times this setting has been reverted (for logging).</summary>
    public int ResetCount(string settingId) => _resetStreak.GetValueOrDefault(settingId);

    /// <summary>Settings the breaker is currently tracking a streak or cooldown for.</summary>
    public IReadOnlyCollection<string> TrackedSettingIds =>
        _resetStreak.Keys.Union(_trippedUntil.Keys, StringComparer.OrdinalIgnoreCase).ToArray();

    /// <summary>
    /// True while the setting is in its post-trip cooldown -- callers must exclude
    /// it from the auto-apply set so it stops spawning UAC prompts / display
    /// reconfigurations every poll.
    /// </summary>
    public bool IsTripped(string settingId, DateTimeOffset now) =>
        _trippedUntil.TryGetValue(settingId, out var until) && now < until;

    /// <summary>
    /// Record that Windows reverted a value the app had previously verified.
    /// Increments the reset streak and, when it reaches the trip threshold (and
    /// the setting isn't already cooling down), opens the cooldown window.
    /// Returns <c>true</c> only on the tick the breaker trips, so the caller can
    /// log it and notify the user exactly once per trip.
    /// </summary>
    public bool RecordExternalReset(string settingId, DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(settingId)) return false;

        // An expired cooldown is the "one retry" the breaker allows; clear it so
        // the streak picks up where it left off and a single fresh revert re-trips.
        if (_trippedUntil.TryGetValue(settingId, out var until) && now >= until)
            _trippedUntil.Remove(settingId);

        if (IsTripped(settingId, now)) return false; // already open -- don't re-trip

        var streak = _resetStreak.GetValueOrDefault(settingId) + 1;
        _resetStreak[settingId] = streak;
        if (streak >= _tripThreshold)
        {
            _trippedUntil[settingId] = now + _cooldown;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Record that the setting is healthy this tick (in sync, nothing reverted).
    /// Clears its streak and any cooldown so a setting that briefly fought Windows
    /// and then settled returns to normal monitoring rather than staying penalized.
    /// </summary>
    public void RecordHealthy(string settingId)
    {
        if (string.IsNullOrEmpty(settingId)) return;
        _resetStreak.Remove(settingId);
        _trippedUntil.Remove(settingId);
    }
}
