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
    public ObservableCollection<DisplayRow> DisplayRows { get; } = new();
    public ObservableCollection<GlobalToggleRow> GlobalToggleRows { get; } = new();

    public event Action? Saved;

    public SettingsWindow(ConfigStore store)
    {
        InitializeComponent();
        _store = store;
        _config = store.Load();

        LaunchAtStartupCheck.IsChecked = _config.LaunchAtStartup;
        ConsolidateCheck.IsChecked = _config.ConsolidateNotifications;
        PollSecondsBox.Value = _config.PollIntervalSeconds;

        ThemeCombo.ItemsSource = Enum.GetValues<AppThemeChoice>();
        ThemeCombo.SelectedItem = _config.Theme;

        VersionLink.Content = GetVersionDisplay();
        VersionLink.ToolTip = GetVersionTooltip();

        DisplaysList.ItemsSource = DisplayRows;
        GlobalTogglesList.ItemsSource = GlobalToggleRows;

        LoadGlobals();
        LoadDisplays();
    }

    private static string OnOffText(bool? state) =>
        state is null ? "not detected" : (state.Value ? "On" : "Off");

    private static string GamingDefaultText(bool? state) =>
        state is null ? "not detected" : (state.Value ? "Gaming-optimized" : "Default");

    private void LoadGlobals()
    {
        GlobalToggleRows.Clear();
        var g = _config.Global;

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Game Mode",
            description: "Tells Windows to prioritize the running game and suppress background work.",
            currentText: $"Current: {OnOffText(SafeRead(GameModeMonitor.ReadCurrent))}",
            defaultText: "Default: On",
            pref: g.GameMode, groupName: "gm"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Game DVR background recording",
            description: "Always-on game capture. Costs CPU/GPU during gameplay; off is gaming-recommended.",
            currentText: $"Current: {OnOffText(SafeRead(GameDvrMonitor.ReadCurrent))}",
            defaultText: "Default: On",
            pref: g.GameDvr, groupName: "dvr"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Hardware-accelerated GPU Scheduling (HAGS)",
            description: "Lets the GPU manage its own command queue. Lower latency on supported GPUs. Requires reboot.",
            currentText: $"Current: {OnOffText(SafeRead(HagsMonitor.ReadCurrent))}",
            defaultText: "Default: On (Win11)",
            pref: g.Hags, groupName: "hags"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Memory Integrity / VBS (Core Isolation)",
            description: "Hypervisor-Enforced Code Integrity. Disabling recovers ~5–15% gaming perf but reduces malware protection. Requires reboot.",
            currentText: $"Current: {OnOffText(SafeRead(MemoryIntegrityMonitor.ReadCurrent))}",
            defaultText: "Default: On (Win11)",
            pref: g.MemoryIntegrity, groupName: "memint"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "System Responsiveness",
            description: "CPU percentage Windows reserves for non-multimedia tasks. Lower frees CPU for games.",
            currentText: $"Current: {GamingDefaultText(SafeRead(SystemResponsivenessMonitor.ReadCurrent))}",
            defaultText: "Default: 20    Gaming: 10",
            pref: g.SystemResponsiveness, groupName: "sysresp"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Network Throttling",
            description: "Multimedia packet pacing. Disabling reduces network jitter for online games.",
            currentText: $"Current: {GamingDefaultText(SafeRead(NetworkThrottlingMonitor.ReadCurrent))}",
            defaultText: "Default: 10    Gaming: disabled",
            pref: g.NetworkThrottling, groupName: "netthr"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "USB Selective Suspend (global)",
            description: "Lets Windows suspend idle USB devices. Disabling keeps mice/keyboards/headsets always responsive.",
            currentText: $"Current: {GamingDefaultText(SafeRead(UsbSelectiveSuspendMonitor.ReadCurrent))}",
            defaultText: "Default: enabled    Gaming: disabled",
            pref: g.UsbSelectiveSuspend, groupName: "usbsus"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Games multimedia task profile",
            description: "Priority + scheduling values for processes registered with the Games multimedia class.",
            currentText: $"Current: {GamingDefaultText(SafeRead(GamesTaskProfileMonitor.ReadCurrent))}",
            defaultText: "Default: standard    Gaming: boosted",
            pref: g.GamesTaskProfile, groupName: "gtask"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Mouse \"Enhance pointer precision\"",
            description: "Acceleration curve applied to mouse movement. Most gamers want this off for consistent aim.",
            currentText: $"Current: {OnOffText(SafeRead(MousePrecisionMonitor.ReadCurrent))}",
            defaultText: "Default: On",
            pref: g.MousePrecision, groupName: "mp"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Fullscreen optimizations (global)",
            description: "Borderless-windowed compositing layer. Generally fine; some titles prefer it off.",
            currentText: $"Current: {OnOffText(SafeRead(FullscreenOptimizationsMonitor.ReadCurrent))}",
            defaultText: "Default: On",
            pref: g.FullscreenOptimizations, groupName: "fso"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Variable Refresh Rate (Windows)",
            description: "DirectX user-pref VRR toggle. Smoother frame pacing on G-Sync / FreeSync displays.",
            currentText: $"Current: {OnOffText(SafeRead(VrrMonitor.ReadCurrent))}",
            defaultText: "Default: not set",
            pref: g.Vrr, groupName: "vrr"));

        var planNames = PowerPlanMonitor.ListAvailablePlans();
        var active = SafeRunGuid(PowerPlanMonitor.GetActivePlan);
        var activeName = active is not null && planNames.TryGetValue(active.Value, out var name) ? name : "unknown";
        PowerPlanCurrentText.Text = $"Current: {activeName}";
        PowerPlanMonitorCheck.IsChecked = g.PowerPlan.Monitor;
        PowerPlanAutoApplyCheck.IsChecked = g.PowerPlan.AutoApply;
        var availableChoices = Enum.GetValues<PowerPlanChoice>()
            .Where(c => planNames.ContainsKey(PowerPlanMonitor.ToGuid(c)))
            .ToList();
        if (availableChoices.Count == 0) availableChoices = Enum.GetValues<PowerPlanChoice>().ToList();
        PowerPlanCombo.ItemsSource = availableChoices;
        PowerPlanCombo.SelectedItem = availableChoices.Contains(g.PowerPlan.Desired)
            ? g.PowerPlan.Desired
            : availableChoices[0];
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

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _config.LaunchAtStartup = LaunchAtStartupCheck.IsChecked == true;
        _config.ConsolidateNotifications = ConsolidateCheck.IsChecked == true;
        if (PollSecondsBox.Value is double pv && pv >= 5)
            _config.PollIntervalSeconds = (int)pv;
        if (ThemeCombo.SelectedItem is AppThemeChoice tc)
            _config.Theme = tc;

        _config.Global.PowerPlan.Monitor = PowerPlanMonitorCheck.IsChecked == true;
        _config.Global.PowerPlan.AutoApply = PowerPlanAutoApplyCheck.IsChecked == true;
        if (PowerPlanCombo.SelectedItem is PowerPlanChoice c)
            _config.Global.PowerPlan.Desired = c;

        foreach (var row in GlobalToggleRows) row.WriteBack();
        foreach (var row in DisplayRows) row.WriteTo(_config);

        _store.Save(_config);
        StartupRegistration.Sync(_config.LaunchAtStartup);
        Saved?.Invoke();
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}

public sealed class GlobalToggleRow : INotifyPropertyChanged
{
    private readonly ToggleSettingPref _pref;
    public string Name { get; }
    public string Description { get; }
    public string CurrentText { get; }
    public string DefaultText { get; }
    public string GroupName { get; }

    public bool Monitor { get => _pref.Monitor; set { _pref.Monitor = value; OnPropertyChanged(); } }
    public bool DesiredOn
    {
        get => _pref.DesiredOn;
        set { if (_pref.DesiredOn != value) { _pref.DesiredOn = value; OnPropertyChanged(); OnPropertyChanged(nameof(DesiredOff)); } }
    }
    public bool DesiredOff
    {
        get => !_pref.DesiredOn;
        set { if (value && _pref.DesiredOn) { _pref.DesiredOn = false; OnPropertyChanged(); OnPropertyChanged(nameof(DesiredOn)); } }
    }
    public bool AutoApply { get => _pref.AutoApply; set { _pref.AutoApply = value; OnPropertyChanged(); } }

    public GlobalToggleRow(string name, string description, string currentText, string defaultText,
                           ToggleSettingPref pref, string groupName)
    {
        Name = name;
        Description = description;
        CurrentText = currentText;
        DefaultText = defaultText;
        _pref = pref;
        GroupName = groupName;
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
