namespace GamerGuardian.Services;

/// <summary>
/// The nine sections a managed setting can belong to. These mirror where a setting
/// is actually presented to the user today, not how it is stored.
///
/// <para>This is deliberately flat. Sections do <b>not</b> know which navigation
/// group they sit under — grouping is a shell concern and stays out of this
/// table.</para>
/// </summary>
public enum SettingSection
{
    /// <summary>The id is not mapped. Returned explicitly rather than defaulting or
    /// throwing, so an unmapped id is visible to callers and to tests.</summary>
    Unknown,
    Gaming,
    Display,
    CpuPower,
    Telemetry,
    WindowsAi,
    Network,
    Debloat,
    Services,
    Bios,
}

/// <summary>
/// Maps a setting id to the one section it belongs to.
///
/// <para><b>Source of truth</b> is the <c>Load*()</c> methods in
/// <c>UI/SettingsWindow.xaml.cs</c> and which <c>TabItem</c> in
/// <c>UI/SettingsWindow.xaml</c> hosts the row they build — i.e. where the user
/// actually sees the setting. The grouping comments in <c>Models/AppConfig.cs</c>
/// corroborate most of this but are not authoritative; where they disagree, the
/// <c>Load*()</c> method wins. The known disagreements are called out on the
/// entries below.</para>
///
/// <para>Prefixed ids (<c>service:</c>, <c>task:</c>, <c>ai.app:</c>, <c>hdr:</c>,
/// <c>refresh:</c>, <c>resolution:</c>, <c>drr:</c>) are resolved by their family
/// via <see cref="SettingDocsCatalog.ParseId"/> — the same parsing
/// <see cref="SettingDocsCatalog.Get"/> uses, not a second copy of it. Everything
/// else is looked up in <see cref="Globals"/>.</para>
/// </summary>
public static class SettingSectionMap
{
    /// <summary>
    /// Section for a setting id, or <see cref="SettingSection.Unknown"/> when the id
    /// is unmapped, empty, or null. Never throws; never guesses a default.
    /// </summary>
    public static SettingSection SectionFor(string? settingId)
    {
        if (string.IsNullOrWhiteSpace(settingId)) return SettingSection.Unknown;

        var (kind, rest) = SettingDocsCatalog.ParseId(settingId);
        return kind switch
        {
            // Both service and scheduled-task rows are built by LoadServices() /
            // LoadScheduledTasks() into the one "Windows services" TabItem.
            SettingIdKind.Service => SettingSection.Services,
            SettingIdKind.ScheduledTask => SettingSection.Services,
            // LoadWindowsAi() builds the UWP app-removal rows into the same
            // "Windows AI" TabItem as the policy toggles.
            SettingIdKind.AiApp => SettingSection.WindowsAi,
            // Per-display instance ids from LoadDisplays() -> "Display" TabItem.
            SettingIdKind.Hdr => SettingSection.Display,
            SettingIdKind.RefreshRate => SettingSection.Display,
            SettingIdKind.Resolution => SettingSection.Display,
            SettingIdKind.Drr => SettingSection.Display,
            _ => Globals.TryGetValue(rest, out var s) ? s : SettingSection.Unknown,
        };
    }

    /// <summary>True when the id maps to a real section.</summary>
    public static bool IsMapped(string? settingId) => SectionFor(settingId) != SettingSection.Unknown;

    /// <summary>Every explicitly mapped id. Exposed for tests and for the section
    /// counts; prefix-resolved families are not listed here because they are
    /// unbounded (one id per installed service, task, package, and display).</summary>
    public static IReadOnlyDictionary<string, SettingSection> MappedIds => Globals;

    private static readonly Dictionary<string, SettingSection> Globals =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // ---- Gaming: LoadGlobals() -> "Global gaming" TabItem ----
            ["gamemode"] = SettingSection.Gaming,
            ["gamedvr"] = SettingSection.Gaming,
            ["hags"] = SettingSection.Gaming,
            ["memintegrity"] = SettingSection.Gaming,
            ["vbs"] = SettingSection.Gaming,
            ["sysresponse"] = SettingSection.Gaming,
            ["usbsuspend"] = SettingSection.Gaming,
            ["gamestask"] = SettingSection.Gaming,
            ["mouseaccel"] = SettingSection.Gaming,
            ["fso"] = SettingSection.Gaming,
            ["vrr"] = SettingSection.Gaming,
            // faststartup + visualfx are built by LoadGlobals() onto the Global
            // gaming tab, even though AppConfig.cs:165 groups them under a "System
            // toggles" comment together with powerthrottling, which lives on the
            // CPU / Power tab. Load*() wins: these two are Gaming.
            ["faststartup"] = SettingSection.Gaming,
            ["visualfx"] = SettingSection.Gaming,

            // ---- Display: LoadDisplays() -> "Display" TabItem ----
            // The bare, colon-less base ids. Per-display instance ids ("hdr:KEY")
            // are resolved by prefix in SectionFor.
            ["hdr"] = SettingSection.Display,
            ["refresh"] = SettingSection.Display,
            ["resolution"] = SettingSection.Display,
            ["drr"] = SettingSection.Display,

            // ---- CPU and power: "CPU / Power" TabItem ----
            // powerthrottling comes from LoadPowerToggles(); powerplan is populated
            // by LoadGlobals() but its card is hosted in the CPU / Power TabItem,
            // and cpuplan is the plan-builder action on the same tab. Section
            // follows where the user sees them.
            ["powerthrottling"] = SettingSection.CpuPower,
            ["powerplan"] = SettingSection.CpuPower,
            ["cpuplan"] = SettingSection.CpuPower,

            // ---- Telemetry: LoadPrivacy() -> "Privacy" TabItem ----
            ["privacy.advertisingid"] = SettingSection.Telemetry,
            ["privacy.tailoredexp"] = SettingSection.Telemetry,
            ["privacy.cdp"] = SettingSection.Telemetry,
            ["privacy.activityhistory"] = SettingSection.Telemetry,
            ["privacy.speech"] = SettingSection.Telemetry,
            ["privacy.inking"] = SettingSection.Telemetry,

            // ---- Windows AI: LoadWindowsAi() -> "Windows AI" TabItem ----
            ["ai.copilot"] = SettingSection.WindowsAi,
            ["ai.recall"] = SettingSection.WindowsAi,
            ["ai.clicktodo"] = SettingSection.WindowsAi,
            ["ai.edge"] = SettingSection.WindowsAi,
            ["ai.notepadpaint"] = SettingSection.WindowsAi,
            ["ai.settingssearch"] = SettingSection.WindowsAi,
            ["ai.actions"] = SettingSection.WindowsAi,
            ["ai.inputinsights"] = SettingSection.WindowsAi,
            ["ai.office"] = SettingSection.WindowsAi,

            // ---- Network: LoadNetwork() -> "Network" TabItem ----
            // netthrottle is stored in AppConfig's ungrouped top block but has been
            // presented on the Network tab since v0.1.46. Load*() wins.
            ["netthrottle"] = SettingSection.Network,
            ["network.nagle"] = SettingSection.Network,
            ["network.nicpower"] = SettingSection.Network,

            // ---- Debloat: LoadDebloat() -> "Debloat" TabItem ----
            // Split across two hosts on one tab (DebloatAdsList and
            // DebloatBackgroundList); both are the Debloat section.
            ["debloat.suggestedcontent"] = SettingSection.Debloat,
            ["debloat.spotlight"] = SettingSection.Debloat,
            ["debloat.finishsetup"] = SettingSection.Debloat,
            ["debloat.startrecommend"] = SettingSection.Debloat,
            ["debloat.explorerads"] = SettingSection.Debloat,
            ["debloat.feedback"] = SettingSection.Debloat,
            ["debloat.widgets"] = SettingSection.Debloat,
            ["debloat.edge"] = SettingSection.Debloat,

            // ---- BIOS: the "Recommended BIOS" TabItem is advisory text only
            // (BiosGuidanceList renders CpuTuneCatalog BiosRecommendation entries).
            // It hosts no managed setting, so no id maps to SettingSection.Bios.
        };
}
