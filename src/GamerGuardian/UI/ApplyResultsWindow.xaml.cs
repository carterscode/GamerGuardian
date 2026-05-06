using System.Diagnostics;
using System.Windows;
using GamerGuardian.Models;
using Wpf.Ui.Controls;

namespace GamerGuardian.UI;

public partial class ApplyResultsWindow : FluentWindow
{
    public ApplyResultsWindow(IReadOnlyList<ApplyResult> results)
    {
        InitializeComponent();
        var ok = results.Count(r => r.Verified);
        var fail = results.Count - ok;
        HeaderText.Text = fail == 0
            ? $"Applied {ok} setting{(ok == 1 ? "" : "s")} successfully"
            : $"Applied {ok} of {results.Count} settings ({fail} failed)";
        SubText.Text = "Each row shows the value before, what you wanted, and the actual value re-read from the OS after apply. The 'Mechanism' line tells you exactly where the change was written; the PowerShell snippet lets you verify the same value yourself outside the app.";

        ItemsList.ItemsSource = results.Select(r => new ResultRow(r)).ToList();

        if (results.Any(r => r.RequiresReboot && r.Verified))
            RebootButton.Visibility = Visibility.Visible;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

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

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string s && !string.IsNullOrEmpty(s))
        {
            try { System.Windows.Clipboard.SetText(s); }
            catch { }
        }
    }
}

public sealed class ResultRow
{
    public string Description { get; }
    public string Before { get; }
    public string Desired { get; }
    public string After { get; }
    public bool RequiresReboot { get; }
    public string Mechanism { get; }
    public string VerifyCommand { get; }
    public string Icon { get; }
    public System.Windows.Media.Brush IconColor { get; }
    public Visibility RebootBadgeVisibility { get; }
    public Visibility HasVerifyCommandVisibility { get; }

    public ResultRow(ApplyResult r)
    {
        Description = r.Description;
        Before = r.Before;
        Desired = r.Desired;
        After = r.After;
        RequiresReboot = r.RequiresReboot;
        Mechanism = r.Mechanism;
        VerifyCommand = r.VerifyCommand;
        Icon = r.Verified ? "CheckmarkCircle24" : "ErrorCircle24";
        IconColor = r.Verified
            ? (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["SystemFillColorSuccessBrush"]
            : (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["SystemFillColorCriticalBrush"];
        RebootBadgeVisibility = r.RequiresReboot ? Visibility.Visible : Visibility.Collapsed;
        HasVerifyCommandVisibility = string.IsNullOrEmpty(r.VerifyCommand) ? Visibility.Collapsed : Visibility.Visible;
    }
}
