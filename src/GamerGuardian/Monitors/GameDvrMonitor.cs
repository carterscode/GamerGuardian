using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Game DVR background recording. Covers the two per-user capture toggles
/// (GameDVR_Enabled, AppCaptureEnabled) AND the machine-wide HKLM policy
/// <c>AllowGameDVR</c>. The policy is the part Windows re-enables after feature
/// updates, so locking it down keeps capture off for good.
///
/// <para>Semantics are intuitive (Enabled/Disabled): <c>DesiredOn</c> maps to
/// "GameDVR on". The gaming-recommended default is OFF. When desired OFF, the
/// monitor requires the HKCU toggles disabled AND the HKLM policy = 0 (the full
/// lockdown). When desired ON, it restores the toggles and deletes the policy so
/// Windows is back at its default.</para>
/// </summary>
public sealed class GameDvrMonitor : IMonitoredSetting
{
    public string Id => "gamedvr";

    private const string ConfigStoreKey = @"System\GameConfigStore";
    private const string GameDvrKey = @"Software\Microsoft\Windows\CurrentVersion\GameDVR";
    private const string PolicyKey = @"SOFTWARE\Policies\Microsoft\Windows\GameDVR";
    private const string PolicyValue = "AllowGameDVR";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.GameDvr;
        var (captureOn, policy) = ReadFullState();
        if (captureOn is null) yield break; // can't read the per-user state -> not drift

        bool desired = pref.DesiredOn;
        // Compliance is richer than a single value: the OFF (gaming) state requires
        // the capture toggles disabled AND the HKLM policy locked to 0; the ON state
        // requires capture enabled AND the policy not forcing-off (absent or 1).
        bool compliant = desired
            ? captureOn.Value && policy != 0
            : !captureOn.Value && policy == 0;
        if (compliant) yield break;

        int capRaw = captureOn.Value ? 1 : 0;
        string policyStr = policy is null ? "(unset)" : policy.Value.ToString();
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: desired
                ? "Game DVR recording -- restore Windows default"
                : "Game DVR recording -- lock off (capture toggles + AllowGameDVR policy)",
            CurrentValue: (captureOn.Value && policy != 0) ? "On" : "Off",
            DesiredValue: desired ? "On" : "Off",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: $"GameDVR_Enabled={capRaw}, AppCaptureEnabled={capRaw}, AllowGameDVR={policyStr}",
            RawDesired: desired
                ? "GameDVR_Enabled=1, AppCaptureEnabled=1, AllowGameDVR=(deleted)"
                : "GameDVR_Enabled=0, AppCaptureEnabled=0, AllowGameDVR=0");
    }

    /// <summary>Effective on/off for the UI: capture enabled AND the policy isn't
    /// forcing it off. Null when the per-user keys can't be read.</summary>
    public static bool? ReadCurrent()
    {
        var (captureOn, policy) = ReadFullState();
        if (captureOn is null) return null;
        return captureOn.Value && policy != 0;
    }

    /// <summary>Reads the per-user capture state and the HKLM policy value.
    /// captureOn is null only when neither per-user key is readable.</summary>
    private static (bool? captureOn, int? policy) ReadFullState()
    {
        bool? captureOn = null;
        using (var ks = Registry.CurrentUser.OpenSubKey(ConfigStoreKey, writable: false))
            if (ks?.GetValue("GameDVR_Enabled") is int v) captureOn = v != 0;
        if (captureOn is null)
            using (var kp = Registry.CurrentUser.OpenSubKey(GameDvrKey, writable: false))
                if (kp?.GetValue("AppCaptureEnabled") is int vp) captureOn = vp != 0;

        int? policy = null;
        try
        {
            using var pk = Registry.LocalMachine.OpenSubKey(PolicyKey, writable: false);
            policy = pk?.GetValue(PolicyValue) as int?;
        }
        catch { /* policy stays null = unset */ }

        return (captureOn, policy);
    }

    public static void Apply(bool on)
    {
        using (var k = Registry.CurrentUser.CreateSubKey(ConfigStoreKey, writable: true)!)
            k.SetValue("GameDVR_Enabled", on ? 1 : 0, RegistryValueKind.DWord);
        using (var k = Registry.CurrentUser.CreateSubKey(GameDvrKey, writable: true)!)
            k.SetValue("AppCaptureEnabled", on ? 1 : 0, RegistryValueKind.DWord);

        if (on)
            ElevatedRegistry.DeleteHklmValue(PolicyKey, PolicyValue);
        else
            ElevatedRegistry.SetHklmDword(PolicyKey, PolicyValue, 0);
    }
}
