using System.Runtime;
using GamerGuardian.Models;
using GamerGuardian.Monitors;
using GamerGuardian.Native;
using Microsoft.Win32;

namespace GamerGuardian.Services;

public sealed class MonitorService : IDisposable
{
    private readonly ConfigStore _store;
    private readonly IReadOnlyList<IMonitoredSetting> _monitors;
    private readonly Func<DriftReport, Task> _onDriftAsync;

    /// <summary>Fast cadence (the user's poll interval, default 30 s): only the
    /// <see cref="MonitorTier.Volatile"/> settings (display) -- the ones Windows
    /// actually changes mid-session.</summary>
    private readonly System.Threading.Timer _fastTimer;

    /// <summary>Slow backstop: re-checks the ~40 <see cref="MonitorTier.Stable"/>
    /// registry/policy/service settings that otherwise only change across a reboot
    /// or feature update. Catches anything the startup check and the
    /// resume/unlock/display events miss, without polling them every 30 s.</summary>
    private readonly System.Threading.Timer _slowTimer;
    private static readonly TimeSpan StableBackstop = TimeSpan.FromMinutes(10);

    private bool _eventsSubscribed;
    private readonly object _lock = new();
    private bool _running;
    private int _ticksSinceTrim;
    private bool _userPaused;
    private string? _activePauseReason;

    /// <summary>
    /// When an auto-apply fails to verify (Apply ran but the re-read still
    /// shows drift — e.g. Windows immediately reverts a change to a protected
    /// service like DoSvc), back off auto-applying that specific setting for
    /// 15 minutes. Without this we'd spam UAC every 30 s for the same setting.
    /// Per-process state, in-memory only — clears on app restart.
    /// </summary>
    private readonly Dictionary<string, DateTimeOffset> _autoApplyBackoff = new();
    private static readonly TimeSpan AutoApplyBackoffWindow = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Per-setting record of the last value we successfully applied + verified.
    /// When a later tick reports drift on a key in this dict, that drift is by
    /// definition externally caused — we put the value in the desired state and
    /// something else moved it. Drives the EXTRESET log lines and the "this
    /// apply is corrective" tag on the next ApplyResult.
    /// </summary>
    private readonly Dictionary<string, LastVerified> _lastVerified = new();

    /// <summary>
    /// Stops auto-applying a setting Windows keeps reverting. Without it, a value
    /// that verifies-then-reverts every poll re-applies forever -- and every
    /// re-apply that needs elevation throws a UAC secure-desktop prompt (or
    /// reconfigures the display), seizing the mouse + keyboard every 30 s. After a
    /// few reverts the breaker trips and the setting is left to notify-only for a
    /// cooldown. Also tracks the per-setting revert count surfaced in the log.
    /// </summary>
    private readonly AutoApplyCircuitBreaker _breaker = new();

    private readonly record struct LastVerified(string RawValue, string DisplayValue, DateTimeOffset At);

    public bool IsUserPaused => _userPaused;
    public event Action<bool>? PauseChanged;
    public event Action<IReadOnlyList<DriftItem>>? AutoAppliedRebootRequired;

    /// <summary>
    /// The settings currently observed as drifted, keyed by setting id. This is the
    /// same filtered set the scan already computes (monitored settings whose observed
    /// value differs from the desired one), published so the UI can count drift —
    /// per section or in total — without re-running <c>CheckDrift</c>.
    ///
    /// <para>Replaced wholesale on each publish, so the reference a caller holds is
    /// an immutable snapshot and safe to enumerate on any thread.</para>
    /// </summary>
    public IReadOnlyDictionary<string, DriftItem> CurrentDrift => _publishedDrift;

    /// <summary>
    /// Raised after a scan finishes, when the set of drifted setting ids has changed
    /// since the previous publish. Carries the same snapshot as
    /// <see cref="CurrentDrift"/>.
    ///
    /// <para><b>Raised on a background thread</b> (the poll timer), like
    /// <see cref="AutoAppliedRebootRequired"/> — a UI handler must marshal to the
    /// dispatcher before touching controls.</para>
    /// </summary>
    public event Action<IReadOnlyDictionary<string, DriftItem>>? DriftChanged;

    private static readonly IReadOnlyDictionary<string, DriftItem> EmptyDrift =
        new Dictionary<string, DriftItem>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Published snapshot, and also the accumulator across ticks (each
    /// publish replaces it with a freshly built dictionary, so it is never mutated
    /// in place). Single-writer: only <see cref="TickAsync"/> publishes, and the
    /// <see cref="_running"/> guard means ticks never overlap. Volatile so a reader
    /// on another thread sees the newest one.</summary>
    private volatile IReadOnlyDictionary<string, DriftItem> _publishedDrift = EmptyDrift;

    public void SetPaused(bool paused)
    {
        if (_userPaused == paused) return;
        _userPaused = paused;
        PauseChanged?.Invoke(paused);
    }

    public void TogglePaused() => SetPaused(!_userPaused);

    public MonitorService(
        ConfigStore store,
        IReadOnlyList<IMonitoredSetting> monitors,
        Func<DriftReport, Task> onDriftAsync)
    {
        _store = store;
        _monitors = monitors;
        _onDriftAsync = onDriftAsync;
        _fastTimer = new System.Threading.Timer(_ => _ = TickAsync(MonitorTier.Volatile), null, Timeout.Infinite, Timeout.Infinite);
        _slowTimer = new System.Threading.Timer(_ => _ = TickAsync(MonitorTier.Stable), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        var cfg = _store.Load();
        var fastMs = Math.Max(5, cfg.PollIntervalSeconds) * 1000;
        var slowMs = (int)Math.Max(fastMs, StableBackstop.TotalMilliseconds);

        SubscribeSystemEvents();
        _fastTimer.Change(2000, fastMs);     // volatile (display) every poll
        _slowTimer.Change(slowMs, slowMs);   // stable backstop
        ScheduleCheck(tier: null, delayMs: 2000); // one full check at startup
    }

    /// <summary>Forces an immediate full check of every setting (both tiers). Used
    /// by the tray "Check now" and after a Settings Apply/Save.</summary>
    public void TriggerNow() => ScheduleCheck(tier: null, delayMs: 0);

    /// <summary>
    /// Runs a tier (or all settings when <paramref name="tier"/> is null) after a
    /// short delay. The delay lets a triggering system event (resume, unlock,
    /// display reconfigure) settle, and the <see cref="_running"/> guard inside
    /// <see cref="TickAsync"/> coalesces overlapping calls.
    /// </summary>
    private void ScheduleCheck(MonitorTier? tier, int delayMs)
    {
        _ = Task.Run(async () =>
        {
            if (delayMs > 0) await Task.Delay(delayMs);
            await TickAsync(tier);
        });
    }

    // System-event triggers for the stable tier: instead of polling ~40 settings
    // every 30 s, re-check them at the moments Windows might actually have changed
    // them. Resume/unlock re-check everything; a display reconfigure re-checks just
    // the volatile (display) settings.
    private void SubscribeSystemEvents()
    {
        if (_eventsSubscribed) return;
        _eventsSubscribed = true;
        try
        {
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            SystemEvents.SessionSwitch += OnSessionSwitch;
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }
        catch { /* SystemEvents needs a message pump; best-effort */ }
    }

    private void UnsubscribeSystemEvents()
    {
        if (!_eventsSubscribed) return;
        _eventsSubscribed = false;
        try
        {
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        }
        catch { }
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume) ScheduleCheck(tier: null, delayMs: 3000);
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionUnlock) ScheduleCheck(tier: null, delayMs: 1500);
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        // The display topology may have changed (monitor hot-plug / driver) -- a
        // newly attached panel can have different DRR support, so drop the cached
        // support results and let the next check re-probe each display once.
        GamerGuardian.Native.DrrInterop.ClearSupportCache();
        ScheduleCheck(tier: MonitorTier.Volatile, delayMs: 1500);
    }

    /// <summary>
    /// Called by the SettingsWindow's Apply / Save &amp; close path to seed our
    /// in-memory "what we last applied" table with the freshly-applied values.
    /// Without this, the very first background tick after a manual Apply would
    /// misread "no prior verified value" and skip the EXTRESET detection until
    /// the second tick after the eventual revert.
    /// </summary>
    public void RecordVerifiedApplies(IEnumerable<ApplyResult> results)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var r in results)
        {
            if (!r.Verified) continue;
            _lastVerified[r.SettingId] = new LastVerified(r.RawAfter, r.After, now);
        }
    }

    /// <summary>
    /// One monitoring pass. <paramref name="tier"/> selects which settings to check:
    /// <see cref="MonitorTier.Volatile"/> on the fast poll, <see cref="MonitorTier.Stable"/>
    /// on the slow backstop, or <c>null</c> for every setting (startup, resume/unlock,
    /// and the tray "Check now"). The <see cref="_running"/> guard makes overlapping
    /// passes (e.g. a fast tick racing a resume event) coalesce instead of stacking.
    /// </summary>
    private async Task TickAsync(MonitorTier? tier)
    {
        lock (_lock)
        {
            if (_running) return;
            _running = true;
        }
        try
        {
            string? pauseReason = null;
            if (_userPaused)
            {
                pauseReason = "user (manual pause via tray)";
            }
            else if (Shell32.IsFullscreenAppActive())
            {
                var fg = Shell32.GetForegroundProcessName();
                pauseReason = string.IsNullOrEmpty(fg) ? "fullscreen" : $"fullscreen ({fg})";
            }
            else if (BenchmarkDetector.GetRunningBenchmark() is { } bench)
            {
                pauseReason = $"benchmark ({bench})";
            }

            if (pauseReason != _activePauseReason)
            {
                if (pauseReason != null)
                    ChangeLogger.LogPauseEvent("PAUSED", pauseReason);
                else if (_activePauseReason != null)
                    ChangeLogger.LogPauseEvent("RESUMED", $"was: {_activePauseReason}");
                _activePauseReason = pauseReason;
            }

            if (pauseReason != null) return;

            var config = _store.Load();
            // Tier filter: the fast poll only checks volatile (display) settings;
            // the slow backstop only the stable ones; startup/events check all.
            var active = tier is null
                ? _monitors
                : _monitors.Where(m => MonitorVolatility.TierFor(m) == tier.Value);
            var drifted = new List<DriftItem>();
            foreach (var m in active)
            {
                try { drifted.AddRange(m.CheckDrift(config).Where(d => d.IsMonitored)); }
                catch { /* swallow per-monitor failure to keep loop alive */ }
            }
            // NOTE: do NOT _store.Save(config) here. The tick's `config` is a
            // local snapshot loaded at the top of TickAsync; writing it back
            // races with the user's Settings-window Apply: if the user saves
            // a new draft between our Load and Save, our save overwrites the
            // user's just-saved prefs (silently dropping any newly-added
            // properties -- e.g. the v0.1.39 Windows AI prefs). CheckDrift
            // doesn't mutate config anyway, so the save was a no-op except
            // for triggering this race. Removed in v0.1.40.

            var now = DateTimeOffset.UtcNow;
            var driftedIds = new HashSet<string>(drifted.Select(d => d.SettingId), StringComparer.OrdinalIgnoreCase);

            // Anything the breaker is tracking that ISN'T drifting this tick has
            // recovered (Windows stopped reverting it) -- clear its streak/cooldown
            // so it returns to normal monitoring. Only consider settings that were
            // actually checked this tick: on a volatile-only fast poll a stable
            // setting's absence from `drifted` means "not checked", not "recovered".
            foreach (var id in _breaker.TrackedSettingIds.ToList())
            {
                if (tier is not null && MonitorVolatility.TierFor(id) != tier.Value) continue;
                if (!driftedIds.Contains(id)) _breaker.RecordHealthy(id);
            }

            // External-reset detection happens BEFORE auto-apply so we log the
            // cause (EXTRESET) and the effect (the corrective apply) as two
            // separate, easily-correlated lines. When a setting has been reverted
            // enough times the breaker trips: we stop treating it as
            // previously-verified (so it stops logging EXTRESET every poll) and the
            // auto-apply filter below skips it for the cooldown window.
            var externalResetIds = new HashSet<string>();
            foreach (var d in drifted)
            {
                if (!_lastVerified.TryGetValue(d.SettingId, out var prev)) continue;
                // Drift exists on a setting we previously verified-applied. By
                // definition this is an external reset — we set it correctly,
                // something else moved it.
                externalResetIds.Add(d.SettingId);
                bool tripped = _breaker.RecordExternalReset(d.SettingId, now);
                ChangeLogger.LogExternalReset(
                    settingId: d.SettingId,
                    description: d.Description,
                    lastAppliedValue: $"{prev.DisplayValue} ({prev.RawValue})",
                    currentValue: $"{d.CurrentValue} ({d.RawBefore})",
                    lastAppliedAt: prev.At,
                    stickinessCount: _breaker.ResetCount(d.SettingId),
                    autoApplyOn: d.AutoApply && !tripped);
                if (tripped)
                {
                    ChangeLogger.LogCircuitBreaker(
                        d.SettingId, d.Description,
                        _breaker.ResetCount(d.SettingId), _breaker.Cooldown);
                    // Drop the verified record so we stop re-detecting this as an
                    // external reset (and re-logging) every poll while it's tripped.
                    _lastVerified.Remove(d.SettingId);
                    externalResetIds.Remove(d.SettingId);
                }
            }

            var auto = drifted
                .Where(d => d.AutoApply)
                .Where(d => !_breaker.IsTripped(d.SettingId, now))
                .Where(d => !_autoApplyBackoff.TryGetValue(d.SettingId, out var until) || now >= until)
                .ToList();
            // Ids auto-applied and verified this tick. They are no longer drifted, so
            // the published snapshot drops them -- otherwise the count would report
            // drift the app just corrected, for as long as a full tier re-check away
            // (up to the 10-minute stable backstop). Bookkeeping only: nothing below
            // reads this to decide whether to apply.
            var appliedAndVerified = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (auto.Count > 0)
            {
                // Split into corrective (was previously verified, now drifted)
                // vs initial. Each batch gets its own session id so log readers
                // can distinguish "Windows reverted, we restored" from
                // "drift first detected" at a glance.
                var corrective = auto.Where(a => externalResetIds.Contains(a.SettingId)).ToList();
                var initial    = auto.Where(a => !externalResetIds.Contains(a.SettingId)).ToList();

                var allResults = new List<ApplyResult>(auto.Count);

                if (corrective.Count > 0)
                {
                    var sid = ChangeApplier.NewSessionId();
                    var results = await ChangeApplier.ApplyAndVerifyAsync(
                        corrective, _monitors, config, source: "auto-revert", sessionId: sid);
                    // Stamp the corrective-context fields the applier can't know about.
                    for (int i = 0; i < results.Count; i++)
                    {
                        results[i] = results[i] with
                        {
                            ExternalResetDetected = true,
                            StickinessCount = _breaker.ResetCount(results[i].SettingId)
                        };
                    }
                    ChangeLogger.LogApplyResults(results, "auto-revert");
                    allResults.AddRange(results);
                }
                if (initial.Count > 0)
                {
                    var sid = ChangeApplier.NewSessionId();
                    var results = await ChangeApplier.ApplyAndVerifyAsync(
                        initial, _monitors, config, source: "auto", sessionId: sid);
                    ChangeLogger.LogApplyResults(results, "auto");
                    allResults.AddRange(results);
                }

                // Update backoff + last-verified using the combined batch.
                for (int i = 0; i < auto.Count && i < allResults.Count; i++)
                {
                    var driftItem = auto.First(d => d.SettingId == allResults[i].SettingId);
                    if (allResults[i].Verified)
                    {
                        _autoApplyBackoff.Remove(allResults[i].SettingId);
                        _lastVerified[allResults[i].SettingId] = new LastVerified(
                            allResults[i].RawAfter, allResults[i].After, now);
                        appliedAndVerified.Add(allResults[i].SettingId);
                    }
                    else
                    {
                        _autoApplyBackoff[allResults[i].SettingId] = now + AutoApplyBackoffWindow;
                        // Don't update _lastVerified on failure -- next tick's drift
                        // shouldn't look like an external reset if we just couldn't write.
                    }
                    _ = driftItem; // referenced for clarity above
                }

                var rebootSettings = new List<DriftItem>();
                for (int i = 0; i < auto.Count && i < allResults.Count; i++)
                {
                    if (allResults[i].Verified && auto[i].RequiresReboot)
                        rebootSettings.Add(auto[i]);
                }
                if (rebootSettings.Count > 0)
                    AutoAppliedRebootRequired?.Invoke(rebootSettings);
            }

            // Notify only about drift the user asked to be notified about
            // (Monitor on, Auto-apply off). A setting set to auto-apply — "silently
            // change to my desired state" — must NEVER produce a notification, even
            // on a tick where we couldn't apply it (verify-backoff or breaker
            // cooldown). Surfacing a toast for a silent setting is exactly the
            // "silent didn't mean silent" complaint: those failures are logged
            // (LogApplyResults / LogCircuitBreaker) and retried, not shown.
            var prompt = SelectNotifiable(drifted, id => _breaker.IsTripped(id, now));
            if (prompt.Count > 0)
                await _onDriftAsync(new DriftReport(prompt));

            // The scan is finished: publish what is drifting now.
            PublishDrift(tier, drifted, appliedAndVerified);

            if (++_ticksSinceTrim >= 5)
            {
                _ticksSinceTrim = 0;
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(2, GCCollectionMode.Optimized, blocking: false, compacting: true);
                Psapi.TrimSelf();
            }
        }
        finally
        {
            lock (_lock) _running = false;
        }
    }

    /// <summary>
    /// Merges this tick's findings into the accumulated drift set and publishes a
    /// fresh immutable snapshot, raising <see cref="DriftChanged"/> when the set of
    /// drifted ids actually changed.
    ///
    /// <para>The merge is tier-scoped: a tick only re-checks its own tier, so only
    /// that tier's entries are authoritative. On a volatile-only fast poll a stable
    /// setting's absence from <paramref name="drifted"/> means "not checked", not
    /// "no longer drifting" — the same reasoning the circuit-breaker recovery sweep
    /// uses. Clearing everything here would make the count flicker to near-zero on
    /// every 30-second display poll.</para>
    ///
    /// <para>Pure bookkeeping: it observes the scan, it never influences it.</para>
    /// </summary>
    private void PublishDrift(MonitorTier? tier, List<DriftItem> drifted, HashSet<string> appliedAndVerified)
    {
        var previous = _publishedDrift;
        var snapshot = MergeDrift(previous, tier, drifted, appliedAndVerified);

        // A count surface only cares about which ids are drifting, so an unchanged
        // id set raises nothing and the UI doesn't re-render on every quiet poll.
        bool changed = snapshot.Count != previous.Count
                       || !snapshot.Keys.All(previous.ContainsKey);

        _publishedDrift = snapshot;
        if (changed) DriftChanged?.Invoke(snapshot);
    }

    /// <summary>
    /// The merge rule, pure so it can be unit-tested without timers or the registry
    /// (same approach as <see cref="SelectNotifiable"/>). Returns a new dictionary;
    /// neither argument is mutated.
    /// </summary>
    public static Dictionary<string, DriftItem> MergeDrift(
        IReadOnlyDictionary<string, DriftItem> previous,
        MonitorTier? tier,
        IEnumerable<DriftItem> drifted,
        IReadOnlySet<string> appliedAndVerified)
    {
        var merged = new Dictionary<string, DriftItem>(StringComparer.OrdinalIgnoreCase);

        // Carry forward only the tiers this tick did NOT re-check.
        foreach (var (id, item) in previous)
            if (tier is not null && MonitorVolatility.TierFor(id) != tier.Value)
                merged[id] = item;

        foreach (var d in drifted)
            merged[d.SettingId] = d;

        foreach (var id in appliedAndVerified)
            merged.Remove(id);

        return merged;
    }

    /// <summary>
    /// The notification-eligibility rule, pulled out pure so it can be unit-tested
    /// without a timer or the registry. A drifted setting is shown to the user only
    /// when it is NOT set to auto-apply (auto-apply means "silently change", so it
    /// never notifies) and is NOT currently tripped by the circuit breaker (tripped
    /// settings are logged once and would otherwise re-notify every poll).
    /// </summary>
    public static List<DriftItem> SelectNotifiable(
        IEnumerable<DriftItem> drifted, Func<string, bool> isTripped) =>
        drifted
            .Where(d => !d.AutoApply)
            .Where(d => !isTripped(d.SettingId))
            .ToList();

    public void Dispose()
    {
        UnsubscribeSystemEvents();
        _fastTimer.Dispose();
        _slowTimer.Dispose();
    }
}
