using System.Windows;
using System.Windows.Controls;
using GamerGuardian.Models;
using GamerGuardian.Services;

namespace GamerGuardian.UI.Views;

/// <summary>
/// The pinned Status view. Shows the drifted count in aggregate and per section,
/// and the monitoring pause state.
///
/// <para>Deliberately nothing else: no managed count, no last-scan timestamp, no
/// pause reason, no pause persistence — all recorded as decided against in
/// <c>docs/ui-overhaul.md</c>. The drifted count is the only status surface.</para>
///
/// <para>Reads <see cref="MonitorService.CurrentDrift"/>, which the scan already
/// publishes, so nothing here re-runs a drift check. Grouping uses
/// <see cref="SettingSectionMap"/>.</para>
/// </summary>
public partial class StatusView : System.Windows.Controls.UserControl
{
    private MonitorService? _monitor;

    public StatusView() => InitializeComponent();

    /// <summary>Attach to the live monitor. Safe to call with null (design time,
    /// or a window constructed without a monitor service).</summary>
    internal void Bind(MonitorService? monitor)
    {
        _monitor = monitor;
        if (monitor is not null)
        {
            // Raised on the poll thread -- marshal before touching controls.
            monitor.DriftChanged += OnDriftChanged;
            monitor.PauseChanged += OnPauseChanged;
        }
        Refresh();
    }

    internal void Detach()
    {
        if (_monitor is null) return;
        _monitor.DriftChanged -= OnDriftChanged;
        _monitor.PauseChanged -= OnPauseChanged;
        _monitor = null;
    }

    private void OnDriftChanged(IReadOnlyDictionary<string, DriftItem> _) =>
        Dispatcher.BeginInvoke(new Action(Refresh));

    private void OnPauseChanged(bool _) =>
        Dispatcher.BeginInvoke(new Action(Refresh));

    /// <summary>Re-render from the published snapshot. Cheap — no system reads.</summary>
    internal void Refresh()
    {
        try
        {
            var drift = _monitor?.CurrentDrift;
            int total = drift?.Count ?? 0;

            DriftCountText.Text = total.ToString();
            DriftHeadlineText.Text = total == 0
                ? "Everything matches your preferences"
                : total == 1
                    ? "1 setting has drifted"
                    : $"{total} settings have drifted";

            var okBg = (System.Windows.Media.Brush)FindResource("SystemFillColorSuccessBackgroundBrush");
            var okFg = (System.Windows.Media.Brush)FindResource("SystemFillColorSuccessBrush");
            var warnBg = (System.Windows.Media.Brush)FindResource("SystemFillColorCautionBackgroundBrush");
            var warnFg = (System.Windows.Media.Brush)FindResource("SystemFillColorCautionBrush");
            DriftCountBadge.Background = total == 0 ? okBg : warnBg;
            DriftCountText.Foreground = total == 0 ? okFg : warnFg;

            BuildSectionCounts(drift);
            RefreshPause();
        }
        catch { /* status is informational; never let it break the window */ }
    }

    private void BuildSectionCounts(IReadOnlyDictionary<string, DriftItem>? drift)
    {
        SectionCountsList.Children.Clear();

        var counts = new Dictionary<SettingSection, int>();
        if (drift is not null)
        {
            foreach (var id in drift.Keys)
            {
                var s = SettingSectionMap.SectionFor(id);
                counts[s] = counts.GetValueOrDefault(s) + 1;
            }
        }

        // Every section is listed, including the ones at zero, so the view reads as
        // a complete picture rather than a list that mysteriously grows and shrinks.
        foreach (var (section, label) in SectionLabels)
        {
            int n = counts.GetValueOrDefault(section);
            SectionCountsList.Children.Add(BuildRow(label, n));
        }

        // Anything unmapped would otherwise be invisible; surface it rather than
        // silently dropping it from the total.
        int unknown = counts.GetValueOrDefault(SettingSection.Unknown);
        if (unknown > 0) SectionCountsList.Children.Add(BuildRow("Unmapped", unknown));
    }

    private static readonly (SettingSection Section, string Label)[] SectionLabels =
    {
        (SettingSection.Gaming, "Gaming"),
        (SettingSection.Display, "Display"),
        (SettingSection.CpuPower, "CPU and power"),
        (SettingSection.Telemetry, "Telemetry"),
        (SettingSection.WindowsAi, "Windows AI"),
        (SettingSection.Network, "Network"),
        (SettingSection.Debloat, "Debloat"),
        (SettingSection.Services, "Services"),
    };

    private UIElement BuildRow(string label, int count)
    {
        var grid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var name = new TextBlock { Text = label, FontSize = 13, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(name, 0);
        grid.Children.Add(name);

        var value = new TextBlock
        {
            Text = count.ToString(),
            FontSize = 13,
            FontWeight = count > 0 ? FontWeights.SemiBold : FontWeights.Normal,
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (count == 0)
            value.Foreground = (System.Windows.Media.Brush)FindResource("TextFillColorTertiaryBrush");
        Grid.SetColumn(value, 1);
        grid.Children.Add(value);

        return grid;
    }

    private void RefreshPause()
    {
        bool paused = _monitor?.IsUserPaused ?? false;
        PauseStateText.Text = paused
            ? "Paused. Nothing is being checked or corrected until you resume."
            : "Running. Monitored settings are checked in the background.";
        PauseToggleButton.Content = paused ? "Resume monitoring" : "Pause monitoring";
        PauseToggleButton.IsEnabled = _monitor is not null;
    }

    private void PauseToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _monitor?.TogglePaused();
        RefreshPause();
    }
}
