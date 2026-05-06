using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using GamerGuardian.Models;
using GamerGuardian.Monitors;
using GamerGuardian.Native;
using GamerGuardian.Services;

namespace GamerGuardian.UI;

public partial class SettingsWindow : Window
{
    private readonly ConfigStore _store;
    private readonly AppConfig _config;
    public ObservableCollection<DisplayRow> Rows { get; } = new();

    public event Action? Saved;

    public SettingsWindow(ConfigStore store)
    {
        InitializeComponent();
        _store = store;
        _config = store.Load();

        LaunchAtStartupCheck.IsChecked = _config.LaunchAtStartup;
        ConsolidateCheck.IsChecked = _config.ConsolidateNotifications;
        PollSecondsBox.Text = _config.PollIntervalSeconds.ToString(CultureInfo.InvariantCulture);

        DisplaysList.ItemsSource = Rows;
        LoadRows();
    }

    private void LoadRows()
    {
        Rows.Clear();
        foreach (var d in DisplayHelper.EnumerateActiveDisplays())
        {
            if (!_config.Displays.TryGetValue(d.StableKey, out var pref))
            {
                pref = new DisplayPreference { DisplayLabel = d.DisplayLabel };
                _config.Displays[d.StableKey] = pref;
            }
            var hdr = HdrMonitor.ReadHdrState(d);
            var refresh = RefreshRateMonitor.GetCurrentRefresh(d.GdiDeviceName);
            uint maxHz = refresh is null ? 0 : RefreshRateMonitor.GetMaxSupportedRefresh(d.GdiDeviceName, refresh.Value.Width, refresh.Value.Height);
            var rates = refresh is null ? Array.Empty<uint>() : RefreshRateMonitor.GetSupportedRefreshRates(d.GdiDeviceName, refresh.Value.Width, refresh.Value.Height);

            var status = string.Format(CultureInfo.InvariantCulture,
                "Now — HDR: {0}{1}    Refresh: {2}{3}",
                hdr is null ? "unknown" : (hdr.Value.Supported ? (hdr.Value.Enabled ? "On" : "Off") : "not supported"),
                hdr?.Supported == false ? "" : "",
                refresh is null ? "unknown" : refresh.Value.Hz + " Hz",
                maxHz > 0 ? $" (max {maxHz} Hz)" : "");

            Rows.Add(new DisplayRow(d.StableKey, d.DisplayLabel, status, pref, rates, maxHz));
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _config.LaunchAtStartup = LaunchAtStartupCheck.IsChecked == true;
        _config.ConsolidateNotifications = ConsolidateCheck.IsChecked == true;
        if (int.TryParse(PollSecondsBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) && s >= 5)
            _config.PollIntervalSeconds = s;

        foreach (var row in Rows) row.WriteTo(_config);
        _store.Save(_config);
        StartupRegistration.Sync(_config.LaunchAtStartup);
        Saved?.Invoke();
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}

public sealed class DisplayRow : INotifyPropertyChanged
{
    private readonly string _key;
    private readonly DisplayPreference _pref;

    public string HeaderText { get; }
    public string StatusText { get; }
    public string GroupName { get; }
    public IReadOnlyList<uint> AvailableHz { get; }

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

    public DisplayRow(string key, string label, string status, DisplayPreference pref, IReadOnlyList<uint> rates, uint maxHz)
    {
        _key = key;
        _pref = pref;
        HeaderText = label;
        StatusText = status;
        GroupName = "rate_" + key.GetHashCode().ToString("X");
        AvailableHz = rates;
    }

    public void WriteTo(AppConfig cfg) => cfg.Displays[_key] = _pref;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
