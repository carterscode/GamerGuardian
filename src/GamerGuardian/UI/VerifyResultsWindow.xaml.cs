using System.Diagnostics;
using System.Linq;
using System.Windows;
using GamerGuardian.Models;
using GamerGuardian.Monitors;
using GamerGuardian.Services;
using Wpf.Ui.Controls;

namespace GamerGuardian.UI;

/// <summary>
/// What "Verify all" found, and what to do about it.
///
/// <para>Verify used to report a bare count in a MessageBox: it told the user a
/// setting had drifted without saying which one, and left them with no way to put it
/// back. This lists each drifted setting and offers to fix them through the same
/// verified-apply path the Settings Apply button uses, so the change is applied,
/// re-read, logged, and shown in the Apply Results window.</para>
///
/// <para>Monitored and unmonitored drift are both listed, because Verify checks
/// every setting, but unmonitored rows are badged. That difference is exactly why
/// Verify's count and the Status count disagreed: the Status count is defined as
/// monitored settings only.</para>
/// </summary>
public partial class VerifyResultsWindow : FluentWindow
{
    private readonly IReadOnlyList<DriftItem> _drifted;
    private readonly IReadOnlyList<IMonitoredSetting> _monitors;
    private readonly AppConfig _config;
    private readonly MonitorService? _monitorService;

    /// <summary>Raised after a fix run so the owner can reload its controls — the
    /// applied values are now different from what the open form is showing.</summary>
    public event Action? Fixed;

    public VerifyResultsWindow(
        IReadOnlyList<DriftItem> drifted,
        IReadOnlyList<IMonitoredSetting> monitors,
        AppConfig config,
        MonitorService? monitorService)
    {
        InitializeComponent();
        _drifted = drifted;
        _monitors = monitors;
        _config = config;
        _monitorService = monitorService;

        int monitored = drifted.Count(d => d.IsMonitored);
        int unmonitored = drifted.Count - monitored;

        HeaderText.Text = drifted.Count == 1
            ? "1 setting has drifted from your preferences"
            : $"{drifted.Count} settings have drifted from your preferences";

        SubText.Text = BuildSubText(monitored, unmonitored);

        ItemsList.ItemsSource = drifted.Select(d => new VerifyRow(d)).ToList();
    }

    /// <summary>
    /// Explains the count, including the monitored/unmonitored split when it is the
    /// reason the Status page disagrees. Pure so it can be unit-tested.
    /// </summary>
    public static string BuildSubText(int monitored, int unmonitored)
    {
        var text = "Nothing has been changed. A snapshot was written to the change log.";
        if (unmonitored > 0)
        {
            text += monitored > 0
                ? $" {monitored} of these are monitored and counted on the Status page; the other {unmonitored} "
                  + "are not monitored, so Status does not count them."
                : $" {(unmonitored == 1 ? "This setting is" : "These settings are")} not monitored, "
                  + "so the Status page does not count them — that is why its number can read 0 while this list is not empty.";
        }
        return text + " \"Fix these settings\" applies your preferred value and re-reads it to confirm.";
    }

    private async void FixButton_Click(object sender, RoutedEventArgs e)
    {
        FixButton.IsEnabled = false;
        CloseButton.IsEnabled = false;
        var original = FixButton.Content;
        FixButton.Content = "Fixing…";
        try
        {
            // Same path as the Settings Apply button: apply, re-read to verify, log,
            // then show the proof. "verify-fix" as the source so the change log
            // distinguishes this from a normal manual Apply.
            var results = await ChangeApplier.ApplyAndVerifyAsync(
                _drifted, _monitors, _config, source: "verify-fix",
                sessionId: ChangeApplier.NewSessionId());

            if (results.Count > 0)
            {
                ChangeLogger.LogApplyResults(results, "verify-fix");
                // Drops the fixed ids from the published drift snapshot, so the
                // Status count falls immediately instead of waiting for that tier's
                // next scan.
                _monitorService?.RecordVerifiedApplies(results);
            }

            Fixed?.Invoke();

            var win = new ApplyResultsWindow(results) { Owner = Owner ?? this };
            win.Show();

            // An unowned prompt, so it survives this window closing — same reason
            // the Settings Apply path uses RebootPrompt rather than a MessageBox.
            var rebootDescriptions = results
                .Where(r => r.RequiresReboot && r.Verified)
                .Select(r => r.Description)
                .ToList();
            if (rebootDescriptions.Count > 0) RebootPrompt.Show(rebootDescriptions);

            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Fix failed: " + ex.Message, "GamerGuardian",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            FixButton.Content = original;
            FixButton.IsEnabled = true;
            CloseButton.IsEnabled = true;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void OpenLogButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = ChangeLogger.LogPath;
            if (!System.IO.File.Exists(path))
            {
                System.IO.File.WriteAllText(path, "(no changes have been applied yet)\n");
            }
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch { }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        // WPF-UI memory hygiene: release the visual tree so the working set does not
        // creep across repeated opens.
        try
        {
            Content = null;
            DataContext = null;
            ItemsList.ItemsSource = null;
        }
        catch { }
    }
}

/// <summary>One drifted setting as the Verify list shows it.</summary>
public sealed class VerifyRow
{
    public VerifyRow(DriftItem d)
    {
        Description = string.IsNullOrWhiteSpace(d.Description) ? d.DisplayLabel : d.Description;
        CurrentValue = d.CurrentValue;
        DesiredValue = d.DesiredValue;
        IsMonitored = d.IsMonitored;
        SectionNote = BuildSectionNote(d);
    }

    public string Description { get; }
    public string CurrentValue { get; }
    public string DesiredValue { get; }
    public bool IsMonitored { get; }
    public string SectionNote { get; }

    public Visibility UnmonitoredBadgeVisibility =>
        IsMonitored ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>Where to find this setting in the app, plus how it is enforced.
    /// Pure so it can be unit-tested.</summary>
    public static string BuildSectionNote(DriftItem d)
    {
        var section = SettingSectionMap.SectionFor(d.SettingId) switch
        {
            SettingSection.Gaming => "Gaming",
            SettingSection.Display => "Display",
            SettingSection.CpuPower => "CPU and power",
            SettingSection.Telemetry => "Telemetry",
            SettingSection.WindowsAi => "Windows AI",
            SettingSection.Network => "Network",
            SettingSection.Debloat => "Debloat",
            SettingSection.Services => "Services",
            SettingSection.Bios => "BIOS",
            _ => null,
        };

        var mode = !d.IsMonitored
            ? "not monitored, so it is never corrected automatically"
            : d.AutoApply
                ? "set to apply silently, so the next scan should correct it on its own"
                : "monitored, so you are notified when it drifts";

        return section is null ? $"This setting is {mode}." : $"{section} — {mode}.";
    }
}
