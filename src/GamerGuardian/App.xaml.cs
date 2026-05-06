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
            new IMonitoredSetting[] { new HdrMonitor(), new RefreshRateMonitor() },
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
}
