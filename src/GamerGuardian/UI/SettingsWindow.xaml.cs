using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using GamerGuardian.Models;
using GamerGuardian.Monitors;
using GamerGuardian.Native;
using GamerGuardian.Services;
using Wpf.Ui.Controls;

namespace GamerGuardian.UI;

public partial class SettingsWindow : FluentWindow
{
    private readonly ConfigStore _store;
    private readonly AppConfig _config;
    private readonly IReadOnlyList<IMonitoredSetting> _monitors;
    private readonly Action _exitApp;
    public ObservableCollection<DisplayRow> DisplayRows { get; } = new();
    public ObservableCollection<GlobalToggleRow> GlobalToggleRows { get; } = new();
    public ObservableCollection<ServiceRow> ServiceRows { get; } = new();
    private bool _suppressPresetEvents;

    public event Action? Saved;

    public SettingsWindow(ConfigStore store, IReadOnlyList<IMonitoredSetting> monitors, Action exitApp)
    {
        InitializeComponent();
        _store = store;
        _monitors = monitors;
        _exitApp = exitApp;
        _config = store.Load();

        LaunchAtStartupCheck.IsChecked = _config.LaunchAtStartup;
        ConsolidateCheck.IsChecked = _config.ConsolidateNotifications;
        CheckForUpdatesCheck.IsChecked = _config.CheckForUpdatesOnStartup;
        PollSecondsBox.Value = _config.PollIntervalSeconds;

        ThemeCombo.ItemsSource = Enum.GetValues<AppThemeChoice>();
        ThemeCombo.SelectedItem = _config.Theme;

        VersionLink.Content = GetVersionDisplay();
        VersionLink.ToolTip = GetVersionTooltip();

        DisplaysList.ItemsSource = DisplayRows;
        GlobalTogglesList.ItemsSource = GlobalToggleRows;
        ServicesList.ItemsSource = ServiceRows;

        LoadGlobals();
        LoadDisplays();
        LoadServices();
    }

    private void LoadServices()
    {
        ServiceRows.Clear();
        foreach (var def in ServiceCatalog.All)
        {
            if (!_config.Services.TryGetValue(def.Name, out var pref) || pref is null)
            {
                pref = new ServicePref();
                _config.Services[def.Name] = pref;
            }

            var installed = WindowsServiceController.Exists(def.Name);
            var current = installed
                ? WindowsServiceController.ReadStartType(def.Name)
                : ServiceStartType.Unknown;

            // For services the user hasn't opted into monitoring, mirror current state
            // into Want so the radio doesn't lie about a "recommendation" the user never asked for.
            if (!pref.Monitor && installed && current != ServiceStartType.Unknown)
            {
                pref.Desired = current switch
                {
                    ServiceStartType.Disabled => ServiceTargetState.Disabled,
                    ServiceStartType.Manual when def.DefaultStartType != ServiceStartType.Manual
                        => ServiceTargetState.Manual,
                    _ => ServiceTargetState.Default,
                };
            }

            var status = installed
                ? WindowsServiceController.ReadStatus(def.Name)
                : null;
            var statusSuffix = status is null ? "" : $", {status.Value}";
            var currentText = installed
                ? $"Current: {WindowsServiceMonitor.DescribeStart(current)}{statusSuffix}"
                : "Current: not installed on this system";
            var defaultText = $"Default: {WindowsServiceMonitor.DescribeStart(def.DefaultStartType)}";

            ServiceRows.Add(new ServiceRow(
                def: def,
                pref: pref,
                isInstalled: installed,
                currentText: currentText,
                defaultText: defaultText,
                onPrefChanged: OnRowPrefChanged));
        }

        UpdatePresetRadio();
    }

    private void UpdatePresetRadio()
    {
        // Default preset = every installed row at Default.
        // Gaming preset = every installed row with a RecommendedTarget at that target.
        //   Non-recommended rows can be anywhere — the preset doesn't manage them.
        // The two are mutually exclusive in practice (recommended targets aren't Default).
        bool matchesDefault = ServiceRows.All(r =>
            !r.IsInstalled || r.DesiredDefault);
        bool matchesGaming = !matchesDefault && ServiceRows
            .Where(r => r.IsInstalled && r.Definition.RecommendedTarget.HasValue)
            .All(r => GetDesired(r) == r.Definition.RecommendedTarget!.Value);

        _suppressPresetEvents = true;
        try
        {
            ServicesPresetGaming.IsChecked = matchesGaming;
            ServicesPresetDefault.IsChecked = matchesDefault;
        }
        finally { _suppressPresetEvents = false; }
    }

    private static ServiceTargetState GetDesired(ServiceRow r) =>
        r.DesiredDisabled ? ServiceTargetState.Disabled
        : r.DesiredManual ? ServiceTargetState.Manual
        : ServiceTargetState.Default;

    private void ServicesPresetGaming_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressPresetEvents) return;
        ApplyServicesPreset(useRecommended: true);
    }

    private void ServicesPresetDefault_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressPresetEvents) return;
        ApplyServicesPreset(useRecommended: false);
    }

    private void ApplyServicesPreset(bool useRecommended)
    {
        foreach (var row in ServiceRows.Where(r => r.IsInstalled))
        {
            if (useRecommended)
            {
                // Only flip rows with a RecommendedTarget. Leave others alone.
                if (row.Definition.RecommendedTarget is { } target)
                    row.SetDesiredFromPreset(target);
            }
            else
            {
                row.SetDesiredFromPreset(ServiceTargetState.Default);
            }
        }
        try { _store.Save(_config); } catch { }
        ChangeLogger.LogPreferenceChange(
            "Windows services preset",
            "Want",
            "(custom)",
            useRecommended ? "Gaming optimized" : "Default");
    }

    private static string OnOffText(bool? state) =>
        state is null ? "not detected" : (state.Value ? "Enabled" : "Disabled");

    private static string GamingDefaultText(bool? state) =>
        state is null ? "not detected" : (state.Value ? "Gaming-optimized" : "Default");

    private static void SyncIfUnmonitored(ToggleSettingPref pref, Func<bool?> readCurrent)
    {
        if (pref.Monitor) return;
        bool? cur;
        try { cur = readCurrent(); } catch { return; }
        if (cur.HasValue) pref.DesiredOn = cur.Value;
    }

    private void LoadGlobals()
    {
        GlobalToggleRows.Clear();
        var g = _config.Global;

        // For settings the user hasn't opted into monitoring, default Want to Current
        // so the radios reflect the actual system state instead of a "ghost" recommendation.
        SyncIfUnmonitored(g.GameMode, GameModeMonitor.ReadCurrent);
        SyncIfUnmonitored(g.GameDvr, GameDvrMonitor.ReadCurrent);
        SyncIfUnmonitored(g.Hags, HagsMonitor.ReadCurrent);
        SyncIfUnmonitored(g.MemoryIntegrity, MemoryIntegrityMonitor.ReadCurrent);
        SyncIfUnmonitored(g.SystemResponsiveness, SystemResponsivenessMonitor.ReadCurrent);
        SyncIfUnmonitored(g.NetworkThrottling, NetworkThrottlingMonitor.ReadCurrent);
        SyncIfUnmonitored(g.UsbSelectiveSuspend, UsbSelectiveSuspendMonitor.ReadCurrent);
        SyncIfUnmonitored(g.GamesTaskProfile, GamesTaskProfileMonitor.ReadCurrent);
        SyncIfUnmonitored(g.MousePrecision, MousePrecisionMonitor.ReadCurrent);
        SyncIfUnmonitored(g.FullscreenOptimizations, FullscreenOptimizationsMonitor.ReadCurrent);
        SyncIfUnmonitored(g.Vrr, VrrMonitor.ReadCurrent);

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Game Mode",
            description: "Tells Windows to prioritize the running game and suppress background work.",
            currentText: $"Current: {OnOffText(SafeRead(GameModeMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.GameMode, groupName: "gm",
            onPrefChanged: OnRowPrefChanged));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Game DVR background recording",
            description: "Always-on game capture. Costs CPU/GPU during gameplay; off is gaming-recommended.",
            currentText: $"Current: {OnOffText(SafeRead(GameDvrMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.GameDvr, groupName: "dvr",
            onPrefChanged: OnRowPrefChanged));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Hardware-accelerated GPU Scheduling (HAGS)",
            description: "Lets the GPU manage its own command queue. Lower latency on supported GPUs.",
            currentText: $"Current: {OnOffText(SafeRead(HagsMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled (Win11)",
            onLabel: "Enabled", offLabel: "Disabled",
            requiresReboot: true,
            pref: g.Hags, groupName: "hags",
            onPrefChanged: OnRowPrefChanged));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Memory Integrity / VBS (Core Isolation)",
            description: "Hypervisor-Enforced Code Integrity. Disabling recovers ~5–15% gaming perf but reduces malware protection.",
            currentText: $"Current: {OnOffText(SafeRead(MemoryIntegrityMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled (Win11)",
            onLabel: "Enabled", offLabel: "Disabled",
            requiresReboot: true,
            pref: g.MemoryIntegrity, groupName: "memint",
            onPrefChanged: OnRowPrefChanged));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "System Responsiveness",
            description: "CPU percentage Windows reserves for non-multimedia tasks. Lower frees CPU for games.",
            currentText: $"Current: {GamingDefaultText(SafeRead(SystemResponsivenessMonitor.ReadCurrent))}",
            defaultText: "Default: 20    Gaming: 10",
            onLabel: "Gaming", offLabel: "Default",
            requiresReboot: true,
            pref: g.SystemResponsiveness, groupName: "sysresp",
            onPrefChanged: OnRowPrefChanged));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Network Throttling",
            description: "Multimedia packet pacing. Disabling reduces network jitter for online games.",
            currentText: $"Current: {GamingDefaultText(SafeRead(NetworkThrottlingMonitor.ReadCurrent))}",
            defaultText: "Default: 10    Gaming: Disabled",
            onLabel: "Gaming", offLabel: "Default",
            pref: g.NetworkThrottling, groupName: "netthr",
            onPrefChanged: OnRowPrefChanged));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "USB Selective Suspend (global)",
            description: "Lets Windows suspend idle USB devices. Disabling keeps mice/keyboards/headsets always responsive.",
            currentText: $"Current: {GamingDefaultText(SafeRead(UsbSelectiveSuspendMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled    Gaming: Disabled",
            onLabel: "Gaming", offLabel: "Default",
            requiresReboot: true,
            pref: g.UsbSelectiveSuspend, groupName: "usbsus",
            onPrefChanged: OnRowPrefChanged));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Games multimedia task profile",
            description: "Priority + scheduling values for processes registered with the Games multimedia class.",
            currentText: $"Current: {GamingDefaultText(SafeRead(GamesTaskProfileMonitor.ReadCurrent))}",
            defaultText: "Default: standard    Gaming: boosted",
            onLabel: "Gaming", offLabel: "Default",
            pref: g.GamesTaskProfile, groupName: "gtask",
            onPrefChanged: OnRowPrefChanged));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Mouse \"Enhance pointer precision\"",
            description: "Acceleration curve applied to mouse movement. Most gamers want this off for consistent aim.",
            currentText: $"Current: {OnOffText(SafeRead(MousePrecisionMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.MousePrecision, groupName: "mp",
            onPrefChanged: OnRowPrefChanged));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Fullscreen optimizations (global)",
            description: "Borderless-windowed compositing layer. Generally fine; some titles prefer it off.",
            currentText: $"Current: {OnOffText(SafeRead(FullscreenOptimizationsMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.FullscreenOptimizations, groupName: "fso",
            onPrefChanged: OnRowPrefChanged));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Variable Refresh Rate (DirectX)",
            description: "G-Sync / FreeSync compatibility flag (Settings → Display → Graphics). Not the same as Dynamic Refresh Rate (DRR) in Advanced Display.",
            currentText: $"Current: {OnOffText(SafeRead(VrrMonitor.ReadCurrent))}",
            defaultText: "Default: not set",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.Vrr, groupName: "vrr",
            onPrefChanged: OnRowPrefChanged));

        var planNames = PowerPlanMonitor.ListAvailablePlans();
        var active = SafeRunGuid(PowerPlanMonitor.GetActivePlan);
        var activeName = active is not null && planNames.TryGetValue(active.Value, out var name) ? name : "unknown";
        PowerPlanCurrentText.Text = $"Current: {activeName}";
        PowerPlanMonitorCheck.IsChecked = g.PowerPlan.Monitor;
        PowerPlanAutoApplyCheck.IsChecked = g.PowerPlan.AutoApply;

        var planItems = planNames
            .OrderBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new PowerPlanItem(kv.Key, kv.Value))
            .ToList();
        PowerPlanCombo.ItemsSource = planItems;
        PowerPlanCombo.DisplayMemberPath = nameof(PowerPlanItem.Name);

        var savedGuid = PowerPlanMonitor.ResolveDesiredGuid(g.PowerPlan);
        PowerPlanCombo.SelectedItem = planItems.FirstOrDefault(p => p.Guid == savedGuid)
            ?? (active is Guid a ? planItems.FirstOrDefault(p => p.Guid == a) : null)
            ?? planItems.FirstOrDefault();
    }

    private static bool? SafeRead(Func<bool?> f)
    {
        try { return f(); } catch { return null; }
    }

    private static Guid? SafeRunGuid(Func<Guid> f)
    {
        try { var g = f(); return g == Guid.Empty ? null : g; } catch { return null; }
    }

    private void LoadDisplays()
    {
        DisplayRows.Clear();
        foreach (var d in DisplayHelper.EnumerateActiveDisplays())
        {
            if (!_config.Displays.TryGetValue(d.StableKey, out var pref))
            {
                pref = new DisplayPreference { DisplayLabel = d.DisplayLabel };
                _config.Displays[d.StableKey] = pref;
            }
            var hdr = SafeRead(() => HdrMonitor.ReadHdrState(d) is { } s ? (bool?)(s.Supported && s.Enabled) : null);
            var refresh = string.IsNullOrEmpty(d.GdiDeviceName) ? null : RefreshRateMonitor.GetCurrentRefresh(d.GdiDeviceName);
            uint maxHz = refresh is null ? 0 : RefreshRateMonitor.GetMaxSupportedRefresh(d.GdiDeviceName, refresh.Value.Width, refresh.Value.Height);
            var rates = refresh is null ? Array.Empty<uint>() : RefreshRateMonitor.GetSupportedRefreshRates(d.GdiDeviceName, refresh.Value.Width, refresh.Value.Height);
            var resolutions = string.IsNullOrEmpty(d.GdiDeviceName) ? Array.Empty<(uint, uint)>() : ResolutionMonitor.ListSupported(d.GdiDeviceName);
            var resStrings = resolutions.Select(r => $"{r.Item1}x{r.Item2}").ToList();
            var current = ResolutionMonitor.GetCurrent(d.GdiDeviceName);

            var status = string.Format(CultureInfo.InvariantCulture,
                "Now — HDR: {0}    Refresh: {1}    Resolution: {2}",
                hdr is null ? "unknown" : (hdr.Value ? "On" : "Off"),
                refresh is null ? "unknown" : refresh.Value.Hz + " Hz" + (maxHz > 0 ? $" (max {maxHz})" : ""),
                current is null ? "unknown" : $"{current.Value.Width}x{current.Value.Height}");

            DisplayRows.Add(new DisplayRow(d.StableKey, d.DisplayLabel, status, pref, rates, resStrings));
        }
    }

    private static string GetVersionDisplay()
    {
        var v = GetSemverString();
#if DEBUG
        return $"v{v} (dev)";
#else
        return $"v{v}";
#endif
    }

    private static string GetSemverString()
    {
        var asm = typeof(App).Assembly;
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(info))
        {
            var idx = info.IndexOf('+');
            return idx > 0 ? info[..idx] : info;
        }
        var v = asm.GetName().Version;
        return v != null ? $"{v.Major}.{v.Minor}.{v.Build}" : "0.0.0";
    }

    private static string GetVersionTooltip()
    {
        var asm = typeof(App).Assembly;
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "(unknown)";
        var fileV = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "(unknown)";
        var rt = Environment.Version.ToString();
#if DEBUG
        var build = "Debug";
#else
        var build = "Release";
#endif
        return $"Informational: {info}\nFile: {fileV}\n.NET: {rt}\nBuild: {build}\n\nClick to open releases page";
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeCombo.SelectedItem is AppThemeChoice c)
            ThemeService.Apply(c);
    }

    private void PowerPlanCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PowerPlanCombo.SelectedItem is not PowerPlanItem pi) return;
        var oldGuid = _config.Global.PowerPlan.DesiredGuid;
        var oldName = _config.Global.PowerPlan.DesiredName;
        _config.Global.PowerPlan.DesiredGuid = pi.Guid.ToString();
        _config.Global.PowerPlan.DesiredName = pi.Name;
        try { _store.Save(_config); } catch { }
        if (oldGuid != pi.Guid.ToString())
            ChangeLogger.LogPreferenceChange("Power plan", "Want",
                oldName ?? oldGuid ?? "(unset)", pi.Name);
    }

    private void PowerPlanMonitorCheck_Changed(object sender, RoutedEventArgs e)
    {
        var v = PowerPlanMonitorCheck.IsChecked == true;
        if (_config.Global.PowerPlan.Monitor == v) return;
        var before = _config.Global.PowerPlan.Monitor;
        _config.Global.PowerPlan.Monitor = v;
        try { _store.Save(_config); } catch { }
        ChangeLogger.LogPreferenceChange("Power plan", "Monitor", before.ToString(), v.ToString());
    }

    private void PowerPlanAutoApplyCheck_Changed(object sender, RoutedEventArgs e)
    {
        var v = PowerPlanAutoApplyCheck.IsChecked == true;
        if (_config.Global.PowerPlan.AutoApply == v) return;
        var before = _config.Global.PowerPlan.AutoApply;
        _config.Global.PowerPlan.AutoApply = v;
        try { _store.Save(_config); } catch { }
        ChangeLogger.LogPreferenceChange("Power plan", "AutoApply", before.ToString(), v.ToString());
    }

    private void OpenChangeLogButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = ChangeLogger.LogPath;
            if (!System.IO.File.Exists(path))
            {
                var dir = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(path, "(no changes have been applied yet)\n");
            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
        catch { }
    }

    private async void CheckUpdatesNowButton_Click(object sender, RoutedEventArgs e)
    {
        var btn = CheckUpdatesNowButton;
        var prev = btn.Content;
        btn.IsEnabled = false;
        btn.Content = "Checking…";
        try
        {
            var info = await UpdateService.CheckLatestAsync();
            if (info is null)
            {
                System.Windows.MessageBox.Show(
                    $"You're on the latest version (v{UpdateService.CurrentSemver()}).",
                    "GamerGuardian",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            else
            {
                var win = new UpdateAvailableWindow(info, _store, _exitApp);
                win.Show();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Couldn't reach the update server.\n\n{ex.Message}",
                "GamerGuardian",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
        finally
        {
            btn.Content = prev;
            btn.IsEnabled = true;
        }
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        await ApplyChangesAsync(closeAfter: false);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        await ApplyChangesAsync(closeAfter: true);
    }

    private async Task ApplyChangesAsync(bool closeAfter)
    {
        PersistFormToConfig();
        _store.Save(_config);
        StartupRegistration.Sync(_config.LaunchAtStartup);

        var drifted = new List<DriftItem>();
        foreach (var m in _monitors)
        {
            try { drifted.AddRange(m.CheckDrift(_config)); }
            catch { /* per-monitor failures shouldn't break Apply */ }
        }

        var results = await ChangeApplier.ApplyAndVerifyAsync(drifted, _monitors, _config);

        if (results.Count > 0)
        {
            ChangeLogger.LogApplyResults(results, "manual");
        }

        Saved?.Invoke();

        LoadGlobals();
        LoadDisplays();
        LoadServices();

        if (results.Count > 0)
        {
            var win = new ApplyResultsWindow(results) { Owner = this };
            win.Show();
        }

        if (closeAfter) Close();
    }

    private void PersistFormToConfig()
    {
        _config.LaunchAtStartup = LaunchAtStartupCheck.IsChecked == true;
        _config.ConsolidateNotifications = ConsolidateCheck.IsChecked == true;
        _config.CheckForUpdatesOnStartup = CheckForUpdatesCheck.IsChecked == true;
        if (PollSecondsBox.Value is double pv && pv >= 5)
            _config.PollIntervalSeconds = (int)pv;
        if (ThemeCombo.SelectedItem is AppThemeChoice tc)
            _config.Theme = tc;

        _config.Global.PowerPlan.Monitor = PowerPlanMonitorCheck.IsChecked == true;
        _config.Global.PowerPlan.AutoApply = PowerPlanAutoApplyCheck.IsChecked == true;
        if (PowerPlanCombo.SelectedItem is PowerPlanItem pi)
        {
            _config.Global.PowerPlan.DesiredGuid = pi.Guid.ToString();
            _config.Global.PowerPlan.DesiredName = pi.Name;
        }

        foreach (var row in GlobalToggleRows) row.WriteBack();
        foreach (var row in DisplayRows) row.WriteTo(_config);
        foreach (var row in ServiceRows) row.WriteBack();
    }

    private bool _suppressSaveOnClose = false;

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_suppressSaveOnClose) return;
        try
        {
            PersistFormToConfig();
            _store.Save(_config);
        }
        catch { }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        try
        {
            DisplaysList.ItemsSource = null;
            GlobalTogglesList.ItemsSource = null;
            ServicesList.ItemsSource = null;
            DisplayRows.Clear();
            GlobalToggleRows.Clear();
            ServiceRows.Clear();
        }
        catch { }
    }

    private void OnRowPrefChanged(string settingName, string field, string before, string after)
    {
        try { _store.Save(_config); } catch { }
        ChangeLogger.LogPreferenceChange(settingName, field, before, after);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _suppressSaveOnClose = true;
        Close();
    }
}

public sealed class PowerPlanItem
{
    public Guid Guid { get; }
    public string Name { get; }
    public PowerPlanItem(Guid guid, string name) { Guid = guid; Name = name; }
    public override string ToString() => Name;
}

public sealed class GlobalToggleRow : INotifyPropertyChanged
{
    private readonly ToggleSettingPref _pref;
    private readonly Action<string, string, string, string>? _onPrefChanged;
    public string Name { get; }
    public string Description { get; }
    public string CurrentText { get; }
    public string DefaultText { get; }
    public string OnLabel { get; }
    public string OffLabel { get; }
    public string GroupName { get; }
    public bool RequiresReboot { get; }
    public Visibility RebootBadgeVisibility => RequiresReboot ? Visibility.Visible : Visibility.Collapsed;

    public bool Monitor
    {
        get => _pref.Monitor;
        set
        {
            if (_pref.Monitor == value) return;
            var before = _pref.Monitor;
            _pref.Monitor = value;
            OnPropertyChanged();
            _onPrefChanged?.Invoke(Name, "Monitor", before.ToString(), value.ToString());
        }
    }
    public bool DesiredOn
    {
        get => _pref.DesiredOn;
        set
        {
            if (_pref.DesiredOn == value) return;
            var before = _pref.DesiredOn;
            _pref.DesiredOn = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DesiredOff));
            _onPrefChanged?.Invoke(Name, "Want", before ? OnLabel : OffLabel, value ? OnLabel : OffLabel);
        }
    }
    public bool DesiredOff
    {
        get => !_pref.DesiredOn;
        set { if (value && _pref.DesiredOn) DesiredOn = false; }
    }
    public bool AutoApply
    {
        get => _pref.AutoApply;
        set
        {
            if (_pref.AutoApply == value) return;
            var before = _pref.AutoApply;
            _pref.AutoApply = value;
            OnPropertyChanged();
            _onPrefChanged?.Invoke(Name, "AutoApply", before.ToString(), value.ToString());
        }
    }

    public GlobalToggleRow(string name, string description, string currentText, string defaultText,
                           string onLabel, string offLabel,
                           ToggleSettingPref pref, string groupName,
                           bool requiresReboot = false,
                           Action<string, string, string, string>? onPrefChanged = null)
    {
        Name = name;
        Description = description;
        CurrentText = currentText;
        DefaultText = defaultText;
        OnLabel = onLabel;
        OffLabel = offLabel;
        _pref = pref;
        GroupName = groupName;
        RequiresReboot = requiresReboot;
        _onPrefChanged = onPrefChanged;
    }

    public void WriteBack() { /* mutations are direct; nothing to do */ }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class ServiceRow : INotifyPropertyChanged
{
    private readonly ServicePref _pref;
    private readonly Action<string, string, string, string>? _onPrefChanged;

    public ServiceDefinition Definition { get; }
    public string Name => Definition.DisplayName;
    public string ServiceName => Definition.Name;
    public string Description => Definition.Description;
    public string CurrentText { get; }
    public string DefaultText { get; }
    public string GroupName { get; }
    public bool IsInstalled { get; }

    public bool RequiresReboot => Definition.RequiresReboot;
    public Visibility RebootBadgeVisibility =>
        RequiresReboot ? Visibility.Visible : Visibility.Collapsed;
    public Visibility RecommendedBadgeVisibility =>
        Definition.RecommendedTarget.HasValue ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NotInstalledBadgeVisibility =>
        IsInstalled ? Visibility.Collapsed : Visibility.Visible;

    public bool Monitor
    {
        get => _pref.Monitor;
        set
        {
            if (_pref.Monitor == value) return;
            var before = _pref.Monitor;
            _pref.Monitor = value;
            OnPropertyChanged();
            _onPrefChanged?.Invoke($"Service: {Definition.DisplayName}", "Monitor", before.ToString(), value.ToString());
        }
    }
    public bool DesiredDefault
    {
        get => _pref.Desired == ServiceTargetState.Default;
        set { if (value) SetDesired(ServiceTargetState.Default); }
    }
    public bool DesiredManual
    {
        get => _pref.Desired == ServiceTargetState.Manual;
        set { if (value) SetDesired(ServiceTargetState.Manual); }
    }
    public bool DesiredDisabled
    {
        get => _pref.Desired == ServiceTargetState.Disabled;
        set { if (value) SetDesired(ServiceTargetState.Disabled); }
    }

    /// <summary>Direct setter that bypasses the per-radio plumbing. Used by the preset.</summary>
    public void SetDesiredFromPreset(ServiceTargetState v) => SetDesired(v);

    private void SetDesired(ServiceTargetState v)
    {
        if (_pref.Desired == v) return;
        var before = _pref.Desired;
        _pref.Desired = v;
        OnPropertyChanged(nameof(DesiredDefault));
        OnPropertyChanged(nameof(DesiredManual));
        OnPropertyChanged(nameof(DesiredDisabled));
        _onPrefChanged?.Invoke($"Service: {Definition.DisplayName}", "Want",
            before.ToString(), v.ToString());
    }
    public bool AutoApply
    {
        get => _pref.AutoApply;
        set
        {
            if (_pref.AutoApply == value) return;
            var before = _pref.AutoApply;
            _pref.AutoApply = value;
            OnPropertyChanged();
            _onPrefChanged?.Invoke($"Service: {Definition.DisplayName}", "AutoApply", before.ToString(), value.ToString());
        }
    }

    public ServiceRow(
        ServiceDefinition def,
        ServicePref pref,
        bool isInstalled,
        string currentText,
        string defaultText,
        Action<string, string, string, string>? onPrefChanged)
    {
        Definition = def;
        _pref = pref;
        IsInstalled = isInstalled;
        CurrentText = currentText;
        DefaultText = defaultText;
        GroupName = "svc_" + def.Name;
        _onPrefChanged = onPrefChanged;
    }

    public void WriteBack() { /* mutations are direct; nothing to do */ }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class DisplayRow : INotifyPropertyChanged
{
    private readonly string _key;
    private readonly DisplayPreference _pref;

    public string HeaderText { get; }
    public string StatusText { get; }
    public string RateGroupName { get; }
    public IReadOnlyList<uint> AvailableHz { get; }
    public IReadOnlyList<string> AvailableResolutions { get; }

    public bool HdrMonitor { get => _pref.Hdr.Monitor; set { _pref.Hdr.Monitor = value; OnPropertyChanged(); } }
    public bool HdrDesiredOn { get => _pref.Hdr.DesiredOn; set { _pref.Hdr.DesiredOn = value; OnPropertyChanged(); } }
    public bool HdrAutoApply { get => _pref.Hdr.AutoApply; set { _pref.Hdr.AutoApply = value; OnPropertyChanged(); } }

    public bool RefreshMonitor { get => _pref.RefreshRate.Monitor; set { _pref.RefreshRate.Monitor = value; OnPropertyChanged(); } }
    public bool RefreshUseMax
    {
        get => _pref.RefreshRate.Target == RefreshRateTarget.Maximum;
        set
        {
            if (value)
            {
                _pref.RefreshRate.Target = RefreshRateTarget.Maximum;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RefreshUseFixed));
            }
        }
    }
    public bool RefreshUseFixed
    {
        get => _pref.RefreshRate.Target == RefreshRateTarget.Fixed;
        set
        {
            if (value)
            {
                _pref.RefreshRate.Target = RefreshRateTarget.Fixed;
                if (_pref.RefreshRate.FixedHz is null && AvailableHz.Count > 0)
                    _pref.RefreshRate.FixedHz = AvailableHz[^1];
                OnPropertyChanged();
                OnPropertyChanged(nameof(RefreshUseMax));
                OnPropertyChanged(nameof(FixedHz));
            }
        }
    }
    public uint? FixedHz
    {
        get => _pref.RefreshRate.FixedHz;
        set { _pref.RefreshRate.FixedHz = value; OnPropertyChanged(); }
    }
    public bool RefreshAutoApply { get => _pref.RefreshRate.AutoApply; set { _pref.RefreshRate.AutoApply = value; OnPropertyChanged(); } }

    public bool ResolutionMonitor { get => _pref.Resolution.Monitor; set { _pref.Resolution.Monitor = value; OnPropertyChanged(); } }
    public string? DesiredResolution
    {
        get => _pref.Resolution.DesiredWidth is { } w && _pref.Resolution.DesiredHeight is { } h ? $"{w}x{h}" : null;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _pref.Resolution.DesiredWidth = null;
                _pref.Resolution.DesiredHeight = null;
            }
            else
            {
                var parts = value.Split('x');
                if (parts.Length == 2 && uint.TryParse(parts[0], out var w) && uint.TryParse(parts[1], out var h))
                {
                    _pref.Resolution.DesiredWidth = w;
                    _pref.Resolution.DesiredHeight = h;
                }
            }
            OnPropertyChanged();
        }
    }
    public bool ResolutionAutoApply { get => _pref.Resolution.AutoApply; set { _pref.Resolution.AutoApply = value; OnPropertyChanged(); } }

    public DisplayRow(string key, string label, string status, DisplayPreference pref, IReadOnlyList<uint> rates, IReadOnlyList<string> resolutions)
    {
        _key = key;
        _pref = pref;
        HeaderText = label;
        StatusText = status;
        RateGroupName = "rate_" + key.GetHashCode().ToString("X");
        AvailableHz = rates;
        AvailableResolutions = resolutions;
    }

    public void WriteTo(AppConfig cfg) => cfg.Displays[_key] = _pref;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
