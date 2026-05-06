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

    public bool IsUserPaused => _userPaused;
    public event Action<bool>? PauseChanged;

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
            if (_userPaused) return;
            if (Shell32.IsFullscreenAppActive()) return;
            if (BenchmarkDetector.GetRunningBenchmark() is not null) return;

            var config = _store.Load();
            var drifted = new List<DriftItem>();
            foreach (var m in _monitors)
            {
                try { drifted.AddRange(m.CheckDrift(config)); }
                catch { /* swallow per-monitor failure to keep loop alive */ }
            }
            _store.Save(config);

            var auto = drifted.Where(d => d.AutoApply).ToList();
            foreach (var item in auto)
            {
                try { await item.Apply(); } catch { }
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
