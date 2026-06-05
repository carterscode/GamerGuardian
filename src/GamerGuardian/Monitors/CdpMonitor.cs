using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Cross-Device Platform (CDP) -- the "Continue experiences on this device" /
/// shared-experiences subsystem. Gaming/privacy-recommended OFF. Driven by the
/// HKLM policy <c>EnableCdp</c>; absence of the value means CDP is at the Windows
/// default (ON). Inverted Gaming/Default semantics: <c>DesiredOn=true</c> means
/// the gaming state (CDP disabled, EnableCdp=0). HKLM write goes through the
/// elevated path; reversal deletes the policy value to restore the default.
/// </summary>
public sealed class CdpMonitor : IMonitoredSetting
{
    public string Id => "privacy.cdp";
    private const string SubKey = @"SOFTWARE\Policies\Microsoft\Windows\System";
    private const string ValueName = "EnableCdp";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.Cdp;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn; // true = gaming (CDP disabled)
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "privacy",
            DisplayLabel: "Privacy",
            Description: desired
                ? "Cross-Device Platform -- disable via policy"
                : "Cross-Device Platform -- restore Windows default",
            CurrentValue: current.Value ? "Disabled (gaming)" : "Default",
            DesiredValue: desired ? "Disabled (gaming)" : "Default",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: current.Value ? "EnableCdp=0" : "(default / unset)",
            RawDesired: desired ? "EnableCdp=0" : "(deleted)");
    }

    /// <summary>True when CDP is in the gaming state (policy explicitly disables it).</summary>
    public static bool? ReadCurrent()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(SubKey, writable: false);
            var v = k?.GetValue(ValueName) as int?;
            return v == 0;
        }
        catch { return null; }
    }

    public static void Apply(bool gaming)
    {
        if (gaming)
            ElevatedRegistry.SetHklmDword(SubKey, ValueName, 0);
        else
            ElevatedRegistry.DeleteHklmValue(SubKey, ValueName);
    }
}
