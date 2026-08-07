using System.Windows;
using GamerGuardian.Models;
using Wpf.Ui.Controls;

namespace GamerGuardian.UI;

public partial class NotificationWindow : FluentWindow
{
    private readonly DriftReport _report;

    public NotificationWindow(DriftReport report)
    {
        InitializeComponent();
        _report = report;
        ItemsList.ItemsSource = report.Items;
        HeaderText.Text = Services.NotificationHeader.For(report);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 16;
        Top = area.Bottom - ActualHeight - 16;
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyButton.IsEnabled = false;
        DismissButton.IsEnabled = false;
        // Applying from a drift notification is still an apply — if a reboot-required
        // setting was among them, the user needs the same restart prompt they'd get
        // from the Settings Apply button. Only items whose Apply didn't throw are
        // flagged (a declined UAC prompt shouldn't claim a reboot is pending).
        var rebootDescriptions = new List<string>();
        foreach (var item in _report.Items)
        {
            try
            {
                await item.Apply();
                if (item.RequiresReboot) rebootDescriptions.Add(item.Description);
            }
            catch { }
        }
        if (rebootDescriptions.Count > 0)
            Services.RebootPrompt.Show(rebootDescriptions);
        Close();
    }

    private void DismissButton_Click(object sender, RoutedEventArgs e) => Close();
}
