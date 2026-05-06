using System.Windows;
using GamerGuardian.Monitors;
using GamerGuardian.Services;
using GamerGuardian.Tray;
using GamerGuardian.UI;
using WpfApplication = System.Windows.Application;

namespace GamerGuardian;

public partial class App : WpfApplication
{
    private static Mutex? _singleInstanceMutex;

    private TrayIconHost? _tray;
    private MonitorService? _monitor;
    private Notifier? _notifier;
    private ConfigStore? _store;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Any(a => a == "--test"))
        {
            RunSelfTest();
            Shutdown();
            return;
        }

        _singleInstanceMutex = new Mutex(initiallyOwned: true, "GamerGuardian.SingleInstance", out bool created);
        if (!created)
        {
            Shutdown();
            return;
        }

        _store = new ConfigStore();
        var cfg = _store.Load();
        StartupRegistration.Sync(cfg.LaunchAtStartup);

        _notifier = new Notifier();
        _monitor = new MonitorService(
            _store,
            new IMonitoredSetting[]
            {
                new HdrMonitor(),
                new RefreshRateMonitor(),
                new ResolutionMonitor(),
                new VrrMonitor(),
                new HagsMonitor(),
                new GameModeMonitor(),
                new GameDvrMonitor(),
                new MousePrecisionMonitor(),
                new FullscreenOptimizationsMonitor(),
                new PowerPlanMonitor(),
            },
            report => _notifier.ShowAsync(report));

        _tray = new TrayIconHost();
        _tray.OpenSettingsRequested += ShowSettings;
        _tray.CheckNowRequested += () => _monitor.TriggerNow();
        _tray.ExitRequested += ExitApp;

        _monitor.Start();

        bool isFirstRun = !System.IO.File.Exists(_store.ConfigPath);
        if (isFirstRun) ShowSettings();
    }

    private void ShowSettings()
    {
        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new SettingsWindow(_store!);
        _settingsWindow.Saved += () => _monitor?.TriggerNow();
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private void ExitApp()
    {
        _tray?.Dispose();
        _monitor?.Dispose();
        Shutdown();
    }

    private static void RunSelfTest()
    {
        var log = new List<string>();
        void Run(string name, Func<string> f)
        {
            try { log.Add($"OK   {name,-44} = {f()}"); }
            catch (Exception ex) { log.Add($"FAIL {name,-44} = {ex.GetType().Name}: {ex.Message}"); }
        }

        Run("displays", () => string.Join(", ",
            GamerGuardian.Native.DisplayHelper.EnumerateActiveDisplays().Select(d => d.DisplayLabel)));

        Run("HagsMonitor.ReadCurrent",
            () => GamerGuardian.Monitors.HagsMonitor.ReadCurrent()?.ToString() ?? "(null)");
        Run("GameModeMonitor.ReadCurrent",
            () => GamerGuardian.Monitors.GameModeMonitor.ReadCurrent()?.ToString() ?? "(null)");
        Run("GameDvrMonitor.ReadCurrent",
            () => GamerGuardian.Monitors.GameDvrMonitor.ReadCurrent()?.ToString() ?? "(null)");
        Run("MousePrecisionMonitor.ReadCurrent",
            () => GamerGuardian.Monitors.MousePrecisionMonitor.ReadCurrent()?.ToString() ?? "(null)");
        Run("FullscreenOptimizationsMonitor.ReadCurrent",
            () => GamerGuardian.Monitors.FullscreenOptimizationsMonitor.ReadCurrent()?.ToString() ?? "(null)");
        Run("VrrMonitor.ReadCurrent",
            () => GamerGuardian.Monitors.VrrMonitor.ReadCurrent()?.ToString() ?? "(null)");
        Run("PowerPlanMonitor.GetActivePlan", () => GamerGuardian.Monitors.PowerPlanMonitor.GetActivePlan().ToString());
        Run("PowerPlanMonitor.ListAvailablePlans",
            () => string.Join(", ", GamerGuardian.Monitors.PowerPlanMonitor.ListAvailablePlans().Values));

        foreach (var d in GamerGuardian.Native.DisplayHelper.EnumerateActiveDisplays())
        {
            Run($"HDR[{d.DisplayLabel}]",
                () => GamerGuardian.Monitors.HdrMonitor.ReadHdrState(d) is { } s ? $"supported={s.Supported} enabled={s.Enabled}" : "(null)");
            Run($"Refresh[{d.DisplayLabel}]",
                () => GamerGuardian.Monitors.RefreshRateMonitor.GetCurrentRefresh(d.GdiDeviceName)?.ToString() ?? "(null)");
            Run($"Resolution[{d.DisplayLabel}]",
                () => GamerGuardian.Monitors.ResolutionMonitor.GetCurrent(d.GdiDeviceName)?.ToString() ?? "(null)");
        }

        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gamerguardian_selftest.txt");
        System.IO.File.WriteAllLines(path, log);
        Environment.ExitCode = log.Any(l => l.StartsWith("FAIL")) ? 1 : 0;
    }
}
