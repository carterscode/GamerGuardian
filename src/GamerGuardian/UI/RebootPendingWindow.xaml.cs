using System.Diagnostics;
using System.Windows;
using Wpf.Ui.Controls;

namespace GamerGuardian.UI;

public partial class RebootPendingWindow : FluentWindow
{
    public RebootPendingWindow(IReadOnlyList<string> settingDescriptions)
    {
        InitializeComponent();
        ItemsList.ItemsSource = settingDescriptions;
        Loaded += (_, _) =>
        {
            var area = SystemParameters.WorkArea;
            Left = area.Right - Width - 16;
            Top = area.Bottom - ActualHeight - 16;
        };
    }

    private void RebootButton_Click(object sender, RoutedEventArgs e)
    {
        GamerGuardian.Services.RebootHelper.ForceRebootNow();
        Close();
    }

    private void LaterButton_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        try
        {
            ItemsList.ItemsSource = null;
            Content = null;
            DataContext = null;
        }
        catch { }
    }
}
