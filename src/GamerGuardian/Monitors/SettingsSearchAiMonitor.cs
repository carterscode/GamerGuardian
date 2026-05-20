using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Disables Copilot-flavored search suggestions in the Windows Search box
/// + the taskbar "companion" widget. Both are HKCU policies -- no UAC.
///
/// <para>Note: this does NOT disable the Windows Search index itself. Start
/// menu search, Explorer search, and Outlook search continue to work; only
/// the AI-flavored web/Copilot suggestion layer is silenced.</para>
/// </summary>
public sealed class SettingsSearchAiMonitor : IMonitoredSetting
{
    public string Id => "ai.settingssearch";

    private const string PolicyKey = @"SOFTWARE\Policies\Microsoft\Windows\Explorer";
    private const string PolicyVal = "DisableSearchBoxSuggestions";
    private const string AdvancedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string TaskbarCompanionVal = "TaskbarCompanion";

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
            CurrentValue: current.Value ? "On" : "Off (policy)",
            DesiredValue: desired ? "On" : "Off (policy)",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore:  current.Value ? "(default)" : "DisableSearchBoxSuggestions=1, TaskbarCompanion=0",
            RawDesired: desired ? "(deleted)" : "DisableSearchBoxSuggestions=1, TaskbarCompanion=0");
    }

    public static bool? ReadCurrent()
    {
        try
        {
            using var policy = Registry.CurrentUser.OpenSubKey(PolicyKey, writable: false);
            using var advanced = Registry.CurrentUser.OpenSubKey(AdvancedKey, writable: false);
            var disable = policy?.GetValue(PolicyVal) as int?;
            var companion = advanced?.GetValue(TaskbarCompanionVal) as int?;

            // The PolicyVal (DisableSearchBoxSuggestions) is the primary signal
            // and authoritative. The TaskbarCompanion value is a belt-and-
            // suspenders write -- on many Windows builds the value name isn't
            // recognized by Explorer and the write silently doesn't stick. We
            // treat companion as "off" when it's absent OR explicitly 0; only
            // companion == 1 counts as "companion still on". Without this,
            // ReadCurrent would forever report "on" after Apply even though
            // the user-visible search box AI suggestions are actually off.
            bool searchSuggestionsOff = disable == 1;
            bool companionOff = companion != 1;
            bool off = searchSuggestionsOff && companionOff;
            return !off;
        }
        catch { return null; }
    }

    public static void Apply(bool on)
    {
        try
        {
            using var policy = Registry.CurrentUser.CreateSubKey(PolicyKey, writable: true)!;
            using var advanced = Registry.CurrentUser.CreateSubKey(AdvancedKey, writable: true)!;
            if (on)
            {
                policy.DeleteValue(PolicyVal, throwOnMissingValue: false);
                advanced.DeleteValue(TaskbarCompanionVal, throwOnMissingValue: false);
            }
            else
            {
                policy.SetValue(PolicyVal, 1, RegistryValueKind.DWord);
                advanced.SetValue(TaskbarCompanionVal, 0, RegistryValueKind.DWord);
            }
        }
        catch { }
    }
}
