using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using GamerGuardian.Models;
using GamerGuardian.Monitors;
using GamerGuardian.Native;
using GamerGuardian.Services;
using Wpf.Ui.Controls;

namespace GamerGuardian.UI;

public partial class SettingsWindow : FluentWindow
{
    private readonly ConfigStore _store;
    /// <summary>The committed config that the background <see cref="MonitorService"/> reads
    /// from disk on each tick. The UI never writes to this directly — only
    /// <see cref="ApplyChangesAsync"/> does, after copying the draft over.</summary>
    private readonly AppConfig _config;
    /// <summary>A deep clone of <see cref="_config"/> that the UI freely mutates as
    /// the user toggles radios / checkboxes / combos. Discarded on Cancel /
    /// window close; copied back into <see cref="_config"/> on Apply / Save &amp; close.
    /// This is what makes "click a radio, click Cancel, nothing happens" work.</summary>
    private AppConfig _draft;
    private readonly IReadOnlyList<IMonitoredSetting> _monitors;
    private readonly MonitorService? _monitorService;
    private readonly Action _exitApp;

    // One instance per navigation destination, created once and kept alive. Held
    // rather than rebuilt on navigation so scroll position, expander state and
    // selection survive moving between sections -- and so the Load*() methods below
    // keep a stable target to write into, exactly as they did with the TabControl.
    private readonly Views.StatusView _status = new();
    private readonly Views.GeneralView _general = new();
    private readonly Views.GamingView _gaming = new();
    private readonly Views.DisplayView _display = new();
    private readonly Views.CpuPowerView _cpuPower = new();
    private readonly Views.TelemetryView _telemetry = new();
    private readonly Views.WindowsAiView _windowsAi = new();
    private readonly Views.NetworkView _network = new();
    private readonly Views.DebloatView _debloat = new();
    private readonly Views.ServicesView _services = new();
    private readonly Views.BiosView _bios = new();
    public ObservableCollection<DisplayRow> DisplayRows { get; } = new();
    public ObservableCollection<GlobalToggleRow> GlobalToggleRows { get; } = new();
    public ObservableCollection<GlobalToggleRow> PrivacyToggleRows { get; } = new();
    public ObservableCollection<GlobalToggleRow> DebloatAdsRows { get; } = new();
    public ObservableCollection<GlobalToggleRow> DebloatBackgroundRows { get; } = new();
    public ObservableCollection<GlobalToggleRow> NetworkToggleRows { get; } = new();
    public ObservableCollection<GlobalToggleRow> PowerToggleRows { get; } = new();
    public ObservableCollection<GlobalToggleRow> WindowsAiRowsCollection { get; } = new();
    public ObservableCollection<WindowsAiAppRow> WindowsAiAppRowsCollection { get; } = new();
    public ObservableCollection<ServiceRow> ServiceRows { get; } = new();
    public ObservableCollection<ScheduledTaskRow> ScheduledTaskRows { get; } = new();
    private bool _suppressPresetEvents;
    /// <summary>Set while LoadGlobals seeds the power-plan combo, so the
    /// SelectionChanged handler ignores the programmatic selection and doesn't
    /// stage a phantom pending change.</summary>
    private bool _loadingPowerPlan;
    /// <summary>
    /// Number of staged preference toggles since the window opened (or since
    /// the last successful Apply). Drives the "N pending changes" status text
    /// next to the Apply button. Counts every toggle, not net diff — clicking
    /// Disabled then back to Default reads as 2 pending. Simple but honest.
    /// </summary>
    private int _pendingCount;

    public event Action? Saved;

    public SettingsWindow(ConfigStore store, IReadOnlyList<IMonitoredSetting> monitors, Action exitApp)
        : this(store, monitors, exitApp, monitorService: null) { }

    public SettingsWindow(
        ConfigStore store,
        IReadOnlyList<IMonitoredSetting> monitors,
        Action exitApp,
        MonitorService? monitorService)
    {
        InitializeComponent();
        _store = store;
        _monitors = monitors;
        _exitApp = exitApp;
        _monitorService = monitorService;
        _config = store.Load();
        _draft = AppConfigCloner.Clone(_config);

        // Views forward their interactive handlers back here; this window still owns
        // the draft and every apply path, exactly as it did behind the TabControl.
        _general.Owner = this;
        _services.Owner = this;
        _cpuPower.Owner = this;
        _status.Bind(_monitorService);

        _general.LaunchAtStartupCheck.IsChecked = _draft.LaunchAtStartup;
        _general.CheckForUpdatesCheck.IsChecked = _draft.CheckForUpdatesOnStartup;
        _general.PollSecondsBox.Value = _draft.PollIntervalSeconds;

        _general.ThemeCombo.ItemsSource = Enum.GetValues<AppThemeChoice>();
        _general.ThemeCombo.SelectedItem = _draft.Theme;

        VersionLink.Content = GetVersionDisplay();
        VersionLink.ToolTip = GetVersionTooltip();

        _display.DisplaysList.ItemsSource = DisplayRows;
        _gaming.GlobalTogglesList.ItemsSource = GlobalToggleRows;
        _telemetry.PrivacyTogglesList.ItemsSource = PrivacyToggleRows;
        _debloat.DebloatAdsList.ItemsSource = DebloatAdsRows;
        _debloat.DebloatBackgroundList.ItemsSource = DebloatBackgroundRows;
        _network.NetworkTogglesList.ItemsSource = NetworkToggleRows;
        _cpuPower.PowerTogglesList.ItemsSource = PowerToggleRows;
        _services.ServicesList.ItemsSource = ServiceRows;
        _services.ScheduledTasksList.ItemsSource = ScheduledTaskRows;
        _windowsAi.WindowsAiRows.ItemsSource = WindowsAiRowsCollection;
        _windowsAi.WindowsAiAppRows.ItemsSource = WindowsAiAppRowsCollection;

        LoadGlobals();
        LoadDisplays();
        LoadServices();
        LoadScheduledTasks();
        LoadWindowsAi();
        LoadPrivacy();
        LoadDebloat();
        LoadNetwork();
        LoadCpuTabs();
        UpdatePendingStatus();

        // Beta marker on the window title and the title bar. Appended in code rather
        // than in XAML so the markup stays identical between build flavors;
        // DisplaySuffix is "" in a stable build, making both lines no-ops there.
        Title += AppIdentity.DisplaySuffix;
        if (WindowTitleBar is not null)
            WindowTitleBar.Title += AppIdentity.DisplaySuffix;

        // Land on Status. NavigationView.SelectedItem is read-only, so the initial
        // content is set directly and the first item is marked active for the pane
        // highlight; every later change comes through SelectionChanged.
        if (MainNav.MenuItems.Count > 0 &&
            MainNav.MenuItems[0] is Wpf.Ui.Controls.NavigationViewItem first)
        {
            first.IsActive = true;
        }
        Navigate("status");
    }

    /// <summary>
    /// Maps a navigation item's Tag to its view. The views are long-lived fields, so
    /// this is a content swap rather than a page construction -- no page service, no
    /// per-navigation rebuild, and no loss of scroll or expander state.
    /// </summary>
    private void Navigate(string? tag)
    {
        System.Windows.UIElement view = tag switch
        {
            "status" => _status,
            "gaming" => _gaming,
            "display" => _display,
            "cpupower" => _cpuPower,
            "telemetry" => _telemetry,
            "windowsai" => _windowsAi,
            "network" => _network,
            "debloat" => _debloat,
            "services" => _services,
            "bios" => _bios,
            "general" => _general,
            _ => _status,
        };

        // Refresh the drift numbers on arrival so Status is current even if no scan
        // finished while the window was open.
        if (ReferenceEquals(view, _status)) _status.Refresh();

        MainNav.ReplaceContent(view, null);
    }

    private void MainNav_SelectionChanged(Wpf.Ui.Controls.NavigationView sender, RoutedEventArgs e)
    {
        if (sender.SelectedItem is FrameworkElement fe && fe.Tag is string tag)
            Navigate(tag);
    }

    /// <summary>
    /// Re-clones the committed config into a fresh draft. Called after a
    /// successful Apply so the rows we re-bind to reflect the now-applied state.
    /// </summary>
    private void RebaseDraftFromConfig()
    {
        _draft = AppConfigCloner.Clone(_config);
        _pendingCount = 0;
    }

    /// <summary>Updates the "N pending changes" status text in the button bar.</summary>
    private void UpdatePendingStatus()
    {
        try
        {
            if (PendingStatusText is null) return;
            PendingStatusText.Text = _pendingCount switch
            {
                0 => "No pending changes",
                1 => "1 pending change",
                _ => $"{_pendingCount} pending changes",
            };
        }
        catch { /* binding may not be ready during early init */ }
    }

    private void LoadServices()
    {
        ServiceRows.Clear();
        foreach (var def in ServiceCatalog.All)
        {
            if (!_draft.Services.TryGetValue(def.Name, out var pref) || pref is null)
            {
                pref = new ServicePref();
                _draft.Services[def.Name] = pref;
            }

            var installed = WindowsServiceController.Exists(def.Name);
            string currentText;
            string defaultText;

            if (def.PolicyOverride is { } po)
            {
                // Policy-managed service (DoSvc et al.). The service start type is
                // owned by Windows and reverts under WaaSMedicSvc — we don't display
                // it. What's user-relevant here is the policy registry value.
                int? policy = ReadPolicyDword(po);
                bool currentlyDisabledByPolicy = policy.HasValue && (uint)policy.Value == po.DisabledValue;

                currentText = currentlyDisabledByPolicy
                    ? "Current: Disabled by Group Policy"
                    : (policy.HasValue
                        ? $"Current: policy {po.PolicyValue}={policy.Value}"
                        : "Current: Windows default (no policy override)");
                defaultText = "Default: Windows default (no policy override)";

                if (!pref.Monitor && installed)
                {
                    pref.Desired = currentlyDisabledByPolicy
                        ? ServiceTargetState.Disabled
                        : ServiceTargetState.Default;
                }
            }
            else
            {
                var current = installed
                    ? WindowsServiceController.ReadStartType(def.Name)
                    : ServiceStartType.Unknown;

                // For services the user hasn't opted into monitoring, mirror current state
                // into Want so the radio doesn't lie about a "recommendation" the user never asked for.
                if (!pref.Monitor && installed && current != ServiceStartType.Unknown)
                {
                    pref.Desired = current switch
                    {
                        ServiceStartType.Disabled => ServiceTargetState.Disabled,
                        ServiceStartType.Manual when def.DefaultStartType != ServiceStartType.Manual
                            => ServiceTargetState.Manual,
                        _ => ServiceTargetState.Default,
                    };
                }

                var status = installed
                    ? WindowsServiceController.ReadStatus(def.Name)
                    : null;
                var statusSuffix = status is null ? "" : $", {status.Value}";
                currentText = installed
                    ? $"Current: {WindowsServiceMonitor.DescribeStart(current)}{statusSuffix}"
                    : "Current: not installed on this system";
                defaultText = $"Default: {WindowsServiceMonitor.DescribeStart(def.DefaultStartType)}";
            }

            ServiceRows.Add(new ServiceRow(
                def: def,
                pref: pref,
                isInstalled: installed,
                currentText: currentText,
                defaultText: defaultText,
                onPrefChanged: OnRowPrefChanged));
        }

        UpdatePresetRadio();
    }

    private void LoadScheduledTasks()
    {
        ScheduledTaskRows.Clear();

        // QueryState spawns schtasks.exe; run the five queries in parallel so opening
        // Settings blocks on the slowest single spawn, not the sum of all five.
        var states = ScheduledTaskCatalog.All
            .AsParallel().AsOrdered()
            .Select(def => (def, state: ScheduledTaskController.QueryState(def.TaskPath)))
            .ToList();

        foreach (var (def, state) in states)
        {
            if (!_draft.ScheduledTasks.TryGetValue(def.TaskPath, out var pref) || pref is null)
            {
                pref = new ScheduledTaskPref();
                _draft.ScheduledTasks[def.TaskPath] = pref;
            }

            bool present = state != ScheduledTaskState.NotPresent;

            // For tasks the user hasn't opted into monitoring, mirror the live state into
            // Want so the "Disable" checkbox doesn't claim an intent the user never set.
            if (!pref.Monitor && present)
                pref.Desired = state == ScheduledTaskState.Disabled
                    ? ScheduledTaskTarget.Disabled
                    : ScheduledTaskTarget.Default;

            var currentText = state switch
            {
                ScheduledTaskState.Disabled => "Current: Disabled",
                ScheduledTaskState.Enabled => "Current: Enabled",
                _ => "Current: not present on this system",
            };

            ScheduledTaskRows.Add(new ScheduledTaskRow(def, pref, present, currentText, OnRowPrefChanged));
        }
    }

    /// <summary>
    /// Populates the Windows AI tab: 5 policy toggles (same template as
    /// Global gaming) + the UWP-removal section. Reads current state via
    /// each monitor's static ReadCurrent / IsInstalled probe.
    /// </summary>
    private void LoadWindowsAi()
    {
        WindowsAiRowsCollection.Clear();
        var g = _draft.Global;

        WindowsAiRowsCollection.Add(new GlobalToggleRow(
            name: "Windows Copilot",
            description: "System-wide Copilot disable policy. Off hides the taskbar button and blocks the Copilot panel from opening.",
            currentText: $"Current: {OnOffText(SafeRead(CopilotMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.Copilot, groupName: "ai_copilot",
            onPrefChanged: OnRowPrefChanged,
            settingId: "ai.copilot"));

        WindowsAiRowsCollection.Add(new GlobalToggleRow(
            name: "Windows Recall + AI data analysis",
            description: "Group-policy block for Recall snapshotting and on-device AI screen analysis.",
            currentText: $"Current: {OnOffText(SafeRead(RecallMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.Recall, groupName: "ai_recall",
            onPrefChanged: OnRowPrefChanged,
            settingId: "ai.recall"));

        WindowsAiRowsCollection.Add(new GlobalToggleRow(
            name: "Click-to-Do (Snipping Tool AI)",
            description: "Disable the AI 'do something with this' action layer over screenshots.",
            currentText: $"Current: {OnOffText(SafeRead(ClickToDoMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.ClickToDo, groupName: "ai_ctd",
            onPrefChanged: OnRowPrefChanged,
            settingId: "ai.clicktodo"));

        WindowsAiRowsCollection.Add(new GlobalToggleRow(
            name: "Microsoft Edge Copilot / Hubs sidebar / GenAI",
            description: "Three Edge enterprise policies: hide the right-edge Copilot icon, block page-context sharing, and disable local generative AI.",
            currentText: $"Current: {OnOffText(SafeRead(EdgeAiMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.EdgeAi, groupName: "ai_edge",
            onPrefChanged: OnRowPrefChanged,
            settingId: "ai.edge"));

        WindowsAiRowsCollection.Add(new GlobalToggleRow(
            name: "Notepad Rewrite + Paint AI",
            description: "Per-user disable of Notepad Rewrite and Paint Cocreator / Image Creator / Generative Erase.",
            currentText: $"Current: {OnOffText(SafeRead(NotepadPaintAiMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.NotepadPaintAi, groupName: "ai_notepadpaint",
            onPrefChanged: OnRowPrefChanged,
            settingId: "ai.notepadpaint"));

        WindowsAiRowsCollection.Add(new GlobalToggleRow(
            name: "Search box AI suggestions + taskbar companion",
            description: "Hides the AI-flavored web suggestions in the Windows search box and disables the floating Copilot taskbar widget. Indexing itself is untouched.",
            currentText: $"Current: {OnOffText(SafeRead(SettingsSearchAiMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.SettingsSearchAi, groupName: "ai_settingssearch",
            onPrefChanged: OnRowPrefChanged,
            settingId: "ai.settingssearch"));

        WindowsAiRowsCollection.Add(new GlobalToggleRow(
            name: "Windows AI Actions (right-click rewrite / summarize)",
            description: "Disables Windows' shell-level AI Actions surface via the FeatureManagement override hive.",
            currentText: $"Current: {OnOffText(SafeRead(AiActionsMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.AiActions, groupName: "ai_actions",
            onPrefChanged: OnRowPrefChanged,
            settingId: "ai.actions"));

        WindowsAiRowsCollection.Add(new GlobalToggleRow(
            name: "Typing / input insights data collection",
            description: "HKCU opt-out of Windows' typing-data harvesting. Personalized suggestions degrade slightly; everything else works normally.",
            currentText: $"Current: {OnOffText(SafeRead(InputInsightsMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.InputInsights, groupName: "ai_inputinsights",
            onPrefChanged: OnRowPrefChanged,
            settingId: "ai.inputinsights"));

        WindowsAiRowsCollection.Add(new GlobalToggleRow(
            name: "Microsoft 365 Copilot in Word / Excel / OneNote",
            description: "Disables the Copilot ribbon entries in the desktop Office apps + opts out of MS AI model training on document contents. No-op if Office isn't installed.",
            currentText: $"Current: {OnOffText(SafeRead(OfficeCopilotMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.OfficeCopilot, groupName: "ai_office",
            onPrefChanged: OnRowPrefChanged,
            settingId: "ai.office"));

        // UWP packages
        WindowsAiAppRowsCollection.Clear();
        foreach (var def in WindowsAiAppCatalog.All)
        {
            if (!_draft.WindowsAiApps.TryGetValue(def.PackageName, out var pref) || pref is null)
            {
                pref = new WindowsAiAppPref();
                _draft.WindowsAiApps[def.PackageName] = pref;
            }
            bool? installed = WindowsAiAppMonitor.IsInstalled(def.PackageName);
            string currentText = installed switch
            {
                true => "Current: Installed for current user",
                false => "Current: Not installed",
                _ => "Current: probe failed (PowerShell missing?)"
            };
            WindowsAiAppRowsCollection.Add(new WindowsAiAppRow(def, pref, currentText, OnRowPrefChanged));
        }
    }

    /// <summary>
    /// Reads a DWORD policy value without elevation. Reading HKLM Policies
    /// keys doesn't require admin -- only writing does. Returns null if the
    /// key/value doesn't exist or isn't a DWORD.
    /// </summary>
    private static int? ReadPolicyDword(GamerGuardian.Models.PolicyOverride po)
    {
        try
        {
            using var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(po.PolicyKey, writable: false);
            return k?.GetValue(po.PolicyValue) as int?;
        }
        catch { return null; }
    }

    private void UpdatePresetRadio()
    {
        // Default preset = every installed row at Default.
        // Gaming preset = every installed row with a RecommendedTarget at that target.
        //   Non-recommended rows can be anywhere — the preset doesn't manage them.
        // The two are mutually exclusive in practice (recommended targets aren't Default).
        bool matchesDefault = ServiceRows.All(r =>
            !r.IsInstalled || r.DesiredDefault);
        bool matchesGaming = !matchesDefault && ServiceRows
            .Where(r => r.IsInstalled && r.Definition.RecommendedTarget.HasValue)
            .All(r => GetDesired(r) == r.Definition.RecommendedTarget!.Value);

        _suppressPresetEvents = true;
        try
        {
            _services.ServicesPresetGaming.IsChecked = matchesGaming;
            _services.ServicesPresetDefault.IsChecked = matchesDefault;
        }
        finally { _suppressPresetEvents = false; }
    }

    private static ServiceTargetState GetDesired(ServiceRow r) =>
        r.DesiredDisabled ? ServiceTargetState.Disabled
        : r.DesiredManual ? ServiceTargetState.Manual
        : ServiceTargetState.Default;

    internal void ServicesPresetGaming_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressPresetEvents) return;
        ApplyServicesPreset(useRecommended: true);
    }

    internal void ServicesPresetDefault_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressPresetEvents) return;
        ApplyServicesPreset(useRecommended: false);
    }

    private void ApplyServicesPreset(bool useRecommended)
    {
        foreach (var row in ServiceRows.Where(r => r.IsInstalled))
        {
            if (useRecommended)
            {
                // Only flip rows with a RecommendedTarget. Leave others alone.
                if (row.Definition.RecommendedTarget is { } target)
                    row.SetDesiredFromPreset(target);
            }
            else
            {
                row.SetDesiredFromPreset(ServiceTargetState.Default);
            }
        }
        // Preset is staged like any other preference toggle -- the actual write
        // to disk and re-apply happens on the user's next Apply / Save & close.
        ChangeLogger.LogPreferenceChange(
            "Windows services preset",
            "Want",
            "(custom)",
            useRecommended ? "Gaming optimized" : "Default");
    }

    private static string OnOffText(bool? state) =>
        state is null ? "not detected" : (state.Value ? "Enabled" : "Disabled");

    private static string GamingDefaultText(bool? state) =>
        state is null ? "not detected" : (state.Value ? "Gaming-optimized" : "Default");

    private static void SyncIfUnmonitored(ToggleSettingPref pref, Func<bool?> readCurrent)
    {
        if (pref.Monitor) return;
        bool? cur;
        try { cur = readCurrent(); } catch { return; }
        if (cur.HasValue) pref.DesiredOn = cur.Value;
    }

    private void LoadPrivacy()
    {
        PrivacyToggleRows.Clear();
        var g = _draft.Global;

        SyncIfUnmonitored(g.AdvertisingId, AdvertisingIdMonitor.ReadCurrent);
        SyncIfUnmonitored(g.TailoredExperiences, TailoredExperiencesMonitor.ReadCurrent);
        SyncIfUnmonitored(g.Cdp, CdpMonitor.ReadCurrent);
        SyncIfUnmonitored(g.ActivityHistory, ActivityHistoryMonitor.ReadCurrent);
        SyncIfUnmonitored(g.OnlineSpeech, OnlineSpeechMonitor.ReadCurrent);
        SyncIfUnmonitored(g.InkingTyping, InkingTypingMonitor.ReadCurrent);

        PrivacyToggleRows.Add(new GlobalToggleRow(
            name: "Advertising ID",
            description: "Per-user identifier apps use to profile you for ads. Privacy-recommended off.",
            currentText: $"Current: {OnOffText(SafeRead(AdvertisingIdMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.AdvertisingId, groupName: "pv_adid",
            onPrefChanged: OnRowPrefChanged,
            settingId: "privacy.advertisingid"));

        PrivacyToggleRows.Add(new GlobalToggleRow(
            name: "Tailored experiences",
            description: "Lets Windows use your diagnostic data to personalize tips, ads, and recommendations.",
            currentText: $"Current: {OnOffText(SafeRead(TailoredExperiencesMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.TailoredExperiences, groupName: "pv_tailored",
            onPrefChanged: OnRowPrefChanged,
            settingId: "privacy.tailoredexp"));

        PrivacyToggleRows.Add(new GlobalToggleRow(
            name: "Cross-Device Platform (CDP)",
            description: "\"Continue experiences on this device\" / shared-experiences subsystem. Off via policy; reasserted after feature updates.",
            currentText: $"Current: {GamingDefaultText(SafeRead(CdpMonitor.ReadCurrent))}",
            defaultText: "Default: On    Gaming: Disabled",
            onLabel: "Gaming", offLabel: "Default",
            pref: g.Cdp, groupName: "pv_cdp",
            onPrefChanged: OnRowPrefChanged,
            settingId: "privacy.cdp"));

        PrivacyToggleRows.Add(new GlobalToggleRow(
            name: "Activity History / Timeline",
            description: "Collection and publishing of your activity feed. Off via policy; reasserted after feature updates.",
            currentText: $"Current: {GamingDefaultText(SafeRead(ActivityHistoryMonitor.ReadCurrent))}",
            defaultText: "Default: On    Gaming: Disabled",
            onLabel: "Gaming", offLabel: "Default",
            pref: g.ActivityHistory, groupName: "pv_activity",
            onPrefChanged: OnRowPrefChanged,
            settingId: "privacy.activityhistory"));

        PrivacyToggleRows.Add(new GlobalToggleRow(
            name: "Online (cloud) speech recognition",
            description: "When on, Windows sends your voice audio to Microsoft for processing. Offline recognition / Voice Access still work with this off.",
            currentText: $"Current: {OnOffText(SafeRead(OnlineSpeechMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.OnlineSpeech, groupName: "pv_speech",
            onPrefChanged: OnRowPrefChanged,
            settingId: "privacy.speech"));

        PrivacyToggleRows.Add(new GlobalToggleRow(
            name: "Inking & typing personalization",
            description: "Windows building (and uploading) a personal dictionary from your handwriting and contacts. Distinct from the Windows AI tab's typing-insights toggle.",
            currentText: $"Current: {OnOffText(SafeRead(InkingTypingMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.InkingTyping, groupName: "pv_inking",
            onPrefChanged: OnRowPrefChanged,
            settingId: "privacy.inking"));
    }

    private void LoadDebloat()
    {
        DebloatAdsRows.Clear();
        DebloatBackgroundRows.Clear();
        var g = _draft.Global;

        SyncIfUnmonitored(g.SuggestedContent, SuggestedContentMonitor.ReadCurrent);
        SyncIfUnmonitored(g.LockScreenSpotlight, LockScreenSpotlightMonitor.ReadCurrent);
        SyncIfUnmonitored(g.FinishSetupNag, FinishSetupNagMonitor.ReadCurrent);
        SyncIfUnmonitored(g.StartRecommendations, StartRecommendationsMonitor.ReadCurrent);
        SyncIfUnmonitored(g.ExplorerAds, ExplorerAdsMonitor.ReadCurrent);
        SyncIfUnmonitored(g.FeedbackNag, FeedbackNagMonitor.ReadCurrent);
        SyncIfUnmonitored(g.Widgets, WidgetsMonitor.ReadCurrent);
        SyncIfUnmonitored(g.EdgeBackground, EdgeBackgroundMonitor.ReadCurrent);

        // ---- Ads, nags & suggested content (all HKCU, no UAC) ----
        DebloatAdsRows.Add(new GlobalToggleRow(
            name: "Suggested content & silent app installs",
            description: "Silently-installed promo apps, Start-menu app suggestions, and 'tips & suggestions' cards. Disabled = no ads / no surprise installs.",
            currentText: $"Current: {OnOffText(SafeRead(SuggestedContentMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.SuggestedContent, groupName: "db_suggested",
            onPrefChanged: OnRowPrefChanged,
            settingId: "debloat.suggestedcontent"));

        DebloatAdsRows.Add(new GlobalToggleRow(
            name: "Lock screen tips, fun facts & ads",
            description: "The Windows Spotlight overlay that shows tips and ad-like captions on the lock screen.",
            currentText: $"Current: {OnOffText(SafeRead(LockScreenSpotlightMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.LockScreenSpotlight, groupName: "db_spotlight",
            onPrefChanged: OnRowPrefChanged,
            settingId: "debloat.spotlight"));

        DebloatAdsRows.Add(new GlobalToggleRow(
            name: "\"Finish setting up your device\" nag",
            description: "The post-update prompts to set up OneDrive / a Microsoft account / Microsoft 365.",
            currentText: $"Current: {OnOffText(SafeRead(FinishSetupNagMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.FinishSetupNag, groupName: "db_finishsetup",
            onPrefChanged: OnRowPrefChanged,
            settingId: "debloat.finishsetup"));

        DebloatAdsRows.Add(new GlobalToggleRow(
            name: "Start menu recommendations & recent files",
            description: "AI/Iris app & web suggestions plus recently opened files in the Start 'Recommended' section.",
            currentText: $"Current: {OnOffText(SafeRead(StartRecommendationsMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.StartRecommendations, groupName: "db_startrec",
            onPrefChanged: OnRowPrefChanged,
            settingId: "debloat.startrecommend"));

        DebloatAdsRows.Add(new GlobalToggleRow(
            name: "File Explorer ad banners",
            description: "The OneDrive / Microsoft 365 upsell banners shown in the File Explorer nav pane and status bar.",
            currentText: $"Current: {OnOffText(SafeRead(ExplorerAdsMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.ExplorerAds, groupName: "db_explorerads",
            onPrefChanged: OnRowPrefChanged,
            settingId: "debloat.explorerads"));

        DebloatAdsRows.Add(new GlobalToggleRow(
            name: "Windows feedback request popups",
            description: "The periodic 'rate your experience' dialogs. Disabled = Windows never asks (Feedback Hub still opens manually).",
            currentText: $"Current: {OnOffText(SafeRead(FeedbackNagMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.FeedbackNag, groupName: "db_feedback",
            onPrefChanged: OnRowPrefChanged,
            settingId: "debloat.feedback"));

        // ---- Background bloat (HKLM policy -> one UAC prompt to apply) ----
        DebloatBackgroundRows.Add(new GlobalToggleRow(
            name: "Widgets / News and interests",
            description: "The left-edge weather button and its background MSN web feed. Disabling needs admin (machine-wide policy) and removes the taskbar button.",
            currentText: $"Current: {OnOffText(SafeRead(WidgetsMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.Widgets, groupName: "db_widgets",
            onPrefChanged: OnRowPrefChanged,
            settingId: "debloat.widgets"));

        DebloatBackgroundRows.Add(new GlobalToggleRow(
            name: "Edge startup boost & background mode",
            description: "Keeps Edge resident from boot and after you close it. Disabling frees idle RAM/CPU; Edge still opens on demand and WebView2 keeps working. Needs admin.",
            currentText: $"Current: {OnOffText(SafeRead(EdgeBackgroundMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.EdgeBackground, groupName: "db_edge",
            onPrefChanged: OnRowPrefChanged,
            settingId: "debloat.edge"));
    }

    private void LoadNetwork()
    {
        NetworkToggleRows.Clear();
        var g = _draft.Global;
        SyncIfUnmonitored(g.NetworkThrottling, NetworkThrottlingMonitor.ReadCurrent);
        SyncIfUnmonitored(g.Nagle, NagleMonitor.ReadCurrent);
        SyncIfUnmonitored(g.NicPower, NicPowerMonitor.ReadCurrent);

        NetworkToggleRows.Add(new GlobalToggleRow(
            name: "Network Throttling",
            description: "Windows' MMCSS rate-limits network packets during multimedia tasks. Disabling removes that pacing for steadier online-game netcode. Safe, well-established tweak.",
            currentText: $"Current: {GamingDefaultText(SafeRead(NetworkThrottlingMonitor.ReadCurrent))}",
            defaultText: "Default: 10    Gaming: Disabled",
            onLabel: "Gaming", offLabel: "Default",
            pref: g.NetworkThrottling, groupName: "netthr",
            onPrefChanged: OnRowPrefChanged,
            settingId: "netthrottle"));

        NetworkToggleRows.Add(new GlobalToggleRow(
            name: "Nagle's algorithm (TCP no-delay)",
            description: "Disables Nagle packet batching on every active adapter for lower latency in online games. Contested: the benefit varies by hardware and can make some connections worse -- see Learn more.",
            currentText: $"Current: {GamingDefaultText(SafeRead(NagleMonitor.ReadCurrent))}",
            defaultText: "Default: On    Gaming: Disabled",
            onLabel: "Gaming", offLabel: "Default",
            pref: g.Nagle, groupName: "net_nagle",
            onPrefChanged: OnRowPrefChanged,
            settingId: "network.nagle"));

        NetworkToggleRows.Add(new GlobalToggleRow(
            name: "NIC power management",
            description: "Disables 'Allow the computer to turn off this device to save power' on every active adapter, so the NIC never sleeps. Contested per-hardware; needs a reboot. Leave Default on laptops on battery.",
            currentText: $"Current: {GamingDefaultText(SafeRead(NicPowerMonitor.ReadCurrent))}",
            defaultText: "Default: On    Gaming: Disabled",
            onLabel: "Gaming", offLabel: "Default",
            requiresReboot: true,
            pref: g.NicPower, groupName: "net_nicpower",
            onPrefChanged: OnRowPrefChanged,
            settingId: "network.nicpower"));
    }

    private void LoadGlobals()
    {
        GlobalToggleRows.Clear();
        var g = _draft.Global;

        // For settings the user hasn't opted into monitoring, default Want to Current
        // so the radios reflect the actual system state instead of a "ghost" recommendation.
        SyncIfUnmonitored(g.GameMode, GameModeMonitor.ReadCurrent);
        SyncIfUnmonitored(g.GameDvr, GameDvrMonitor.ReadCurrent);
        SyncIfUnmonitored(g.Hags, HagsMonitor.ReadCurrent);
        SyncIfUnmonitored(g.MemoryIntegrity, MemoryIntegrityMonitor.ReadCurrent);
        SyncIfUnmonitored(g.Vbs, VbsMonitor.ReadCurrent);
        SyncIfUnmonitored(g.SystemResponsiveness, SystemResponsivenessMonitor.ReadCurrent);
        SyncIfUnmonitored(g.UsbSelectiveSuspend, UsbSelectiveSuspendMonitor.ReadCurrent);
        SyncIfUnmonitored(g.GamesTaskProfile, GamesTaskProfileMonitor.ReadCurrent);
        SyncIfUnmonitored(g.MousePrecision, MousePrecisionMonitor.ReadCurrent);
        SyncIfUnmonitored(g.FullscreenOptimizations, FullscreenOptimizationsMonitor.ReadCurrent);
        SyncIfUnmonitored(g.Vrr, VrrMonitor.ReadCurrent);
        SyncIfUnmonitored(g.FastStartup, FastStartupMonitor.ReadCurrent);
        SyncIfUnmonitored(g.VisualFx, VisualEffectsMonitor.ReadCurrent);

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Game Mode",
            description: "Tells Windows to prioritize the running game and suppress background work.",
            currentText: $"Current: {OnOffText(SafeRead(GameModeMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.GameMode, groupName: "gm",
            onPrefChanged: OnRowPrefChanged,
            settingId: "gamemode"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Game DVR background recording",
            description: "Always-on game capture. Costs CPU/GPU during gameplay; off is gaming-recommended.",
            currentText: $"Current: {OnOffText(SafeRead(GameDvrMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.GameDvr, groupName: "dvr",
            onPrefChanged: OnRowPrefChanged,
            settingId: "gamedvr"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Hardware-accelerated GPU Scheduling (HAGS)",
            description: "Lets the GPU manage its own command queue. Lower latency on supported GPUs.",
            currentText: $"Current: {OnOffText(SafeRead(HagsMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled (Win11)",
            onLabel: "Enabled", offLabel: "Disabled",
            requiresReboot: true,
            pref: g.Hags, groupName: "hags",
            onPrefChanged: OnRowPrefChanged,
            settingId: "hags"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Memory Integrity / VBS (Core Isolation)",
            description: "Hypervisor-Enforced Code Integrity. Disabling recovers ~5–15% gaming perf but reduces malware protection.",
            currentText: $"Current: {OnOffText(SafeRead(MemoryIntegrityMonitor.ReadCurrent))}"
                + (MemoryIntegrityMonitor.DefersToVbs(_draft) ? " — overridden by the VBS full-stack toggle below" : ""),
            defaultText: "Default: Enabled (Win11)",
            onLabel: "Enabled", offLabel: "Disabled",
            requiresReboot: true,
            pref: g.MemoryIntegrity, groupName: "memint",
            onPrefChanged: OnRowPrefChanged,
            settingId: "memintegrity"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Virtualization-Based Security (full stack)",
            description: "Superset of Memory Integrity: disables ALL VBS scenarios (HVCI, Credential Guard, System Guard, kernel stack protection) plus the policy keys Windows uses to re-enable them. Breaks Valorant — Vanguard requires Memory Integrity.",
            currentText: $"Current: {VbsMonitor.ReadCurrentText()}",
            defaultText: "Default: Enabled (Win11)",
            onLabel: "Enabled", offLabel: "Disabled",
            requiresReboot: true,
            pref: g.Vbs, groupName: "vbs",
            onPrefChanged: OnRowPrefChanged,
            settingId: "vbs"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "System Responsiveness",
            description: "CPU percentage Windows reserves for non-multimedia tasks. Lower frees CPU for games.",
            currentText: $"Current: {GamingDefaultText(SafeRead(SystemResponsivenessMonitor.ReadCurrent))}",
            defaultText: "Default: 20    Gaming: 10",
            onLabel: "Gaming", offLabel: "Default",
            requiresReboot: true,
            pref: g.SystemResponsiveness, groupName: "sysresp",
            onPrefChanged: OnRowPrefChanged,
            settingId: "sysresponse"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "USB Selective Suspend (global)",
            description: "Lets Windows suspend idle USB devices. Disabling keeps mice/keyboards/headsets always responsive.",
            currentText: $"Current: {GamingDefaultText(SafeRead(UsbSelectiveSuspendMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled    Gaming: Disabled",
            onLabel: "Gaming", offLabel: "Default",
            requiresReboot: true,
            pref: g.UsbSelectiveSuspend, groupName: "usbsus",
            onPrefChanged: OnRowPrefChanged,
            settingId: "usbsuspend"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Games multimedia task profile",
            description: "Priority + scheduling values for processes registered with the Games multimedia class.",
            currentText: $"Current: {GamingDefaultText(SafeRead(GamesTaskProfileMonitor.ReadCurrent))}",
            defaultText: "Default: standard    Gaming: boosted",
            onLabel: "Gaming", offLabel: "Default",
            pref: g.GamesTaskProfile, groupName: "gtask",
            onPrefChanged: OnRowPrefChanged,
            settingId: "gamestask"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Mouse \"Enhance pointer precision\"",
            description: "Acceleration curve applied to mouse movement. Most gamers want this off for consistent aim.",
            currentText: $"Current: {OnOffText(SafeRead(MousePrecisionMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.MousePrecision, groupName: "mp",
            onPrefChanged: OnRowPrefChanged,
            settingId: "mouseaccel"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Fullscreen optimizations (global)",
            description: "Borderless-windowed compositing layer. Generally fine; some titles prefer it off.",
            currentText: $"Current: {OnOffText(SafeRead(FullscreenOptimizationsMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.FullscreenOptimizations, groupName: "fso",
            onPrefChanged: OnRowPrefChanged,
            settingId: "fso"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Variable Refresh Rate (DirectX)",
            description: "G-Sync / FreeSync compatibility flag (Settings → Display → Graphics). Not the same as Dynamic Refresh Rate (DRR) in Advanced Display.",
            currentText: $"Current: {OnOffText(SafeRead(VrrMonitor.ReadCurrent))}",
            defaultText: "Default: not set",
            onLabel: "Enabled", offLabel: "Disabled",
            pref: g.Vrr, groupName: "vrr",
            onPrefChanged: OnRowPrefChanged,
            settingId: "vrr"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Fast Startup (hybrid boot)",
            description: "Saves the kernel session to the hiberfile so 'shutdown' isn't a true cold boot. Off makes every shutdown a clean boot; fixes a class of stale driver/USB state issues.",
            currentText: $"Current: {GamingDefaultText(SafeRead(FastStartupMonitor.ReadCurrent))}",
            defaultText: "Default: On    Gaming: Disabled",
            onLabel: "Gaming", offLabel: "Default",
            requiresReboot: true,
            pref: g.FastStartup, groupName: "faststartup",
            onPrefChanged: OnRowPrefChanged,
            settingId: "faststartup"));

        GlobalToggleRows.Add(new GlobalToggleRow(
            name: "Visual effects (best performance)",
            description: "Windows UI animations and effects. 'Best performance' disables them for the snappiest desktop. Full per-effect changes apply after sign-out.",
            currentText: $"Current: {GamingDefaultText(SafeRead(VisualEffectsMonitor.ReadCurrent))}",
            defaultText: "Default: Let Windows choose    Gaming: Best performance",
            onLabel: "Gaming", offLabel: "Default",
            pref: g.VisualFx, groupName: "visualfx",
            onPrefChanged: OnRowPrefChanged,
            settingId: "visualfx"));

        var planNames = PowerPlanMonitor.ListAvailablePlans();
        var active = SafeRunGuid(PowerPlanMonitor.GetActivePlan);
        var activeName = active is not null && planNames.TryGetValue(active.Value, out var name) ? name : "unknown";
        _cpuPower.PowerPlanCurrentText.Text = $"Current: {activeName}";
        // CPU-aware recommendation: the prebuilt plan the catalog picks for this
        // CPU (Balanced on modern CPUs -- never blindly High Performance), shown
        // by its installed plan name so it matches the dropdown. Mirrors what the
        // one-click preset applies.
        var planRecipe = CpuTuneCatalog.Resolve(CpuDetector.Current);
        var recPlanGuid = PowerPlanMonitor.ToGuid(planRecipe.RecommendedPrebuilt);
        var recPlanName = planNames.TryGetValue(recPlanGuid, out var rpn)
            ? rpn
            : planRecipe.RecommendedPrebuilt.ToString();
        _cpuPower.PowerPlanRecommendedText.Text = $"Recommended: {recPlanName}";
        _cpuPower.PowerPlanMonitorCheck.IsChecked = g.PowerPlan.Monitor;
        _cpuPower.PowerPlanAutoApplyCheck.IsChecked = g.PowerPlan.AutoApply;

        var planItems = planNames
            .OrderBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new PowerPlanItem(kv.Key, kv.Value))
            .ToList();
        _cpuPower.PowerPlanCombo.ItemsSource = planItems;
        _cpuPower.PowerPlanCombo.DisplayMemberPath = nameof(PowerPlanItem.Name);

        // Preselect priority: the user's own explicit pick, else the CPU-aware
        // recommended prebuilt (Balanced) so the "Want" dropdown agrees with the
        // "Recommended:" hint instead of defaulting to High Performance, else the
        // currently-active plan. Suppress the handler while we set this: reflecting
        // saved/recommended state into the combo is not a user edit, so it must not
        // stage a phantom pending change or log a PREF line on every open.
        Guid? explicitPick = !string.IsNullOrEmpty(g.PowerPlan.DesiredGuid)
            && Guid.TryParse(g.PowerPlan.DesiredGuid, out var eg) ? eg : null;
        var preselect = explicitPick
            ?? (planItems.Any(p => p.Guid == recPlanGuid) ? recPlanGuid : (Guid?)null)
            ?? active;
        _loadingPowerPlan = true;
        try
        {
            _cpuPower.PowerPlanCombo.SelectedItem = planItems.FirstOrDefault(p => p.Guid == preselect)
                ?? planItems.FirstOrDefault();
        }
        finally { _loadingPowerPlan = false; }
    }

    private static bool? SafeRead(Func<bool?> f)
    {
        try { return f(); } catch { return null; }
    }

    private static Guid? SafeRunGuid(Func<Guid> f)
    {
        try { var g = f(); return g == Guid.Empty ? null : g; } catch { return null; }
    }

    private void LoadDisplays()
    {
        DisplayRows.Clear();
        var active = DisplayHelper.EnumerateActiveDisplays();
        foreach (var d in active)
        {
            var pref = DisplayPreferenceResolver.Resolve(_draft, d, active);
            var hdr = SafeRead(() => HdrMonitor.ReadHdrState(d) is { } s ? (bool?)(s.Supported && s.Enabled) : null);
            var drr = DrrMonitor.ReadState(d); // internally guarded, won't throw
            bool drrSupported = drr is { } ds && ds.Supported;
            var refresh = string.IsNullOrEmpty(d.GdiDeviceName) ? null : RefreshRateMonitor.GetCurrentRefresh(d.GdiDeviceName);
            uint maxHz = refresh is null ? 0 : RefreshRateMonitor.GetMaxSupportedRefresh(d.GdiDeviceName, refresh.Value.Width, refresh.Value.Height);
            var rates = refresh is null
                ? new List<uint>()
                : RefreshRateMonitor.GetSupportedRefreshRates(d.GdiDeviceName, refresh.Value.Width, refresh.Value.Height).ToList();
            // Keep the saved Fixed target selectable even if the panel is
            // momentarily capped low (e.g. a flaky driver only offering 64 Hz),
            // so opening Settings during a glitch can't silently drop it.
            if (pref.RefreshRate.FixedHz is { } fixedHz && fixedHz > 0 && !rates.Contains(fixedHz))
            {
                rates.Add(fixedHz);
                rates.Sort();
            }
            var resolutions = string.IsNullOrEmpty(d.GdiDeviceName) ? Array.Empty<(uint, uint)>() : ResolutionMonitor.ListSupported(d.GdiDeviceName);
            var resStrings = resolutions.Select(r => $"{r.Item1}x{r.Item2}").ToList();
            var current = ResolutionMonitor.GetCurrent(d.GdiDeviceName);

            var status = string.Format(CultureInfo.InvariantCulture,
                "Now — HDR: {0}    Refresh: {1}    Resolution: {2}    DRR: {3}",
                hdr is null ? "unknown" : (hdr.Value ? "On" : "Off"),
                refresh is null ? "unknown" : refresh.Value.Hz + " Hz" + (maxHz > 0 ? $" (max {maxHz})" : ""),
                current is null ? "unknown" : $"{current.Value.Width}x{current.Value.Height}",
                drr is null ? "unknown" : (!drr.Value.Supported ? "n/a" : (drr.Value.Enabled ? "On" : "Off")));

            DisplayRows.Add(new DisplayRow(d.StableKey, d.DisplayLabel, status, pref, rates, resStrings, drrSupported));
        }
    }

    private static string GetVersionDisplay()
    {
        var raw = GetSemverString();
        return App.IsDevBuild() ? $"v{StripPrerelease(raw)} (dev)" : $"v{raw}";
    }

    private static string StripPrerelease(string semver)
    {
        var dash = semver.IndexOf('-');
        return dash > 0 ? semver[..dash] : semver;
    }

    private static string GetSemverString()
    {
        var asm = typeof(App).Assembly;
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(info))
        {
            var idx = info.IndexOf('+');
            return idx > 0 ? info[..idx] : info;
        }
        var v = asm.GetName().Version;
        return v != null ? $"{v.Major}.{v.Minor}.{v.Build}" : "0.0.0";
    }

    private static string GetVersionTooltip()
    {
        var asm = typeof(App).Assembly;
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "(unknown)";
        var fileV = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "(unknown)";
        var rt = Environment.Version.ToString();
#if DEBUG
        var build = "Debug";
#else
        var build = App.IsDevBuild() ? "Release (dev)" : "Release";
#endif
        return $"Informational: {info}\nFile: {fileV}\n.NET: {rt}\nBuild: {build}\n\nClick to open releases page";
    }

    internal void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_general.ThemeCombo.SelectedItem is AppThemeChoice c)
            ThemeService.Apply(c);
    }

    internal void PowerPlanCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Ignore the selection we set ourselves while loading — only a real user
        // change should stage a pending edit.
        if (_loadingPowerPlan) return;
        if (_cpuPower.PowerPlanCombo.SelectedItem is not PowerPlanItem pi) return;
        var oldGuid = _draft.Global.PowerPlan.DesiredGuid;
        var oldName = _draft.Global.PowerPlan.DesiredName;
        if (oldGuid == pi.Guid.ToString()) return;
        _draft.Global.PowerPlan.DesiredGuid = pi.Guid.ToString();
        _draft.Global.PowerPlan.DesiredName = pi.Name;
        ChangeLogger.LogPreferenceChange("Power plan", "Want",
            oldName ?? oldGuid ?? "(unset)", pi.Name);
        _pendingCount++;
        UpdatePendingStatus();
    }

    internal void PowerPlanMonitorCheck_Changed(object sender, RoutedEventArgs e)
    {
        var v = _cpuPower.PowerPlanMonitorCheck.IsChecked == true;
        if (_draft.Global.PowerPlan.Monitor == v) return;
        var before = _draft.Global.PowerPlan.Monitor;
        _draft.Global.PowerPlan.Monitor = v;
        ChangeLogger.LogPreferenceChange("Power plan", "Monitor", before.ToString(), v.ToString());
        _pendingCount++;
        UpdatePendingStatus();
    }

    internal void PowerPlanAutoApplyCheck_Changed(object sender, RoutedEventArgs e)
    {
        var v = _cpuPower.PowerPlanAutoApplyCheck.IsChecked == true;
        if (_draft.Global.PowerPlan.AutoApply == v) return;
        var before = _draft.Global.PowerPlan.AutoApply;
        _draft.Global.PowerPlan.AutoApply = v;
        ChangeLogger.LogPreferenceChange("Power plan", "AutoApply", before.ToString(), v.ToString());
        _pendingCount++;
        UpdatePendingStatus();
    }

    internal void OpenChangeLogButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = ChangeLogger.LogPath;
            if (!System.IO.File.Exists(path))
            {
                var dir = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(path, "(no changes have been applied yet)\n");
            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
        catch { }
    }

    internal async void CheckUpdatesNowButton_Click(object sender, RoutedEventArgs e)
    {
#if BETA
        // The whole update path is compiled out of a beta build. The button and its
        // XAML stay exactly as they are -- only the body changes -- so there is no
        // orphaned Click target and no unused handler, and the binary genuinely
        // contains no call into UpdateService from here.
        await Task.CompletedTask;
        System.Windows.MessageBox.Show(
            this,
            "Updates are disabled in beta builds.",
            "GamerGuardian",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
#else
        // Dev builds must never self-update. The startup check has always been
        // gated on this (App.OnStartup), but this manual path was not: clicking
        // "Check now" in a dev build would download the newest *stable* installer
        // and launch it over the running dev build. Same guard, both paths.
        //
        // Both guards coexist deliberately: BETA compiles the update path out of
        // the binary entirely, while this runtime check covers the dev builds that
        // are still compiled with the update path present.
        if (App.IsDevBuild())
        {
            System.Windows.MessageBox.Show(
                this,
                $"This is a development build (v{UpdateService.CurrentSemver()}). "
                + "Automatic updates are disabled so it can't replace itself with a release build.",
                "GamerGuardian",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        var btn = _general.CheckUpdatesNowButton;
        var prev = btn.Content;
        btn.IsEnabled = false;
        btn.Content = "Checking…";
        try
        {
            var info = await UpdateService.CheckLatestAsync();
            if (info is null)
            {
                System.Windows.MessageBox.Show(
                    $"You're on the latest version (v{UpdateService.CurrentSemver()}).",
                    "GamerGuardian",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            else
            {
                var win = new UpdateAvailableWindow(info, _store, _exitApp);
                win.Show();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Couldn't reach the update server.\n\n{ex.Message}",
                "GamerGuardian",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
        finally
        {
            btn.Content = prev;
            btn.IsEnabled = true;
        }
#endif
    }

    /// <summary>Guards against re-entrant Apply / Save&amp;close while one is in flight.
    /// Without this, async void handlers let a second click race with the first --
    /// each fires its own UAC stream and they can interleave.</summary>
    private bool _applyInFlight;

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        await ApplyChangesAsync(closeAfter: false);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // Pure-close path: no draft edits since the window opened (or since the
        // last Apply), so there's nothing to apply. Just close. Without this,
        // Save&close re-runs the drift+apply pass and re-prompts UAC for any
        // setting Windows reverted between the two clicks -- which surprises
        // users who already approved everything via Apply.
        if (_pendingCount == 0)
        {
            Close();
            return;
        }
        await ApplyChangesAsync(closeAfter: true);
    }

    private void SetButtonsEnabled(bool enabled)
    {
        try
        {
            // Found via the named template parts in SettingsWindow.xaml.
            // Cancel stays enabled so the user can always escape a hang.
            ApplyButton.IsEnabled = enabled;
            SaveButton.IsEnabled = enabled;
        }
        catch { /* XAML controls may not be ready during very early calls */ }
    }

    private async Task ApplyChangesAsync(bool closeAfter)
    {
        if (_applyInFlight) return;
        _applyInFlight = true;
        SetButtonsEnabled(false);
        try
        {
            await ApplyChangesCoreAsync(closeAfter);
        }
        finally
        {
            _applyInFlight = false;
            SetButtonsEnabled(true);
        }
    }

    private async Task ApplyChangesCoreAsync(bool closeAfter)
    {
        // 1. Flush every form field into the draft (the rows already wrote to
        //    draft on each toggle; this picks up the controls that don't have
        //    explicit handlers, like LaunchAtStartup checkbox + PollSeconds).
        PersistFormToDraft();

        // 2. Commit draft -> live config and persist. From this point on the
        //    background MonitorService will see the new preferences on its
        //    next tick (or this Apply pass, whichever is first).
        AppConfigCloner.CopyInto(_draft, _config);
        _store.Save(_config);
        StartupRegistration.Sync(_config.LaunchAtStartup);

        // 3. Compute drift against the now-committed config and apply.
        var drifted = new List<DriftItem>();
        foreach (var m in _monitors)
        {
            try { drifted.AddRange(m.CheckDrift(_config)); }
            catch { /* per-monitor failures shouldn't break Apply */ }
        }

        var sessionId = ChangeApplier.NewSessionId();
        var results = await ChangeApplier.ApplyAndVerifyAsync(
            drifted, _monitors, _config, source: "manual", sessionId: sessionId);

        if (results.Count > 0)
        {
            ChangeLogger.LogApplyResults(results, "manual");
            // Seed MonitorService's last-verified table so the very next
            // background tick can detect external resets without a one-cycle blind spot.
            _monitorService?.RecordVerifiedApplies(results);
        }

        Saved?.Invoke();

        // 4. Re-base the draft from the now-committed config and rebuild rows
        //    so the UI reflects the freshly-applied state. Resets pending count.
        RebaseDraftFromConfig();
        LoadGlobals();
        LoadDisplays();
        LoadServices();
        LoadScheduledTasks();
        LoadWindowsAi();
        LoadPrivacy();
        LoadDebloat();
        LoadNetwork();
        LoadCpuTabs();
        UpdatePendingStatus();

        if (results.Count > 0)
        {
            var win = new ApplyResultsWindow(results) { Owner = this };
            win.Show();
        }
        else if (!closeAfter)
        {
            // Nothing drifted: every monitored setting already matches its preference.
            // Surface this explicitly so Apply isn't a silent no-op when the user
            // expected it to do something. (Skipped on Save & close — the close
            // itself is the feedback.)
            System.Windows.MessageBox.Show(
                this,
                "No changes to apply -- every monitored setting already matches your preference.",
                "GamerGuardian",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        // Surface a single restart prompt for any reboot-requiring change that
        // actually landed this pass. Deferred to the end on purpose: ApplyAndVerify
        // ran every drifted setting first, so a bulk apply (e.g. the Extreme preset)
        // flags reboot across the whole batch and prompts once here, not per setting.
        // RebootPrompt shows an UNOWNED window so it survives the Close() below —
        // the ApplyResultsWindow is owned by this window and would be destroyed with
        // it on Save & close, which is why bulk applies used to reboot-flag nothing.
        var rebootDescriptions = results
            .Where(r => r.RequiresReboot && r.Verified)
            .Select(r => r.Description)
            .ToList();
        if (rebootDescriptions.Count > 0)
            RebootPrompt.Show(rebootDescriptions);

        if (closeAfter) Close();
    }

    /// <summary>
    /// Flushes every form control (and any row WriteTo/WriteBack hooks) into
    /// the draft. Row property setters already mutate the draft directly, so
    /// this is just for the form-level controls that don't have per-change
    /// handlers (LaunchAtStartup, PollSeconds, etc.).
    /// </summary>
    private void PersistFormToDraft()
    {
        _draft.LaunchAtStartup = _general.LaunchAtStartupCheck.IsChecked == true;
        _draft.CheckForUpdatesOnStartup = _general.CheckForUpdatesCheck.IsChecked == true;
        if (_general.PollSecondsBox.Value is double pv && pv >= 5)
            _draft.PollIntervalSeconds = (int)pv;
        if (_general.ThemeCombo.SelectedItem is AppThemeChoice tc)
            _draft.Theme = tc;

        _draft.Global.PowerPlan.Monitor = _cpuPower.PowerPlanMonitorCheck.IsChecked == true;
        _draft.Global.PowerPlan.AutoApply = _cpuPower.PowerPlanAutoApplyCheck.IsChecked == true;
        if (_cpuPower.PowerPlanCombo.SelectedItem is PowerPlanItem pi)
        {
            _draft.Global.PowerPlan.DesiredGuid = pi.Guid.ToString();
            _draft.Global.PowerPlan.DesiredName = pi.Name;
        }

        foreach (var row in GlobalToggleRows) row.WriteBack();
        foreach (var row in DisplayRows) row.WriteTo(_draft);
        foreach (var row in ServiceRows) row.WriteBack();
    }

    private bool _suppressSaveOnClose = false;

    /// <summary>
    /// Redirects minimize (the '-' button or Win+Down) to a close: the window
    /// destroys, taskbar entry disappears, app stays in the tray. Reopen via
    /// double-click on the tray icon or the tray's Settings menu item.
    /// Without this, minimize would just shrink to a taskbar entry, which is
    /// the wrong UX for a tray app.
    /// </summary>
    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized)
        {
            Close();
        }
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        // Closing the window without explicitly clicking Apply / Save & close
        // discards the draft. Previously this path silently persisted form state
        // to disk -- that's exactly the "I clicked a thing, it applied without
        // asking" behavior we're fixing in v0.1.38. If the user wanted these
        // changes kept they would have clicked Apply or Save & close.
        if (_pendingCount > 0 && !_suppressSaveOnClose)
        {
            ChangeLogger.LogPreferenceChange(
                "Settings window",
                "Closed",
                $"{_pendingCount} pending change(s)",
                "discarded (closed without Apply)");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        try
        {
            // Unsubscribe Status from the monitor first: it holds handlers on a
            // long-lived service, so leaving them attached would keep this whole
            // window graph alive after close -- the exact leak shape the app's
            // memory hygiene exists to prevent.
            _status.Detach();

            // Drop the navigation content and the views' own references so the
            // visual tree is reachable for collection.
            MainNav.ReplaceContent(null!, null);
            _general.Owner = null;
            _services.Owner = null;
            _cpuPower.Owner = null;

            _windowsAi.WindowsAiRows.ItemsSource = null;
            _windowsAi.WindowsAiAppRows.ItemsSource = null;
            _display.DisplaysList.ItemsSource = null;
            _gaming.GlobalTogglesList.ItemsSource = null;
            _telemetry.PrivacyTogglesList.ItemsSource = null;
            _debloat.DebloatAdsList.ItemsSource = null;
            _debloat.DebloatBackgroundList.ItemsSource = null;
            _network.NetworkTogglesList.ItemsSource = null;
            _cpuPower.PowerTogglesList.ItemsSource = null;
            _services.ServicesList.ItemsSource = null;
            _services.ScheduledTasksList.ItemsSource = null;
            DisplayRows.Clear();
            GlobalToggleRows.Clear();
            PrivacyToggleRows.Clear();
            DebloatAdsRows.Clear();
            DebloatBackgroundRows.Clear();
            NetworkToggleRows.Clear();
            PowerToggleRows.Clear();
            ServiceRows.Clear();
            ScheduledTaskRows.Clear();
        }
        catch { }
    }

    private void OnRowPrefChanged(string settingName, string field, string before, string after)
    {
        // Row setters mutate the draft (their _pref reference points at a
        // draft.Services / draft.Global / draft.Displays entry). We never write
        // to _store here -- that happens only in ApplyChangesAsync.
        ChangeLogger.LogPreferenceChange(settingName, field, before, after);
        _pendingCount++;
        UpdatePendingStatus();
    }

    /// <summary>
    /// One-click "use the gaming preset" button. Mutates the draft (not the
    /// live config) via <see cref="RecommendedPreset.ApplyToDraft"/>, bumps
    /// the pending count by the number of settings actually changed (so
    /// Save&amp;close knows there's work and doesn't short-circuit), rebuilds
    /// the row collections so the UI reflects the new draft, and pops a
    /// summary dialog telling the user what just got staged.
    ///
    /// <para>Idempotent: running it a second time when everything's already
    /// in the recommended state stages zero changes and shows the
    /// "already correct" message. Designed so a future GamerGuardian release
    /// that adds new settings to the preset can be picked up by the user
    /// re-clicking this button -- only the new deltas land.</para>
    /// </summary>
    internal void ApplyRecommendedPresetButton_Click(object sender, RoutedEventArgs e) =>
        RunPreset("Recommended preset", () => RecommendedPreset.ApplyToDraft(_draft));

    internal void ApplyExtremePresetButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = System.Windows.MessageBox.Show(this,
            "Apply EXTREME staging will turn on every gaming tweak GamerGuardian knows -- "
            + "including disabling Memory Integrity / VBS and the contested Nagle / NIC tweaks -- "
            + "and enable Monitor + Auto-apply for every setting.\n\n"
            + "Disabling Memory Integrity / VBS weakens kernel-driver malware protection and breaks "
            + "games whose anti-cheat requires it (Valorant / Vanguard). Several changes need a reboot.\n\n"
            + "Nothing is written yet -- this only stages the changes for you to review, then Apply / "
            + "Save & close. Continue?",
            "GamerGuardian -- Apply extreme",
            System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Warning);
        if (confirm != System.Windows.MessageBoxResult.OK) return;

        RunPreset("Extreme preset", () => RecommendedPreset.ApplyExtremeToDraft(_draft));
    }

    internal void ResetToDefaultsButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = System.Windows.MessageBox.Show(this,
            "Reset all to defaults will stage every setting back to its Windows default and turn "
            + "Monitor + Auto-apply off.\n\n"
            + "If you then Apply, this restores Windows' shipped behavior -- which re-enables features "
            + "you may have turned off (Copilot, ads and suggestions, telemetry services) and may need "
            + "a reboot or a UAC prompt.\n\n"
            + "Nothing is written yet -- this only stages the changes for you to review, then Apply / "
            + "Save & close. Continue?",
            "GamerGuardian -- Reset all to defaults",
            System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Warning);
        if (confirm != System.Windows.MessageBoxResult.OK) return;

        RunPreset("Reset to defaults", () => RecommendedPreset.ResetToDefaultsToDraft(_draft));
    }

    /// <summary>
    /// Shared body for the three one-click preset buttons. Runs <paramref name="apply"/>
    /// against the draft, bumps the pending count, rebuilds every row collection so
    /// the UI reflects the mutated draft, and reports what was staged. The row
    /// setters short-circuit on equality, so rebinding doesn't double-log
    /// per-row PREF-STAGE events.
    /// </summary>
    private void RunPreset(string presetName, Func<RecommendedPreset.Result> apply)
    {
        RecommendedPreset.Result result;
        try { result = apply(); }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, $"{presetName} failed: " + ex.Message,
                "GamerGuardian", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        _pendingCount += result.SettingsChanged;
        UpdatePendingStatus();

        LoadGlobals();
        LoadDisplays();
        LoadServices();
        LoadScheduledTasks();
        LoadWindowsAi();
        LoadPrivacy();
        LoadDebloat();
        LoadNetwork();
        LoadCpuTabs();

        if (_general.RecommendedStatusText is not null)
        {
            _general.RecommendedStatusText.Text = result.SettingsChanged == 0
                ? $"{presetName}: all {result.SettingsAlreadyCorrect} setting(s) already in that state. Nothing to do."
                : $"{presetName}: staged {result.SettingsChanged} setting(s); {result.SettingsAlreadyCorrect} already correct. Click Apply or Save & close to commit.";
        }

        var dialogMsg = result.SettingsChanged == 0
            ? $"Your draft already matches the {presetName} across all {result.SettingsAlreadyCorrect} covered setting(s). Nothing to do."
            : $"Staged {result.SettingsChanged} change(s) for the {presetName}. {result.SettingsAlreadyCorrect} setting(s) were already correct and skipped."
              + "\n\nReview the per-tab changes if you want. Click Apply (stay in Settings) or Save & close to commit the apply pass. Click Cancel to discard.";

        System.Windows.MessageBox.Show(this, dialogMsg, $"GamerGuardian -- {presetName}",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    // ---- CPU / Power + Recommended BIOS tabs ------------------------------

    private CpuTuneResult? _cpuRecipe;

    private void LoadPowerToggles()
    {
        PowerToggleRows.Clear();
        var g = _draft.Global;
        SyncIfUnmonitored(g.PowerThrottling, PowerThrottlingMonitor.ReadCurrent);
        PowerToggleRows.Add(new GlobalToggleRow(
            name: "Power Throttling",
            description: "Windows throttles threads it considers background/idle to save power. Off keeps all threads at full clock for sustained performance (desktop-recommended; leave Default on battery).",
            currentText: $"Current: {GamingDefaultText(SafeRead(PowerThrottlingMonitor.ReadCurrent))}",
            defaultText: "Default: On    Gaming: Disabled",
            onLabel: "Gaming", offLabel: "Default",
            pref: g.PowerThrottling, groupName: "powerthrottle",
            onPrefChanged: OnRowPrefChanged,
            settingId: "powerthrottling"));
    }

    private void LoadCpuTabs()
    {
        LoadPowerToggles();
        var cpu = CpuDetector.Current;
        var r = CpuTuneCatalog.Resolve(cpu);
        _cpuRecipe = r;

        if (_cpuPower.CpuDetectedText is null) return; // XAML not ready yet

        _cpuPower.CpuDetectedText.Text = cpu.IsDetected
            ? cpu.RawModel
            : "CPU: not detected -- using a generic tune";

        var tier = r.Definition.Tier switch
        {
            TuneTier.Exact => "exact match",
            TuneTier.Family => "family match",
            _ => "generic",
        };
        var topo = r.Topology switch
        {
            CcdTopology.Single => ", single-CCD",
            CcdTopology.Dual => ", dual-CCD",
            _ => "",
        };
        _cpuPower.CpuTierText.Text =
            $"Recipe: {tier}{topo}, parking: {ParkingText(r.Parking)}. Recommended prebuilt plan: {r.RecommendedPrebuilt}.";

        _cpuPower.CpuPlanStatusText.Text = r.IsGeneric
            ? "No CPU-specific recipe -- 'Build optimized' creates a safe generic tune (aggressive boost, no parking changes)."
            : $"'Build optimized' will create: {r.PlanName}.";

        if (r.NeedsCcdRoutingStack)
        {
            _cpuPower.CcdDependencyCard.Visibility = Visibility.Visible;
            BuildDependencyRows(r);
        }
        else
        {
            _cpuPower.CcdDependencyCard.Visibility = Visibility.Collapsed;
        }

        BuildPlanDetails(r);
        BuildBiosRows(r);
    }

    /// <summary>
    /// Fills the collapsible "What this plan changes" section: the base Windows
    /// plan it clones, the exact processor settings it overrides (its diff from
    /// stock), and why that recipe fits the detected CPU. All text comes from the
    /// pure <see cref="CpuPlanDetails"/> helper so it stays in step with the recipe.
    /// </summary>
    private void BuildPlanDetails(CpuTuneResult r)
    {
        if (_cpuPower.PlanDetailsList is null) return;
        _cpuPower.PlanDetailsList.Children.Clear();

        if (_cpuPower.PlanDetailsHeader is not null)
            _cpuPower.PlanDetailsHeader.Text = $"What the optimized plan changes (vs Windows {r.BasePlanDisplayName})";

        var secondary = (System.Windows.Media.Brush)FindResource("TextFillColorSecondaryBrush");

        _cpuPower.PlanDetailsList.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = CpuPlanDetails.BaseSummary(r),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Foreground = secondary,
        });

        _cpuPower.PlanDetailsList.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = $"Side by side (plugged in) — bold values are what GamerGuardian changes:",
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 4),
        });

        // Read the stock values live from the installed base scheme so the "Windows"
        // column reflects this machine rather than a hardcoded guess (the app never
        // trusts hardcoded power values — see the Power Saver GUID gotcha).
        var baseGuid = PowerPlanMonitor.ResolveBalancedBase();
        Func<Guid, Guid, uint?> readBase = baseGuid == Guid.Empty
            ? (_, _) => null
            : (sub, set) => Powrprof.ReadAcValue(baseGuid, sub, set);
        var comparison = CpuPlanDetails.Comparison(r, readBase);
        _cpuPower.PlanDetailsList.Children.Add(BuildComparisonTable(comparison, r.BasePlanDisplayName));

        _cpuPower.PlanDetailsList.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "Why this suits your CPU:",
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0),
        });
        _cpuPower.PlanDetailsList.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = CpuPlanDetails.Rationale(r),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0),
            Foreground = secondary,
        });
    }

    /// <summary>
    /// Builds the "Windows base vs GamerGuardian" comparison as a 3-column grid.
    /// A value that GamerGuardian changes from the stock value is shown bold so the
    /// real differences stand out from settings it merely pins to the same value.
    /// </summary>
    private System.Windows.Controls.Grid BuildComparisonTable(
        IReadOnlyList<CpuPlanDetails.PlanComparisonRow> rows, string baseName)
    {
        var grid = new System.Windows.Controls.Grid();
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(120) });
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(120) });

        var secondary = (System.Windows.Media.Brush)FindResource("TextFillColorSecondaryBrush");

        void AddCell(int row, int col, string text, FontWeight weight, System.Windows.Media.Brush? fg)
        {
            var tb = new System.Windows.Controls.TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = weight,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(col == 0 ? 0 : 8, 3, 8, 3),
            };
            if (fg is not null) tb.Foreground = fg;
            System.Windows.Controls.Grid.SetRow(tb, row);
            System.Windows.Controls.Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        // Header row.
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        AddCell(0, 0, "Setting", FontWeights.SemiBold, null);
        AddCell(0, 1, $"Windows {baseName}", FontWeights.SemiBold, null);
        AddCell(0, 2, "GamerGuardian", FontWeights.SemiBold, null);

        // Separator under the header.
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        var sep = new System.Windows.Controls.Border
        {
            Height = 1,
            Background = (System.Windows.Media.Brush)FindResource("ControlStrokeColorDefaultBrush"),
            Margin = new Thickness(0, 1, 0, 3),
        };
        System.Windows.Controls.Grid.SetRow(sep, 1);
        System.Windows.Controls.Grid.SetColumn(sep, 0);
        System.Windows.Controls.Grid.SetColumnSpan(sep, 3);
        grid.Children.Add(sep);

        for (int i = 0; i < rows.Count; i++)
        {
            int row = i + 2;
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
            AddCell(row, 0, rows[i].Setting, FontWeights.Normal, null);
            AddCell(row, 1, rows[i].WindowsValue, FontWeights.Normal, secondary);
            // Bold the GamerGuardian value only where it actually differs from stock.
            AddCell(row, 2, rows[i].GamerGuardianValue,
                rows[i].Differs ? FontWeights.SemiBold : FontWeights.Normal, null);
        }

        return grid;
    }

    private void BuildDependencyRows(CpuTuneResult r)
    {
        _cpuPower.CcdDependencyList.Children.Clear();

        var svc = CpuPlanStatus.ReadAmdVCacheService();
        var gameBar = CpuPlanStatus.ReadGameBarEnabled();
        var activeGuid = Powrprof.GetActiveScheme();
        bool planActive = _config.Global.CpuPlan.BuiltSchemeGuid is { } bg
                          && Guid.TryParse(bg, out var bgGuid) && bgGuid == activeGuid;

        // Checkable: AMD 3D V-Cache Optimizer service.
        AddDependencyRow(svc switch
        {
            CcdServiceState.Running => "✓  AMD 3D V-Cache Optimizer service: running",
            CcdServiceState.Stopped => "⚠  AMD 3D V-Cache Optimizer service: stopped -- start it (AMD Adrenalin / Services)",
            _ => "—  AMD 3D V-Cache Optimizer service: not detected (install AMD chipset drivers)",
        });

        // Checkable: Xbox Game Bar.
        AddDependencyRow(gameBar switch
        {
            true => "✓  Xbox Game Bar: enabled (game detection signal)",
            false => "⚠  Xbox Game Bar: disabled -- re-enable so games are detected",
            null => "—  Xbox Game Bar: unknown",
        });

        // Advisory: BIOS CPPC -- cannot be read.
        AddDependencyRow("Advisory  BIOS \"CPPC Dynamic Preferred Cores\" = Driver (the app cannot read BIOS state)");

        var status = CpuPlanStatus.DependencyStatus(planActive, svc, gameBar);
        var summary = status switch
        {
            CcdDependencyStatus.Met =>
                "Checkable dependencies look good. Still verify BIOS CPPC=Driver in your firmware -- the app can't read it, so it never claims full confirmation.",
            CcdDependencyStatus.PartlyUnmet =>
                "At least one dependency is unmet -- the optimized plan won't route games to the cache CCD until it's fixed.",
            _ =>
                "Can't confirm the AMD routing stack (service not detected). Install AMD chipset drivers + the 3D V-Cache Optimizer.",
        };
        var summaryBlock = new System.Windows.Controls.TextBlock
        {
            Text = summary,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Margin = new Thickness(0, 8, 0, 0),
            FontWeight = FontWeights.SemiBold,
        };
        _cpuPower.CcdDependencyList.Children.Add(summaryBlock);
    }

    private void AddDependencyRow(string text)
    {
        _cpuPower.CcdDependencyList.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 2),
        });
    }

    private void BuildBiosRows(CpuTuneResult r)
    {
        if (_bios.BiosGuidanceList is null) return;
        _bios.BiosGuidanceList.Children.Clear();

        if (r.Bios.Count == 0)
        {
            _bios.BiosGuidanceList.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "No CPU-specific BIOS recommendations are available for your processor.",
                TextWrapping = TextWrapping.Wrap,
            });
            return;
        }

        bool first = true;
        foreach (var b in r.Bios)
        {
            var block = new StackPanel { Margin = new Thickness(0, first ? 0 : 10, 0, 0) };
            block.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = $"{b.Name}  →  {b.RecommendedValue}",
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
            });
            block.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = b.Rationale,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0),
                Foreground = (System.Windows.Media.Brush)FindResource("TextFillColorSecondaryBrush"),
            });
            _bios.BiosGuidanceList.Children.Add(block);
            first = false;
        }
    }

    private static string ParkingText(ParkingStrategy p) => p switch
    {
        ParkingStrategy.NoParking => "no parking",
        ParkingStrategy.ParkFrequencyCcd => "park frequency CCD",
        _ => "leave default",
    };

    internal async void BuildOptimizedButton_Click(object sender, RoutedEventArgs e) =>
        await RunCpuActionAsync(CpuPlanApply.BuildOptimizedDriftItem, (System.Windows.Controls.ContentControl)sender, "Building…");

    internal async void SuggestPrebuiltButton_Click(object sender, RoutedEventArgs e) =>
        await RunCpuActionAsync(CpuPlanApply.SuggestPrebuiltDriftItem, (System.Windows.Controls.ContentControl)sender, "Applying…");

    private async Task RunCpuActionAsync(
        Func<CpuTuneResult, AppConfig, DriftItem> make,
        System.Windows.Controls.ContentControl btn, string busyText)
    {
        if (_applyInFlight || _cpuRecipe is null) return;
        _applyInFlight = true;
        SetButtonsEnabled(false);
        var origContent = btn.Content;
        btn.IsEnabled = false;
        btn.Content = busyText;
        try
        {
            // Commit any staged draft edits first so nothing is lost.
            PersistFormToDraft();
            AppConfigCloner.CopyInto(_draft, _config);
            _store.Save(_config);

            var item = make(_cpuRecipe, _config);
            var results = await CpuPlanApply.RunAsync(item, _monitors, _config);

            // The Apply lambda updated _config.Global.PowerPlan / CpuPlan on success.
            _store.Save(_config);
            if (results.Count > 0)
            {
                ChangeLogger.LogApplyResults(results, "ui-cpuplan");
                _monitorService?.RecordVerifiedApplies(results);
            }
            Saved?.Invoke();

            RebaseDraftFromConfig();
            LoadGlobals();
            LoadDisplays();
            LoadServices();
            LoadWindowsAi();
            LoadCpuTabs();
            UpdatePendingStatus();

            if (results.Count > 0)
            {
                var win = new ApplyResultsWindow(results) { Owner = this };
                win.Show();
                if (results.Any(r => !r.Verified))
                {
                    System.Windows.MessageBox.Show(this,
                        "The power plan action did not verify. If a UAC prompt was declined, the Balanced base was missing, " +
                        "or (on dual-CCD X3D) the BIOS/driver/Game Bar dependencies aren't in place, see the Apply Results window for details.",
                        "GamerGuardian", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }

                // Consistency with the main Apply path: any reboot-required change
                // that landed raises the shared restart prompt. Power-plan actions
                // don't need a reboot today, so this is a no-op guard that keeps
                // every apply path honest if that ever changes.
                var rebootDescriptions = results
                    .Where(r => r.RequiresReboot && r.Verified)
                    .Select(r => r.Description)
                    .ToList();
                if (rebootDescriptions.Count > 0)
                    RebootPrompt.Show(rebootDescriptions);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Power plan action failed: " + ex.Message,
                "GamerGuardian", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            btn.Content = origContent;
            btn.IsEnabled = true;
            _applyInFlight = false;
            SetButtonsEnabled(true);
        }
    }

    /// <summary>
    /// Re-reads every monitored setting against the committed config and
    /// writes a [SNAPSHOT] entry to changes.log. Nothing is applied. A status
    /// popup tells the user where to look.
    /// </summary>
    private void VerifyAllButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var rows = new List<(string, string, string, string, bool)>();
            int drifting = 0;
            foreach (var m in _monitors)
            {
                IEnumerable<DriftItem> items;
                try { items = m.CheckDrift(_config).ToList(); }
                catch { continue; }
                foreach (var d in items)
                {
                    rows.Add((d.SettingId, d.DisplayLabel, d.CurrentValue, d.DesiredValue, false));
                    drifting++;
                }
            }
            ChangeLogger.LogStateSnapshot(rows);
            var msg = drifting == 0
                ? "All monitored settings match your preferences. Snapshot written to changes.log."
                : $"{drifting} setting(s) currently drifting from your preferences. Snapshot written to changes.log -- nothing was applied.";
            System.Windows.MessageBox.Show(this, msg, "GamerGuardian -- Verify all",
                System.Windows.MessageBoxButton.OK,
                drifting == 0 ? System.Windows.MessageBoxImage.Information : System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Verify all failed: " + ex.Message, "GamerGuardian",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        // Discards the draft entirely. _config (and on-disk config.json) are
        // unchanged; the MonitorService keeps using whatever was committed
        // before this window opened.
        if (_pendingCount > 0)
        {
            ChangeLogger.LogPreferenceChange(
                "Settings window",
                "Cancel",
                $"{_pendingCount} pending change(s)",
                "discarded");
        }
        _suppressSaveOnClose = true;
        Close();
    }
}

/// <summary>
/// Row for a single Windows AI UWP package in the Windows AI tab. Mutates a
/// <see cref="WindowsAiAppPref"/> reference from the draft config; pending
/// changes only land in <see cref="ConfigStore"/> when the user clicks Apply.
/// </summary>
public sealed class WindowsAiAppRow : INotifyPropertyChanged
{
    private readonly WindowsAiAppPref _pref;
    private readonly Action<string, string, string, string>? _onPrefChanged;

    public WindowsAiAppDefinition Definition { get; }
    public string Name => Definition.DisplayName;
    public string PackageName => Definition.PackageName;
    public string Description => Definition.Description;
    public string CurrentText { get; }

    public string SettingId => $"ai.app:{Definition.PackageName}";
    public string LearnMoreContent => SettingDocsCatalog.FormatForExpander(SettingId);
    public Visibility LearnMoreVisibility =>
        string.IsNullOrEmpty(LearnMoreContent) ? Visibility.Collapsed : Visibility.Visible;

    public bool Monitor
    {
        get => _pref.Monitor;
        set
        {
            if (_pref.Monitor == value) return;
            var before = _pref.Monitor;
            _pref.Monitor = value;
            OnPropertyChanged();
            _onPrefChanged?.Invoke($"AI app: {Definition.DisplayName}", "Monitor", before.ToString(), value.ToString());
        }
    }
    public bool DesiredRemoved
    {
        get => _pref.DesiredRemoved;
        set
        {
            if (_pref.DesiredRemoved == value) return;
            var before = _pref.DesiredRemoved;
            _pref.DesiredRemoved = value;
            OnPropertyChanged();
            _onPrefChanged?.Invoke($"AI app: {Definition.DisplayName}", "Remove", before.ToString(), value.ToString());
        }
    }
    public bool AutoApply
    {
        get => _pref.AutoApply;
        set
        {
            if (_pref.AutoApply == value) return;
            var before = _pref.AutoApply;
            _pref.AutoApply = value;
            OnPropertyChanged();
            _onPrefChanged?.Invoke($"AI app: {Definition.DisplayName}", "AutoApply", before.ToString(), value.ToString());
        }
    }

    public WindowsAiAppRow(
        WindowsAiAppDefinition def,
        WindowsAiAppPref pref,
        string currentText,
        Action<string, string, string, string>? onPrefChanged)
    {
        Definition = def;
        _pref = pref;
        CurrentText = currentText;
        _onPrefChanged = onPrefChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class PowerPlanItem
{
    public Guid Guid { get; }
    public string Name { get; }
    public PowerPlanItem(Guid guid, string name) { Guid = guid; Name = name; }
    public override string ToString() => Name;
}

public sealed class GlobalToggleRow : INotifyPropertyChanged
{
    private readonly ToggleSettingPref _pref;
    private readonly Action<string, string, string, string>? _onPrefChanged;
    public string Name { get; }
    public string Description { get; }
    public string CurrentText { get; }
    public string DefaultText { get; }
    public string OnLabel { get; }
    public string OffLabel { get; }
    public string GroupName { get; }
    public bool RequiresReboot { get; }
    public Visibility RebootBadgeVisibility => RequiresReboot ? Visibility.Visible : Visibility.Collapsed;

    public string SettingId { get; }
    public string LearnMoreContent => SettingDocsCatalog.FormatForExpander(SettingId);
    public Visibility LearnMoreVisibility =>
        string.IsNullOrEmpty(LearnMoreContent) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// GamerGuardian's recommended choice for this setting, rendered in the row's
    /// own vocabulary: the recommendation is stored as a target <c>DesiredOn</c>
    /// in <see cref="SettingRecommendations"/>, and shown as the matching
    /// <see cref="OnLabel"/>/<see cref="OffLabel"/> so it always lines up with the
    /// actual Want options (Enabled/Disabled, Gaming/Default, On/Off). Empty (and
    /// hidden) when the setting has no documented recommendation.
    /// </summary>
    public string RecommendedText =>
        SettingRecommendations.FormatToggleHint(SettingId, OnLabel, OffLabel);
    public Visibility RecommendedTextVisibility =>
        string.IsNullOrEmpty(RecommendedText) ? Visibility.Collapsed : Visibility.Visible;

    public bool Monitor
    {
        get => _pref.Monitor;
        set
        {
            if (_pref.Monitor == value) return;
            var before = _pref.Monitor;
            _pref.Monitor = value;
            OnPropertyChanged();
            _onPrefChanged?.Invoke(Name, "Monitor", before.ToString(), value.ToString());
        }
    }
    public bool DesiredOn
    {
        get => _pref.DesiredOn;
        set
        {
            if (_pref.DesiredOn == value) return;
            var before = _pref.DesiredOn;
            _pref.DesiredOn = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DesiredOff));
            _onPrefChanged?.Invoke(Name, "Want", before ? OnLabel : OffLabel, value ? OnLabel : OffLabel);
        }
    }
    public bool DesiredOff
    {
        get => !_pref.DesiredOn;
        set { if (value && _pref.DesiredOn) DesiredOn = false; }
    }
    public bool AutoApply
    {
        get => _pref.AutoApply;
        set
        {
            if (_pref.AutoApply == value) return;
            var before = _pref.AutoApply;
            _pref.AutoApply = value;
            OnPropertyChanged();
            _onPrefChanged?.Invoke(Name, "AutoApply", before.ToString(), value.ToString());
        }
    }

    public GlobalToggleRow(string name, string description, string currentText, string defaultText,
                           string onLabel, string offLabel,
                           ToggleSettingPref pref, string groupName,
                           bool requiresReboot = false,
                           Action<string, string, string, string>? onPrefChanged = null,
                           string settingId = "")
    {
        Name = name;
        Description = description;
        CurrentText = currentText;
        DefaultText = defaultText;
        OnLabel = onLabel;
        OffLabel = offLabel;
        _pref = pref;
        GroupName = groupName;
        RequiresReboot = requiresReboot;
        _onPrefChanged = onPrefChanged;
        SettingId = settingId;
    }

    public void WriteBack() { /* mutations are direct; nothing to do */ }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class ServiceRow : INotifyPropertyChanged
{
    private readonly ServicePref _pref;
    private readonly Action<string, string, string, string>? _onPrefChanged;

    public ServiceDefinition Definition { get; }
    public string Name => Definition.DisplayName;
    public string ServiceName => Definition.Name;
    public string Description => Definition.Description;
    public string CurrentText { get; }
    public string DefaultText { get; }
    public string GroupName { get; }
    public bool IsInstalled { get; }

    public bool RequiresReboot => Definition.RequiresReboot;
    public Visibility RebootBadgeVisibility =>
        RequiresReboot ? Visibility.Visible : Visibility.Collapsed;
    public Visibility RecommendedBadgeVisibility =>
        Definition.RecommendedTarget.HasValue ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NotInstalledBadgeVisibility =>
        IsInstalled ? Visibility.Collapsed : Visibility.Visible;

    public string SettingId => $"service:{Definition.Name}";
    public string LearnMoreContent => SettingDocsCatalog.FormatForExpander(SettingId);
    public Visibility LearnMoreVisibility =>
        string.IsNullOrEmpty(LearnMoreContent) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// GamerGuardian's recommended startup state for this service, rendered in the
    /// row's radio vocabulary (Default / Manual / Disabled). Services we don't
    /// actively recommend changing have no <c>RecommendedTarget</c> and fall back
    /// to "Default" (i.e. leave it alone).
    /// </summary>
    public string RecommendedText =>
        $"Recommended: {Definition.RecommendedTarget ?? ServiceTargetState.Default}";
    public Visibility RecommendedTextVisibility =>
        string.IsNullOrEmpty(RecommendedText) ? Visibility.Collapsed : Visibility.Visible;

    public bool Monitor
    {
        get => _pref.Monitor;
        set
        {
            if (_pref.Monitor == value) return;
            var before = _pref.Monitor;
            _pref.Monitor = value;
            OnPropertyChanged();
            _onPrefChanged?.Invoke($"Service: {Definition.DisplayName}", "Monitor", before.ToString(), value.ToString());
        }
    }
    public bool DesiredDefault
    {
        get => _pref.Desired == ServiceTargetState.Default;
        set { if (value) SetDesired(ServiceTargetState.Default); }
    }
    public bool DesiredManual
    {
        get => _pref.Desired == ServiceTargetState.Manual;
        set { if (value) SetDesired(ServiceTargetState.Manual); }
    }
    public bool DesiredDisabled
    {
        get => _pref.Desired == ServiceTargetState.Disabled;
        set { if (value) SetDesired(ServiceTargetState.Disabled); }
    }

    /// <summary>Direct setter that bypasses the per-radio plumbing. Used by the preset.</summary>
    public void SetDesiredFromPreset(ServiceTargetState v) => SetDesired(v);

    private void SetDesired(ServiceTargetState v)
    {
        if (_pref.Desired == v) return;
        var before = _pref.Desired;
        _pref.Desired = v;
        OnPropertyChanged(nameof(DesiredDefault));
        OnPropertyChanged(nameof(DesiredManual));
        OnPropertyChanged(nameof(DesiredDisabled));
        _onPrefChanged?.Invoke($"Service: {Definition.DisplayName}", "Want",
            before.ToString(), v.ToString());
    }
    public bool AutoApply
    {
        get => _pref.AutoApply;
        set
        {
            if (_pref.AutoApply == value) return;
            var before = _pref.AutoApply;
            _pref.AutoApply = value;
            OnPropertyChanged();
            _onPrefChanged?.Invoke($"Service: {Definition.DisplayName}", "AutoApply", before.ToString(), value.ToString());
        }
    }

    public ServiceRow(
        ServiceDefinition def,
        ServicePref pref,
        bool isInstalled,
        string currentText,
        string defaultText,
        Action<string, string, string, string>? onPrefChanged)
    {
        Definition = def;
        _pref = pref;
        IsInstalled = isInstalled;
        CurrentText = currentText;
        DefaultText = defaultText;
        GroupName = "svc_" + def.Name;
        _onPrefChanged = onPrefChanged;
    }

    public void WriteBack() { /* mutations are direct; nothing to do */ }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Row view-model for one Application Experience scheduled task. The desired state
/// is binary (leave as-is vs. disable), so the row exposes a single "Disable this
/// task" checkbox rather than the service row's Default/Manual/Disabled radios.
/// </summary>
public sealed class ScheduledTaskRow : INotifyPropertyChanged
{
    private readonly ScheduledTaskPref _pref;
    private readonly Action<string, string, string, string>? _onPrefChanged;

    public ScheduledTaskDefinition Definition { get; }
    public string Name => Definition.DisplayName;
    public string TaskPath => Definition.TaskPath;
    public string Description => Definition.Description;
    public string CurrentText { get; }
    public bool IsPresent { get; }

    public Visibility RecommendedBadgeVisibility =>
        Definition.RecommendedTarget.HasValue ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NotPresentBadgeVisibility =>
        IsPresent ? Visibility.Collapsed : Visibility.Visible;

    public string SettingId => $"task:{Definition.TaskPath.ToLowerInvariant()}";
    public string LearnMoreContent => SettingDocsCatalog.FormatForExpander(SettingId);
    public Visibility LearnMoreVisibility =>
        string.IsNullOrEmpty(LearnMoreContent) ? Visibility.Collapsed : Visibility.Visible;

    public bool Monitor
    {
        get => _pref.Monitor;
        set
        {
            if (_pref.Monitor == value) return;
            var before = _pref.Monitor;
            _pref.Monitor = value;
            OnPropertyChanged();
            _onPrefChanged?.Invoke($"Task: {Definition.DisplayName}", "Monitor", before.ToString(), value.ToString());
        }
    }

    public bool DesiredDisabled
    {
        get => _pref.Desired == ScheduledTaskTarget.Disabled;
        set
        {
            var target = value ? ScheduledTaskTarget.Disabled : ScheduledTaskTarget.Default;
            if (_pref.Desired == target) return;
            var before = _pref.Desired;
            _pref.Desired = target;
            OnPropertyChanged();
            _onPrefChanged?.Invoke($"Task: {Definition.DisplayName}", "Want", before.ToString(), target.ToString());
        }
    }

    public bool AutoApply
    {
        get => _pref.AutoApply;
        set
        {
            if (_pref.AutoApply == value) return;
            var before = _pref.AutoApply;
            _pref.AutoApply = value;
            OnPropertyChanged();
            _onPrefChanged?.Invoke($"Task: {Definition.DisplayName}", "AutoApply", before.ToString(), value.ToString());
        }
    }

    public ScheduledTaskRow(
        ScheduledTaskDefinition def,
        ScheduledTaskPref pref,
        bool isPresent,
        string currentText,
        Action<string, string, string, string>? onPrefChanged)
    {
        Definition = def;
        _pref = pref;
        IsPresent = isPresent;
        CurrentText = currentText;
        _onPrefChanged = onPrefChanged;
    }

    public void WriteBack() { /* mutations are direct; nothing to do */ }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class DisplayRow : INotifyPropertyChanged
{
    private readonly string _key;
    private readonly DisplayPreference _pref;

    public string HeaderText { get; }
    public string StatusText { get; }
    public string RateGroupName { get; }
    public IReadOnlyList<uint> AvailableHz { get; }
    public IReadOnlyList<string> AvailableResolutions { get; }

    public bool HdrMonitor { get => _pref.Hdr.Monitor; set { _pref.Hdr.Monitor = value; OnPropertyChanged(); } }
    public bool HdrDesiredOn { get => _pref.Hdr.DesiredOn; set { _pref.Hdr.DesiredOn = value; OnPropertyChanged(); } }
    public bool HdrAutoApply { get => _pref.Hdr.AutoApply; set { _pref.Hdr.AutoApply = value; OnPropertyChanged(); } }

    // Dynamic Refresh Rate (per display). When the panel/OS doesn't support DRR
    // the controls are hidden and a "Not supported on this display" label shows.
    public bool DrrSupported { get; }
    public System.Windows.Visibility DrrSupportedVisibility =>
        DrrSupported ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    public System.Windows.Visibility DrrUnsupportedVisibility =>
        DrrSupported ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
    public bool DrrMonitor { get => _pref.Drr.Monitor; set { _pref.Drr.Monitor = value; OnPropertyChanged(); } }
    public bool DrrDesiredOn { get => _pref.Drr.DesiredOn; set { _pref.Drr.DesiredOn = value; OnPropertyChanged(); } }
    public bool DrrAutoApply { get => _pref.Drr.AutoApply; set { _pref.Drr.AutoApply = value; OnPropertyChanged(); } }

    public bool RefreshMonitor { get => _pref.RefreshRate.Monitor; set { _pref.RefreshRate.Monitor = value; OnPropertyChanged(); } }
    public bool RefreshUseMax
    {
        get => _pref.RefreshRate.Target == RefreshRateTarget.Maximum;
        set
        {
            if (value)
            {
                _pref.RefreshRate.Target = RefreshRateTarget.Maximum;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RefreshUseFixed));
            }
        }
    }
    public bool RefreshUseFixed
    {
        get => _pref.RefreshRate.Target == RefreshRateTarget.Fixed;
        set
        {
            if (value)
            {
                _pref.RefreshRate.Target = RefreshRateTarget.Fixed;
                if (_pref.RefreshRate.FixedHz is null && AvailableHz.Count > 0)
                    _pref.RefreshRate.FixedHz = AvailableHz[^1];
                OnPropertyChanged();
                OnPropertyChanged(nameof(RefreshUseMax));
                OnPropertyChanged(nameof(FixedHz));
            }
        }
    }
    public uint? FixedHz
    {
        get => _pref.RefreshRate.FixedHz;
        set { _pref.RefreshRate.FixedHz = value; OnPropertyChanged(); }
    }
    public bool RefreshAutoApply { get => _pref.RefreshRate.AutoApply; set { _pref.RefreshRate.AutoApply = value; OnPropertyChanged(); } }

    public bool ResolutionMonitor { get => _pref.Resolution.Monitor; set { _pref.Resolution.Monitor = value; OnPropertyChanged(); } }
    public string? DesiredResolution
    {
        get => _pref.Resolution.DesiredWidth is { } w && _pref.Resolution.DesiredHeight is { } h ? $"{w}x{h}" : null;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _pref.Resolution.DesiredWidth = null;
                _pref.Resolution.DesiredHeight = null;
            }
            else
            {
                var parts = value.Split('x');
                if (parts.Length == 2 && uint.TryParse(parts[0], out var w) && uint.TryParse(parts[1], out var h))
                {
                    _pref.Resolution.DesiredWidth = w;
                    _pref.Resolution.DesiredHeight = h;
                }
            }
            OnPropertyChanged();
        }
    }
    public bool ResolutionAutoApply { get => _pref.Resolution.AutoApply; set { _pref.Resolution.AutoApply = value; OnPropertyChanged(); } }

    public DisplayRow(string key, string label, string status, DisplayPreference pref, IReadOnlyList<uint> rates, IReadOnlyList<string> resolutions, bool drrSupported)
    {
        _key = key;
        _pref = pref;
        HeaderText = label;
        StatusText = status;
        RateGroupName = "rate_" + key.GetHashCode().ToString("X");
        DrrSupported = drrSupported;
        AvailableHz = rates;
        AvailableResolutions = resolutions;
    }

    public void WriteTo(AppConfig cfg) => cfg.Displays[_key] = _pref;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
