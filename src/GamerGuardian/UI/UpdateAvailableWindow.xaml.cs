using System.Diagnostics;
using System.Windows;
using GamerGuardian.Services;
using Wpf.Ui.Controls;

namespace GamerGuardian.UI;

public partial class UpdateAvailableWindow : FluentWindow
{
    private readonly UpdateInfo _info;
    private readonly ConfigStore _store;
    private readonly Action _onAppShouldExit;

    public UpdateAvailableWindow(UpdateInfo info, ConfigStore store, Action onAppShouldExit)
    {
        InitializeComponent();
        _info = info;
        _store = store;
        _onAppShouldExit = onAppShouldExit;

        HeaderText.Text = $"GamerGuardian {info.Version} is available";
        SubText.Text = $"You're running {UpdateService.CurrentSemver()}.";
        NotesText.Text = string.IsNullOrWhiteSpace(info.ReleaseNotes)
            ? "(no release notes)"
            : info.ReleaseNotes;

        Loaded += (_, _) =>
        {
            var area = SystemParameters.WorkArea;
            Left = area.Right - Width - 16;
            Top = area.Bottom - ActualHeight - 16;
        };
    }

    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var cfg = _store.Load();
            cfg.SkippedUpdateVersion = _info.Version;
            _store.Save(cfg);
        }
        catch { }
        Close();
    }

    private void LaterButton_Click(object sender, RoutedEventArgs e) => Close();

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        SkipButton.IsEnabled = false;
        LaterButton.IsEnabled = false;
        InstallButton.IsEnabled = false;
        ProgressPanel.Visibility = Visibility.Visible;
        StatusText.Text = $"Downloading {Math.Round(_info.InstallerSize / 1024.0 / 1024.0, 1)} MB...";

        var progress = new Progress<double>(pct =>
        {
            DownloadProgress.Value = pct;
            StatusText.Text = $"Downloading... {Math.Round(pct * 100):0}%";
        });

        var path = await UpdateService.DownloadInstallerAsync(_info, progress);
        if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
        {
            StatusText.Text = "Download failed. Try again later.";
            SkipButton.IsEnabled = true;
            LaterButton.IsEnabled = true;
            InstallButton.IsEnabled = true;
            return;
        }

        StatusText.Text = "Launching installer...";

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
            _onAppShouldExit();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to launch: {ex.Message}";
            SkipButton.IsEnabled = true;
            LaterButton.IsEnabled = true;
            InstallButton.IsEnabled = true;
        }
    }
}
