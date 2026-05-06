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
        try
        {
            Process.Start("shutdown.exe",
                "/r /t 30 /c \"Rebooting for GamerGuardian settings. Cancel: shutdown /a\"");
        }
        catch { }
        Close();
    }

    private void LaterButton_Click(object sender, RoutedEventArgs e) => Close();
}
