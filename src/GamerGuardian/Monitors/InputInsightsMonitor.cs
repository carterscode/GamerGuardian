using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Disables Windows' typing-data + ink-data harvesting ("Input Insights"):
///
/// <list type="bullet">
///   <item>RestrictImplicitTextCollection -- blocks the OS from saving the
///     plain text you type for personalized suggestions.</item>
///   <item>InsightsEnabled -- the per-user master switch in the Input panel.</item>
/// </list>
///
/// HKCU only -- no UAC. Toggles the same surface zoicware writes; we don't
/// touch the HKLM Group Policy equivalent here because it requires admin
/// and the HKCU path already takes effect for the logged-in user.
/// </summary>
public sealed class InputInsightsMonitor : IMonitoredSetting
{
    public string Id => "ai.inputinsights";

    private const string PersonalizationKey = @"Software\Microsoft\InputPersonalization";
    private const string PersonalizationVal = "RestrictImplicitTextCollection";
    private const string InputSettingsKey   = @"Software\Microsoft\input\Settings";
    private const string InputSettingsVal   = "InsightsEnabled";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.InputInsights;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "ai",
            DisplayLabel: "Windows AI",
            Description: desired
                ? "Typing / input insights data collection -- re-enable"
                : "Typing / input insights data collection -- disable (HKCU)",
            CurrentValue: current.Value ? "On" : "Off",
            DesiredValue: desired ? "On" : "Off",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore:  current.Value ? "(default)" : "RestrictImplicitTextCollection=1, InsightsEnabled=0",
            RawDesired: desired ? "(deleted)" : "RestrictImplicitTextCollection=1, InsightsEnabled=0");
    }

    public static bool? ReadCurrent()
    {
        try
        {
            using var pkey = Registry.CurrentUser.OpenSubKey(PersonalizationKey, writable: false);
            using var ikey = Registry.CurrentUser.OpenSubKey(InputSettingsKey, writable: false);
            var restrict = pkey?.GetValue(PersonalizationVal) as int?;
            var insights = ikey?.GetValue(InputSettingsVal) as int?;
            bool off = restrict == 1 && insights == 0;
            return !off;
        }
        catch { return null; }
    }

    public static void Apply(bool on)
    {
        try
        {
            using var pkey = Registry.CurrentUser.CreateSubKey(PersonalizationKey, writable: true)!;
            using var ikey = Registry.CurrentUser.CreateSubKey(InputSettingsKey, writable: true)!;
            if (on)
            {
                pkey.DeleteValue(PersonalizationVal, throwOnMissingValue: false);
                ikey.DeleteValue(InputSettingsVal, throwOnMissingValue: false);
            }
            else
            {
                pkey.SetValue(PersonalizationVal, 1, RegistryValueKind.DWord);
                ikey.SetValue(InputSettingsVal, 0, RegistryValueKind.DWord);
            }
        }
        catch { }
    }
}
