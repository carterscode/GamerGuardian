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
            _store.Save(config);

            var auto = drifted.Where(d => d.AutoApply).ToList();
            if (auto.Count > 0)
            {
                var results = await ChangeApplier.ApplyAndVerifyAsync(auto, _monitors, config);
                ChangeLogger.LogApplyResults(results, "auto");

                var rebootSettings = new List<DriftItem>();
                for (int i = 0; i < auto.Count && i < results.Count; i++)
                {
                    if (results[i].Verified && auto[i].RequiresReboot)
                        rebootSettings.Add(auto[i]);
                }
                if (rebootSettings.Count > 0)
                    AutoAppliedRebootRequired?.Invoke(rebootSettings);
            }

            var prompt = drifted.Where(d => !d.AutoApply).ToList();
            if (prompt.Count > 0)
                await _onDriftAsync(new DriftReport(prompt));

            if (++_ticksSinceTrim >= 10)
            {
                _ticksSinceTrim = 0;
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
