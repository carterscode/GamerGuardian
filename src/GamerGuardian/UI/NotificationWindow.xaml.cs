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
        foreach (var item in _report.Items)
        {
            try { await item.Apply(); } catch { }
        }
        Close();
    }

    private void DismissButton_Click(object sender, RoutedEventArgs e) => Close();
}
