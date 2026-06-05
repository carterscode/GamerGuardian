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
    public ObservableCollection<DisplayRow> DisplayRows { get; } = new();
    public ObservableCollection<GlobalToggleRow> GlobalToggleRows { get; } = new();
    public ObservableCollection<GlobalToggleRow> PrivacyToggleRows { get; } = new();
    public ObservableCollection<GlobalToggleRow> NetworkToggleRows { get; } = new();
    public ObservableCollection<GlobalToggleRow> PowerToggleRows { get; } = new();
    public ObservableCollection<GlobalToggleRow> WindowsAiRowsCollection { get; } = new();
    public ObservableCollection<WindowsAiAppRow> WindowsAiAppRowsCollection { get; } = new();
    public ObservableCollection<ServiceRow> ServiceRows { get; } = new();
    private bool _suppressPresetEvents;
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

        LaunchAtStartupCheck.IsChecked = _draft.LaunchAtStartup;
        ConsolidateCheck.IsChecked = _draft.ConsolidateNotifications;
        CheckForUpdatesCheck.IsChecked = _draft.CheckForUpdatesOnStartup;
        PollSecondsBox.Value = _draft.PollIntervalSeconds;

        ThemeCombo.ItemsSource = Enum.GetValues<AppThemeChoice>();
        ThemeCombo.SelectedItem = _draft.Theme;

        VersionLink.Content = GetVersionDisplay();
        VersionLink.ToolTip = GetVersionTooltip();

        DisplaysList.ItemsSource = DisplayRows;
        GlobalTogglesList.ItemsSource = GlobalToggleRows;
        PrivacyTogglesList.ItemsSource = PrivacyToggleRows;
        NetworkTogglesList.ItemsSource = NetworkToggleRows;
        PowerTogglesList.ItemsSource = PowerToggleRows;
        ServicesList.ItemsSource = ServiceRows;
        WindowsAiRows.ItemsSource = WindowsAiRowsCollection;
        WindowsAiAppRows.ItemsSource = WindowsAiAppRowsCollection;

        LoadGlobals();
        LoadDisplays();
        LoadServices();
        LoadWindowsAi();
        LoadPrivacy();
        LoadNetwork();
        LoadCpuTabs();
        UpdatePendingStatus();
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
            defaultText: "Default: On",
            onLabel: "On", offLabel: "Off",
            pref: g.Copilot, groupName: "ai_copilot",
            onPrefChanged: OnRowPrefChanged,
            settingId: "ai.copilot"));

        WindowsAiRowsCollection.Add(new GlobalToggleRow(
            name: "Windows Recall + AI data analysis",
            description: "Group-policy block for Recall snapshotting and on-device AI screen analysis.",
            currentText: $"Current: {OnOffText(SafeRead(RecallMonitor.ReadCurrent))}",
            defaultText: "Default: On",
            onLabel: "On", offLabel: "Off",
            pref: g.Recall, groupName: "ai_recall",
            onPrefChanged: OnRowPrefChanged,
            settingId: "ai.recall"));

        WindowsAiRowsCollection.Add(new GlobalToggleRow(
            name: "Click-to-Do (Snipping Tool AI)",
            description: "Disable the AI 'do something with this' action layer over screenshots.",
            currentText: $"Current: {OnOffText(SafeRead(ClickToDoMonitor.ReadCurrent))}",
            defaultText: "Default: On",
            onLabel: "On", offLabel: "Off",
            pref: g.ClickToDo, groupName: "ai_ctd",
            onPrefChanged: OnRowPrefChanged,
            settingId: "ai.clicktodo"));

        WindowsAiRowsCollection.Add(new GlobalToggleRow(
            name: "Microsoft Edge Copilot / Hubs sidebar / GenAI",
            description: "Three Edge enterprise policies: hide the right-edge Copilot icon, block page-context sharing, and disable local generative AI.",
            currentText: $"Current: {OnOffText(SafeRead(EdgeAiMonitor.ReadCurrent))}",
            defaultText: "Default: On",
            onLabel: "On", offLabel: "Off",
            pref: g.EdgeAi, groupName: "ai_edge",
            onPrefChanged: OnRowPrefChanged,
            settingId: "ai.edge"));

        WindowsAiRowsCollection.Add(new GlobalToggleRow(
            name: "Notepad Rewrite + Paint AI",
            description: "Per-user disable of Notepad Rewrite and Paint Cocreator / Image Creator / Generative Erase.",
            currentText: $"Current: {OnOffText(SafeRead(NotepadPaintAiMonitor.ReadCurrent))}",
            defaultText: "Default: On",
            onLabel: "On", offLabel: "Off",
            pref: g.NotepadPaintAi, groupName: "ai_notepadpaint",
            onPrefChanged: OnRowPrefChanged,
            settingId: "ai.notepadpaint"));

        WindowsAiRowsCollection.Add(new GlobalToggleRow(
            name: "Search box AI suggestions + taskbar companion",
            description: "Hides the AI-flavored web suggestions in the Windows search box and disables the floating Copilot taskbar widget. Indexing itself is untouched.",
            currentText: $"Current: {OnOffText(SafeRead(SettingsSearchAiMonitor.ReadCurrent))}",
            defaultText: "Default: On",
            onLabel: "On", offLabel: "Off",
            pref: g.SettingsSearchAi, groupName: "ai_settingssearch",
            onPrefChanged: OnRowPrefChanged,
            settingId: "ai.settingssearch"));

        WindowsAiRowsCollection.Add(new GlobalToggleRow(
            name: "Windows AI Actions (right-click rewrite / summarize)",
            description: "Disables Windows' shell-level AI Actions surface via the FeatureManagement override hive.",
            currentText: $"Current: {OnOffText(SafeRead(AiActionsMonitor.ReadCurrent))}",
            defaultText: "Default: On",
            onLabel: "On", offLabel: "Off",
            pref: g.AiActions, groupName: "ai_actions",
            onPrefChanged: OnRowPrefChanged,
            settingId: "ai.actions"));

        WindowsAiRowsCollection.Add(new GlobalToggleRow(
            name: "Typing / input insights data collection",
            description: "HKCU opt-out of Windows' typing-data harvesting. Personalized suggestions degrade slightly; everything else works normally.",
            currentText: $"Current: {OnOffText(SafeRead(InputInsightsMonitor.ReadCurrent))}",
            defaultText: "Default: On",
            onLabel: "On", offLabel: "Off",
            pref: g.InputInsights, groupName: "ai_inputinsights",
            onPrefChanged: OnRowPrefChanged,
            settingId: "ai.inputinsights"));

        WindowsAiRowsCollection.Add(new GlobalToggleRow(
            name: "Microsoft 365 Copilot in Word / Excel / OneNote",
            description: "Disables the Copilot ribbon entries in the desktop Office apps + opts out of MS AI model training on document contents. No-op if Office isn't installed.",
            currentText: $"Current: {OnOffText(SafeRead(OfficeCopilotMonitor.ReadCurrent))}",
            defaultText: "Default: On",
            onLabel: "On", offLabel: "Off",
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
            ServicesPresetGaming.IsChecked = matchesGaming;
            ServicesPresetDefault.IsChecked = matchesDefault;
        }
        finally { _suppressPresetEvents = false; }
    }

    private static ServiceTargetState GetDesired(ServiceRow r) =>
        r.DesiredDisabled ? ServiceTargetState.Disabled
        : r.DesiredManual ? ServiceTargetState.Manual
        : ServiceTargetState.Default;

    private void ServicesPresetGaming_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressPresetEvents) return;
        ApplyServicesPreset(useRecommended: true);
    }

    private void ServicesPresetDefault_Checked(object sender, RoutedEventArgs e)
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
            currentText: $"Current: {OnOffText(SafeRead(MemoryIntegrityMonitor.ReadCurrent))}",
            defaultText: "Default: Enabled (Win11)",
            onLabel: "Enabled", offLabel: "Disabled",
            requiresReboot: true,
            pref: g.MemoryIntegrity, groupName: "memint",
            onPrefChanged: OnRowPrefChanged,
            settingId: "memintegrity"));

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
        PowerPlanCurrentText.Text = $"Current: {activeName}";
        PowerPlanMonitorCheck.IsChecked = g.PowerPlan.Monitor;
        PowerPlanAutoApplyCheck.IsChecked = g.PowerPlan.AutoApply;

        var planItems = planNames
            .OrderBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new PowerPlanItem(kv.Key, kv.Value))
            .ToList();
        PowerPlanCombo.ItemsSource = planItems;
        PowerPlanCombo.DisplayMemberPath = nameof(PowerPlanItem.Name);

        var savedGuid = PowerPlanMonitor.ResolveDesiredGuid(g.PowerPlan);
        PowerPlanCombo.SelectedItem = planItems.FirstOrDefault(p => p.Guid == savedGuid)
            ?? (active is Guid a ? planItems.FirstOrDefault(p => p.Guid == a) : null)
            ?? planItems.FirstOrDefault();
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

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeCombo.SelectedItem is AppThemeChoice c)
            ThemeService.Apply(c);
    }

    private void PowerPlanCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PowerPlanCombo.SelectedItem is not PowerPlanItem pi) return;
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

    private void PowerPlanMonitorCheck_Changed(object sender, RoutedEventArgs e)
    {
        var v = PowerPlanMonitorCheck.IsChecked == true;
        if (_draft.Global.PowerPlan.Monitor == v) return;
        var before = _draft.Global.PowerPlan.Monitor;
        _draft.Global.PowerPlan.Monitor = v;
        ChangeLogger.LogPreferenceChange("Power plan", "Monitor", before.ToString(), v.ToString());
        _pendingCount++;
        UpdatePendingStatus();
    }

    private void PowerPlanAutoApplyCheck_Changed(object sender, RoutedEventArgs e)
    {
        var v = PowerPlanAutoApplyCheck.IsChecked == true;
        if (_draft.Global.PowerPlan.AutoApply == v) return;
        var before = _draft.Global.PowerPlan.AutoApply;
        _draft.Global.PowerPlan.AutoApply = v;
        ChangeLogger.LogPreferenceChange("Power plan", "AutoApply", before.ToString(), v.ToString());
        _pendingCount++;
        UpdatePendingStatus();
    }

    private void OpenChangeLogButton_Click(object sender, RoutedEventArgs e)
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

    private async void CheckUpdatesNowButton_Click(object sender, RoutedEventArgs e)
    {
        var btn = CheckUpdatesNowButton;
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
        LoadWindowsAi();
        LoadPrivacy();
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
        _draft.LaunchAtStartup = LaunchAtStartupCheck.IsChecked == true;
        _draft.ConsolidateNotifications = ConsolidateCheck.IsChecked == true;
        _draft.CheckForUpdatesOnStartup = CheckForUpdatesCheck.IsChecked == true;
        if (PollSecondsBox.Value is double pv && pv >= 5)
            _draft.PollIntervalSeconds = (int)pv;
        if (ThemeCombo.SelectedItem is AppThemeChoice tc)
            _draft.Theme = tc;

        _draft.Global.PowerPlan.Monitor = PowerPlanMonitorCheck.IsChecked == true;
        _draft.Global.PowerPlan.AutoApply = PowerPlanAutoApplyCheck.IsChecked == true;
        if (PowerPlanCombo.SelectedItem is PowerPlanItem pi)
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
            DisplaysList.ItemsSource = null;
            GlobalTogglesList.ItemsSource = null;
            PrivacyTogglesList.ItemsSource = null;
            NetworkTogglesList.ItemsSource = null;
            PowerTogglesList.ItemsSource = null;
            ServicesList.ItemsSource = null;
            DisplayRows.Clear();
            GlobalToggleRows.Clear();
            PrivacyToggleRows.Clear();
            NetworkToggleRows.Clear();
            PowerToggleRows.Clear();
            ServiceRows.Clear();
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
    private void ApplyRecommendedPresetButton_Click(object sender, RoutedEventArgs e)
    {
        RecommendedPreset.Result result;
        try { result = RecommendedPreset.ApplyToDraft(_draft); }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Recommended preset failed: " + ex.Message,
                "GamerGuardian", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        _pendingCount += result.SettingsChanged;
        UpdatePendingStatus();

        // Rebuild every row collection so the UI reflects the mutated draft.
        // (Row setters short-circuit on equality so this rebinds cleanly
        // without firing per-row PREF-STAGE events -- those would double-log.)
        LoadGlobals();
        LoadDisplays();
        LoadServices();
        LoadWindowsAi();
        LoadPrivacy();
        LoadNetwork();
        LoadCpuTabs();

        if (RecommendedStatusText is not null)
        {
            RecommendedStatusText.Text = result.SettingsChanged == 0
                ? $"All {result.SettingsAlreadyCorrect} preset-managed setting(s) already in the recommended state. Nothing to do."
                : $"Staged {result.SettingsChanged} setting(s); {result.SettingsAlreadyCorrect} already correct. Click Apply or Save & close to commit.";
        }

        var dialogMsg = result.SettingsChanged == 0
            ? $"Your draft already matches the Recommended preset across all {result.SettingsAlreadyCorrect} preset-managed setting(s). Nothing to do."
            : $"Staged {result.SettingsChanged} change(s) to the Recommended preset. {result.SettingsAlreadyCorrect} setting(s) were already correct and skipped."
              + "\n\nReview the per-tab changes if you want. Click Apply (stay in Settings) or Save & close to commit the apply pass. Click Cancel to discard.";

        System.Windows.MessageBox.Show(this, dialogMsg, "GamerGuardian -- Recommended preset",
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

        if (CpuDetectedText is null) return; // XAML not ready yet

        CpuDetectedText.Text = cpu.IsDetected
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
        CpuTierText.Text =
            $"Recipe: {tier}{topo}, parking: {ParkingText(r.Parking)}. Recommended prebuilt plan: {r.RecommendedPrebuilt}.";

        CpuPlanStatusText.Text = r.IsGeneric
            ? "No CPU-specific recipe -- 'Build optimized' creates a safe generic tune (aggressive boost, no parking changes)."
            : $"'Build optimized' will create: {r.PlanName}.";

        if (r.NeedsCcdRoutingStack)
        {
            CcdDependencyCard.Visibility = Visibility.Visible;
            BuildDependencyRows(r);
        }
        else
        {
            CcdDependencyCard.Visibility = Visibility.Collapsed;
        }

        BuildBiosRows(r);
    }

    private void BuildDependencyRows(CpuTuneResult r)
    {
        CcdDependencyList.Children.Clear();

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
        CcdDependencyList.Children.Add(summaryBlock);
    }

    private void AddDependencyRow(string text)
    {
        CcdDependencyList.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 2),
        });
    }

    private void BuildBiosRows(CpuTuneResult r)
    {
        if (BiosGuidanceList is null) return;
        BiosGuidanceList.Children.Clear();

        if (r.Bios.Count == 0)
        {
            BiosGuidanceList.Children.Add(new System.Windows.Controls.TextBlock
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
            BiosGuidanceList.Children.Add(block);
            first = false;
        }
    }

    private static string ParkingText(ParkingStrategy p) => p switch
    {
        ParkingStrategy.NoParking => "no parking",
        ParkingStrategy.ParkFrequencyCcd => "park frequency CCD",
        _ => "leave default",
    };

    private async void BuildOptimizedButton_Click(object sender, RoutedEventArgs e) =>
        await RunCpuActionAsync(CpuPlanApply.BuildOptimizedDriftItem, (System.Windows.Controls.ContentControl)sender, "Building…");

    private async void SuggestPrebuiltButton_Click(object sender, RoutedEventArgs e) =>
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
