using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Disables the web/Copilot-flavored suggestion layer in the Windows Search box
/// plus the "search highlights" companion content. All HKCU -- no UAC.
///
/// <para><b>Why the mechanism changed (v0.1.41).</b> The original implementation
/// keyed both the write and the drift signal on
/// <c>HKCU\SOFTWARE\Policies\Microsoft\Windows\Explorer\DisableSearchBoxSuggestions</c>
/// plus a non-existent <c>TaskbarCompanion</c> Advanced value. On most Windows 11
/// builds neither is reliable: the Explorer policy frequently doesn't take effect
/// (and on managed/MDM machines the Policies hive can be locked, so the write
/// throws and is swallowed), and <c>TaskbarCompanion</c> is not a real value name
/// so it never persists. When the written value doesn't read back, the apply+verify
/// pass keeps reporting drift -> MonitorService backs the setting off for 15 min ->
/// the drift item gets promoted to the notification queue -> the drift popup recurs
/// every cycle even with Auto-apply silently enabled. (See commit 3202e9c, which
/// only loosened the <i>read</i> heuristic and so didn't actually stop the loop.)</para>
///
/// <para><b>The reliable value.</b>
/// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Search\BingSearchEnabled = 0</c>
/// is the value Windows 11 actually honors for the search-box web/AI suggestion
/// layer, and -- being an ordinary user setting rather than a policy -- it always
/// persists and reads back. We make it the <i>sole</i> drift signal so a value
/// Windows happens to ignore can never re-trigger the verify-fail/backoff/popup
/// loop again. <c>IsDynamicSearchBoxEnabled = 0</c> (search highlights / the
/// companion content) and the legacy <c>DisableSearchBoxSuggestions = 1</c> policy
/// are still written best-effort for completeness, but are not part of the read.</para>
///
/// <para>Note: this does NOT disable the Windows Search index itself. Start menu
/// search, Explorer search, and Outlook search continue to work; only the
/// AI/web suggestion layer and highlights are silenced. A sign-out or Explorer
/// restart may be needed before the change is visible.</para>
/// </summary>
public sealed class SettingsSearchAiMonitor : IMonitoredSetting
{
    public string Id => "ai.settingssearch";

    // Authoritative signal -- ordinary user setting, persists and reads back reliably.
    private const string SearchKey = @"Software\Microsoft\Windows\CurrentVersion\Search";
    private const string BingSearchVal = "BingSearchEnabled";
    // Search highlights / companion content.
    private const string SearchSettingsKey = @"Software\Microsoft\Windows\CurrentVersion\SearchSettings";
    private const string DynamicSearchVal = "IsDynamicSearchBoxEnabled";
    // Legacy policy -- best-effort only, unreliable on Win11, NOT read for drift.
    private const string PolicyKey = @"SOFTWARE\Policies\Microsoft\Windows\Explorer";
    private const string PolicyVal = "DisableSearchBoxSuggestions";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.SettingsSearchAi;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "ai",
            DisplayLabel: "Windows AI",
            Description: desired
                ? "Search box AI suggestions + taskbar companion -- re-enable"
                : "Search box AI suggestions + taskbar companion -- disable",
            CurrentValue: current.Value ? "On" : "Off",
            DesiredValue: desired ? "On" : "Off",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore:  current.Value ? "(default)" : "BingSearchEnabled=0, IsDynamicSearchBoxEnabled=0, DisableSearchBoxSuggestions=1",
            RawDesired: desired ? "(deleted)" : "BingSearchEnabled=0, IsDynamicSearchBoxEnabled=0, DisableSearchBoxSuggestions=1");
    }

    /// <summary>
    /// Returns true when search-box AI/web suggestions are "on" (gaming-default),
    /// false when disabled, null when unreadable. Keyed solely on the reliable
    /// <see cref="BingSearchVal"/> so a Windows-ignored value can't cause a
    /// false-positive drift loop -- the failure mode this monitor twice hit.
    /// </summary>
    public static bool? ReadCurrent()
    {
        try
        {
            using var search = Registry.CurrentUser.OpenSubKey(SearchKey, writable: false);
            var bing = search?.GetValue(BingSearchVal) as int?;
            // Disabled iff we explicitly set BingSearchEnabled = 0. Absent value
            // (fresh install) means suggestions are on -> reported as drift when
            // the user wants them off, which the first auto-apply then fixes.
            bool off = bing == 0;
            return !off;
        }
        catch { return null; }
    }

    public static void Apply(bool on)
    {
        // Each write is independent and best-effort: the authoritative
        // BingSearchEnabled write lives under HKCU\...\Search (always
        // user-writable), so it succeeds even when the Policies hive is locked
        // on a managed device. Failures are swallowed per value rather than
        // aborting the whole apply.
        if (on)
        {
            DeleteValue(SearchKey, BingSearchVal);
            DeleteValue(SearchSettingsKey, DynamicSearchVal);
            DeleteValue(PolicyKey, PolicyVal);
        }
        else
        {
            SetValue(SearchKey, BingSearchVal, 0);
            SetValue(SearchSettingsKey, DynamicSearchVal, 0);
            SetValue(PolicyKey, PolicyVal, 1);
        }
    }

    private static void SetValue(string subKey, string name, int value)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(subKey, writable: true);
            key?.SetValue(name, value, RegistryValueKind.DWord);
        }
        catch { }
    }

    private static void DeleteValue(string subKey, string name)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(subKey, writable: true);
            key?.DeleteValue(name, throwOnMissingValue: false);
        }
        catch { }
    }
}
