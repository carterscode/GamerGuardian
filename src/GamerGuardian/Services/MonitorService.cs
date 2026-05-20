using System.Runtime;
using GamerGuardian.Models;
using GamerGuardian.Monitors;
using GamerGuardian.Native;

namespace GamerGuardian.Services;

public sealed class MonitorService : IDisposable
{
    private readonly ConfigStore _store;
    private readonly IReadOnlyList<IMonitoredSetting> _monitors;
    private readonly Func<DriftReport, Task> _onDriftAsync;
    private readonly System.Threading.Timer _timer;
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
    /// <summary>How many times Windows has reverted each setting since app start. Logged with each EXTRESET.</summary>
    private readonly Dictionary<string, int> _stickiness = new();

    private readonly record struct LastVerified(string RawValue, string DisplayValue, DateTimeOffset At);

    public bool IsUserPaused => _userPaused;
    public event Action<bool>? PauseChanged;
    public event Action<IReadOnlyList<DriftItem>>? AutoAppliedRebootRequired;

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
        _timer = new System.Threading.Timer(_ => _ = TickAsync(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        var cfg = _store.Load();
        var ms = Math.Max(5, cfg.PollIntervalSeconds) * 1000;
        _timer.Change(2000, ms);
    }

    public void TriggerNow()
    {
        _timer.Change(0, Timeout.Infinite);
        var cfg = _store.Load();
        var ms = Math.Max(5, cfg.PollIntervalSeconds) * 1000;
        _timer.Change(ms, ms);
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

    private async Task TickAsync()
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
            var drifted = new List<DriftItem>();
            foreach (var m in _monitors)
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

            // External-reset detection happens BEFORE auto-apply so we log the
            // cause (EXTRESET) and the effect (the corrective apply) as two
            // separate, easily-correlated lines.
            var externalResetIds = new HashSet<string>();
            foreach (var d in drifted)
            {
                if (!_lastVerified.TryGetValue(d.SettingId, out var prev)) continue;
                // Drift exists on a setting we previously verified-applied. By
                // definition this is an external reset — we set it correctly,
                // something else moved it.
                externalResetIds.Add(d.SettingId);
                _stickiness[d.SettingId] = _stickiness.GetValueOrDefault(d.SettingId) + 1;
                ChangeLogger.LogExternalReset(
                    settingId: d.SettingId,
                    description: d.Description,
                    lastAppliedValue: $"{prev.DisplayValue} ({prev.RawValue})",
                    currentValue: $"{d.CurrentValue} ({d.RawBefore})",
                    lastAppliedAt: prev.At,
                    stickinessCount: _stickiness[d.SettingId],
                    autoApplyOn: d.AutoApply);
            }

            var now = DateTimeOffset.UtcNow;
            var auto = drifted
                .Where(d => d.AutoApply)
                .Where(d => !_autoApplyBackoff.TryGetValue(d.SettingId, out var until) || now >= until)
                .ToList();
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
                            StickinessCount = _stickiness.GetValueOrDefault(results[i].SettingId)
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

            // Drifts that aren't auto-applied (or are in cooldown) still surface
            // as a notification so the user knows something's drifting and can
            // act manually.
            var prompt = drifted.Where(a => !auto.Any(b => b.SettingId == a.SettingId)).ToList();
            if (prompt.Count > 0)
                await _onDriftAsync(new DriftReport(prompt));

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

    public void Dispose() => _timer.Dispose();
}
