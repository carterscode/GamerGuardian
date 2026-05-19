using System.Reflection;
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
    private IReadOnlyList<IMonitoredSetting>? _allMonitors;

    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
            LogException("unhandled", ex.ExceptionObject as Exception);
        DispatcherUnhandledException += (_, ex) =>
        {
            LogException("dispatcher", ex.Exception);
            ex.Handled = true;
        };

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
        ChangeLogger.LogSessionStart();
        var cfg = _store.Load();
        // Dev builds (local Debug + the dev-build.yml CI artifacts with "-dev" in
        // InformationalVersion) keep their hands off the installed app's Windows-
        // startup entry and never offer to "upgrade" themselves to production.
        if (!IsDevBuild())
        {
            StartupRegistration.Sync(cfg.LaunchAtStartup);
        }
        ThemeService.Apply(cfg.Theme);
        TempCleanup.Run();

        _notifier = new Notifier();
        var fixedMonitors = new IMonitoredSetting[]
        {
            new HdrMonitor(),
            new RefreshRateMonitor(),
            new ResolutionMonitor(),
            new VrrMonitor(),
            new HagsMonitor(),
            new MemoryIntegrityMonitor(),
            new SystemResponsivenessMonitor(),
            new NetworkThrottlingMonitor(),
            new UsbSelectiveSuspendMonitor(),
            new GamesTaskProfileMonitor(),
            new GameModeMonitor(),
            new GameDvrMonitor(),
            new MousePrecisionMonitor(),
            new FullscreenOptimizationsMonitor(),
            new PowerPlanMonitor(),
        };
        var serviceMonitors = GamerGuardian.Services.ServiceCatalog.All
            .Select(d => (IMonitoredSetting)new WindowsServiceMonitor(d));
        _allMonitors = fixedMonitors.Concat(serviceMonitors).ToArray();
        _monitor = new MonitorService(_store, _allMonitors, report => _notifier.ShowAsync(report));
        _monitor.AutoAppliedRebootRequired += items =>
        {
            var descriptions = items.Select(i => i.Description).ToList();
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    var win = new GamerGuardian.UI.RebootPendingWindow(descriptions);
                    win.Closed += (_, _) => ReleaseWindow(win);
                    win.Show();
                }
                catch { }
            });
        };

        _tray = new TrayIconHost();
        _tray.OpenSettingsRequested += ShowSettings;
        _tray.CheckNowRequested += () => _monitor.TriggerNow();
        _tray.PauseToggleRequested += () => _monitor.TogglePaused();
        _tray.ExitRequested += ExitApp;
        _monitor.PauseChanged += paused => _tray?.SetPaused(paused);

        _monitor.Start();

        bool isFirstRun = !System.IO.File.Exists(_store.ConfigPath);
        if (isFirstRun || e.Args.Any(a => a == "--show-settings")) ShowSettings();

        if (cfg.CheckForUpdatesOnStartup && !IsDevBuild())
            _ = Task.Run(async () => await CheckForUpdatesAsync());

        _ = Dispatcher.BeginInvoke(() =>
        {
            GC.Collect();
            GamerGuardian.Native.Psapi.TrimSelf();
        }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            var info = await GamerGuardian.Services.UpdateService.CheckLatestAsync();
            if (info is null) return;
            var cfg = _store?.Load();
            if (cfg is null) return;
            if (cfg.SkippedUpdateVersion == info.Version) return;

            await Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    var win = new GamerGuardian.UI.UpdateAvailableWindow(
                        info,
                        _store!,
                        onAppShouldExit: () =>
                        {
                            _tray?.Dispose();
                            _monitor?.Dispose();
                            Shutdown();
                        });
                    win.Show();
                }
                catch (Exception ex) { LogException("UpdateAvailable", ex); }
            });
        }
        catch (Exception ex) { LogException("UpdateCheck", ex); }
    }

    private void ShowSettings()
    {
        try
        {
            if (_settingsWindow is { IsLoaded: true })
            {
                _settingsWindow.Activate();
                return;
            }
            _settingsWindow = new SettingsWindow(_store!, _allMonitors!, exitApp: ExitApp, monitorService: _monitor);
            _settingsWindow.Saved += () => _monitor?.TriggerNow();
            _settingsWindow.Closed += (_, _) =>
            {
                ReleaseWindow(_settingsWindow);
                _settingsWindow = null;
                _ = Dispatcher.BeginInvoke(() =>
                {
                    System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect(2, GCCollectionMode.Aggressive, blocking: true, compacting: true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    GamerGuardian.Native.Psapi.TrimSelf();
                }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            };
            _settingsWindow.Show();
        }
        catch (Exception ex)
        {
            LogException("ShowSettings", ex);
        }
    }

    private void ExitApp()
    {
        _tray?.Dispose();
        _monitor?.Dispose();
        Shutdown();
    }

    /// <summary>
    /// True for any "this isn't a production build" flavor:
    ///  - Compile-time Debug builds (#if DEBUG)
    ///  - CI dev-builds whose InformationalVersion is stamped "{base}-dev.{sha}"
    ///    by .github/workflows/dev-build.yml
    /// Dev builds skip auto-update and skip Windows-startup registration so they
    /// don't interfere with the installed production app.
    /// </summary>
    public static bool IsDevBuild()
    {
#if DEBUG
        return true;
#else
        var info = typeof(App).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        return info.Contains("-dev", StringComparison.OrdinalIgnoreCase)
            || info.Contains("-beta", StringComparison.OrdinalIgnoreCase);
#endif
    }

    /// <summary>
    /// Frees the visual tree associated with a Window so the heap can reclaim it.
    /// WPF won't release Content/DataContext refs on its own after Close.
    /// </summary>
    private static void ReleaseWindow(System.Windows.Window? window)
    {
        if (window is null) return;
        try
        {
            window.Content = null;
            window.DataContext = null;
        }
        catch { }
    }

    private static void LogException(string source, Exception? ex)
    {
        try
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gamerguardian_error.log");
            // Cap at ~1 MB by rotating to .1
            try
            {
                var fi = new System.IO.FileInfo(path);
                if (fi.Exists && fi.Length > 1_000_000)
                {
                    var prev = path + ".1";
                    if (System.IO.File.Exists(prev)) System.IO.File.Delete(prev);
                    System.IO.File.Move(path, prev);
                }
            }
            catch { }
            System.IO.File.AppendAllText(path,
                $"[{DateTime.Now:s}] {source}: {ex?.GetType().FullName}: {ex?.Message}\n{ex?.StackTrace}\n\n");
        }
        catch { }
    }

    private static void RunSelfTest()
    {
        var log = new List<string>();
        void Run(string name, Func<string> f)
        {
            try { log.Add($"OK   {name,-44} = {f()}"); }
            catch (Exception ex) { log.Add($"FAIL {name,-44} = {ex.GetType().Name}: {ex.Message}"); }
        }

        var asm = typeof(App).Assembly;
        Run("Assembly.GetName().Version", () => asm.GetName().Version?.ToString() ?? "(null)");
        Run("AssemblyInformationalVersion",
            () => asm.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "(null)");
        Run("AssemblyFileVersion",
            () => asm.GetCustomAttribute<System.Reflection.AssemblyFileVersionAttribute>()?.Version ?? "(null)");
        Run("AssemblyVersion",
            () => asm.GetCustomAttribute<System.Reflection.AssemblyVersionAttribute>()?.Version ?? "(null)");
        Run("FileVersionInfo.ProductVersion", () =>
        {
            var p = Environment.ProcessPath;
            return string.IsNullOrEmpty(p) ? "(no path)"
                : System.Diagnostics.FileVersionInfo.GetVersionInfo(p).ProductVersion ?? "(null)";
        });
        Run("FileVersionInfo.FileVersion", () =>
        {
            var p = Environment.ProcessPath;
            return string.IsNullOrEmpty(p) ? "(no path)"
                : System.Diagnostics.FileVersionInfo.GetVersionInfo(p).FileVersion ?? "(null)";
        });

        Run("displays", () => string.Join(", ",
            GamerGuardian.Native.DisplayHelper.EnumerateActiveDisplays().Select(d => d.DisplayLabel)));
        Run("Shell32.IsFullscreenAppActive",
            () => GamerGuardian.Native.Shell32.IsFullscreenAppActive().ToString());
        Run("BenchmarkDetector.GetRunningBenchmark",
            () => GamerGuardian.Services.BenchmarkDetector.GetRunningBenchmark() ?? "(none)");

        Run("HagsMonitor.ReadCurrent",
            () => GamerGuardian.Monitors.HagsMonitor.ReadCurrent()?.ToString() ?? "(null)");
        Run("MemoryIntegrityMonitor.ReadCurrent",
            () => GamerGuardian.Monitors.MemoryIntegrityMonitor.ReadCurrent()?.ToString() ?? "(null)");
        Run("SystemResponsivenessMonitor.ReadCurrent",
            () => GamerGuardian.Monitors.SystemResponsivenessMonitor.ReadCurrent()?.ToString() ?? "(null)");
        Run("NetworkThrottlingMonitor.ReadCurrent",
            () => GamerGuardian.Monitors.NetworkThrottlingMonitor.ReadCurrent()?.ToString() ?? "(null)");
        Run("UsbSelectiveSuspendMonitor.ReadCurrent",
            () => GamerGuardian.Monitors.UsbSelectiveSuspendMonitor.ReadCurrent()?.ToString() ?? "(null)");
        Run("GamesTaskProfileMonitor.ReadCurrent",
            () => GamerGuardian.Monitors.GamesTaskProfileMonitor.ReadCurrent()?.ToString() ?? "(null)");
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
