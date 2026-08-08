using System.Windows;
using System.Windows.Controls;

namespace GamerGuardian.UI.Views;

/// <summary>
/// CPU / Power view: detected CPU and recipe tier, Power Throttling, the power-plan
/// selector, the plan build actions, the "What this plan changes" comparison chart,
/// and the dual-CCD dependency checklist.
///
/// <para>The densest view, and the one carrying the most read-only information —
/// the comparison chart and the CCD dependency card exist only to inform, so nothing
/// would fail if they were lost. Handlers forward to <see cref="SettingsWindow"/>.</para>
/// </summary>
public partial class CpuPowerView : System.Windows.Controls.UserControl
{
    public CpuPowerView() => InitializeComponent();

    internal SettingsWindow? Owner { get; set; }

    private void PowerPlanMonitorCheck_Changed(object sender, RoutedEventArgs e) =>
        Owner?.PowerPlanMonitorCheck_Changed(sender, e);

    private void PowerPlanAutoApplyCheck_Changed(object sender, RoutedEventArgs e) =>
        Owner?.PowerPlanAutoApplyCheck_Changed(sender, e);

    private void PowerPlanCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        Owner?.PowerPlanCombo_SelectionChanged(sender, e);

    private void SuggestPrebuiltButton_Click(object sender, RoutedEventArgs e) =>
        Owner?.SuggestPrebuiltButton_Click(sender, e);

    private void BuildOptimizedButton_Click(object sender, RoutedEventArgs e) =>
        Owner?.BuildOptimizedButton_Click(sender, e);
}
