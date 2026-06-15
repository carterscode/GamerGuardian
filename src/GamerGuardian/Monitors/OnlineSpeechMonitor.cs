using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Online (cloud) speech recognition -- when on, Windows sends your voice audio
/// to Microsoft for processing. Per-user HKCU flag; 0 = use offline recognition
/// only. Intuitive Enabled/Disabled: <c>DesiredOn</c> = cloud speech enabled; the
/// privacy-recommended pref is OFF. Offline Windows speech/Voice Access still works.
/// </summary>
public sealed class OnlineSpeechMonitor : IMonitoredSetting
{
    public string Id => "privacy.speech";
    private const string SubKey = @"Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy";
    private const string ValueName = "HasAccepted";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.OnlineSpeech;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        int raw = desired ? 1 : 0;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "privacy",
            DisplayLabel: "Privacy",
            Description: "Online (cloud) speech recognition",
            CurrentValue: current.Value ? "Enabled" : "Disabled",
            DesiredValue: desired ? "Enabled" : "Disabled",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: $"HasAccepted={(current.Value ? 1 : 0)}",
            RawDesired: $"HasAccepted={raw}");
    }

    /// <summary>True when cloud speech is accepted (=1). Null when the value is
    /// absent so an undecided machine doesn't report drift until the user opts in.</summary>
    public static bool? ReadCurrent()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(SubKey, writable: false);
            if (k?.GetValue(ValueName) is int v) return v != 0;
            return null;
        }
        catch { return null; }
    }

    public static void Apply(bool on)
    {
        try
        {
            using var k = Registry.CurrentUser.CreateSubKey(SubKey, writable: true)!;
            k.SetValue(ValueName, on ? 1 : 0, RegistryValueKind.DWord);
        }
        catch { }
    }
}
