using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Inking &amp; typing personalization -- Windows building a personal dictionary
/// from your handwriting samples and contact names and uploading it to improve
/// suggestions. The master per-user opt-in is <c>Personalization\Settings\AcceptedPrivacyPolicy</c>;
/// we also restrict implicit ink collection and contact harvesting. Intuitive
/// Enabled/Disabled: <c>DesiredOn</c> = personalization/collection enabled; the
/// privacy-recommended pref is OFF.
///
/// <para>Distinct from the Windows AI tab's "Typing / input insights" toggle,
/// which owns <c>RestrictImplicitTextCollection</c> + the input Insights flag.
/// This one covers the ink/handwriting + personal-dictionary side so the two
/// don't fight over the same value.</para>
/// </summary>
public sealed class InkingTypingMonitor : IMonitoredSetting
{
    public string Id => "privacy.inking";
    private const string PersonalizationKey = @"Software\Microsoft\Personalization\Settings";
    private const string PersonalizationVal = "AcceptedPrivacyPolicy";
    private const string InputKey = @"Software\Microsoft\InputPersonalization";
    private const string InkVal = "RestrictImplicitInkCollection";
    private const string TrainedKey = @"Software\Microsoft\InputPersonalization\TrainedDataStore";
    private const string ContactsVal = "HarvestContacts";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.InkingTyping;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "privacy",
            DisplayLabel: "Privacy",
            Description: "Inking & typing personalization (data collection)",
            CurrentValue: current.Value ? "Enabled" : "Disabled",
            DesiredValue: desired ? "Enabled" : "Disabled",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: $"AcceptedPrivacyPolicy={(current.Value ? 1 : 0)}",
            RawDesired: desired
                ? "AcceptedPrivacyPolicy=1, RestrictImplicitInkCollection=0, HarvestContacts=1"
                : "AcceptedPrivacyPolicy=0, RestrictImplicitInkCollection=1, HarvestContacts=0");
    }

    /// <summary>True when the master personalization opt-in is accepted (=1).
    /// Null when absent so an undecided machine doesn't report drift.</summary>
    public static bool? ReadCurrent()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(PersonalizationKey, writable: false);
            if (k?.GetValue(PersonalizationVal) is int v) return v != 0;
            return null;
        }
        catch { return null; }
    }

    public static void Apply(bool on)
    {
        try
        {
            using (var p = Registry.CurrentUser.CreateSubKey(PersonalizationKey, writable: true)!)
                p.SetValue(PersonalizationVal, on ? 1 : 0, RegistryValueKind.DWord);
            using (var i = Registry.CurrentUser.CreateSubKey(InputKey, writable: true)!)
                i.SetValue(InkVal, on ? 0 : 1, RegistryValueKind.DWord);
            using (var t = Registry.CurrentUser.CreateSubKey(TrainedKey, writable: true)!)
                t.SetValue(ContactsVal, on ? 1 : 0, RegistryValueKind.DWord);
        }
        catch { }
    }
}
