using System.Windows;
using System.Windows.Controls;

namespace GamerGuardian.UI.Views;

/// <summary>
/// General view: one-click presets, theme, startup, update check, polling
/// interval, change log.
///
/// <para>Interactive handlers forward to <see cref="SettingsWindow"/>, which still
/// owns the draft and every apply path. Keeping the logic there rather than moving
/// it per-view was deliberate for this extraction — the goal was to split the XAML
/// without also rewriting 2,300 lines of working behavior in the same step.</para>
/// </summary>
public partial class GeneralView : System.Windows.Controls.UserControl
{
    public GeneralView() => InitializeComponent();

    internal SettingsWindow? Owner { get; set; }

    private void ResetToDefaultsButton_Click(object sender, RoutedEventArgs e) =>
        Owner?.ResetToDefaultsButton_Click(sender, e);

    private void ApplyRecommendedPresetButton_Click(object sender, RoutedEventArgs e) =>
        Owner?.ApplyRecommendedPresetButton_Click(sender, e);

    private void ApplyExtremePresetButton_Click(object sender, RoutedEventArgs e) =>
        Owner?.ApplyExtremePresetButton_Click(sender, e);

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        Owner?.ThemeCombo_SelectionChanged(sender, e);

    private void CheckUpdatesNowButton_Click(object sender, RoutedEventArgs e) =>
        Owner?.CheckUpdatesNowButton_Click(sender, e);

    private void OpenChangeLogButton_Click(object sender, RoutedEventArgs e) =>
        Owner?.OpenChangeLogButton_Click(sender, e);
}
