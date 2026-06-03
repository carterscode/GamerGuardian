using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Windows Copilot system-wide policy. Off writes the disable policy in
/// HKLM + HKCU, hides the taskbar button, blanks the Copilot launch alias
/// (so Win+C does nothing), and marks the Copilot UWP as "DisabledByUser"
/// so it can't background-launch. On deletes everything we wrote.
///
/// <para>v0.1.39 extended the off-state with the BrandedKey AppAumid blank
/// + BackgroundAccessApplications DisabledByUser flag (per zoicware/RemoveWindowsAI)
/// so Copilot is fully gagged, not just hidden.</para>
/// </summary>
public sealed class CopilotMonitor : IMonitoredSetting
{
    public string Id => "ai.copilot";

    private const string HklmKey   = @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot";
    private const string HkcuKey   = @"Software\Policies\Microsoft\Windows\WindowsCopilot";
    private const string ValueName = "TurnOffWindowsCopilot";
    private const string ExplorerKey   = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string ExplorerValue = "ShowCopilotButton";
    private const string BrandedKey  = @"Software\Microsoft\Windows\Shell\BrandedKey";
    private const string BrandedVal  = "AppAumid";
    private const string BgAccessKey = @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications\Microsoft.Copilot_8wekyb3d8bbwe";
    private const string BgAccessVal = "DisabledByUser";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.Copilot;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "ai",
            DisplayLabel: "Windows AI",
            Description: desired
                ? "Windows Copilot -- remove disable policy"
                : "Windows Copilot -- disable system-wide via policy + blank launch alias",
            CurrentValue: current.Value ? "On" : "Off (policy)",
            DesiredValue: desired ? "On" : "Off (policy)",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore:  current.Value ? "(no policy / =0)" : "TurnOffWindowsCopilot=1, BrandedKey blanked, BgAccess=1",
            RawDesired: desired ? "(deleted)" : "TurnOffWindowsCopilot=1, BrandedKey blanked, BgAccess=1");
    }

    /// <summary>
    /// True when Copilot is allowed (none of our disabling values are set).
    /// False when ANY disabling value is set. We treat the union of disablers
    /// as "off" so the user gets drift if any one was reverted.
    /// </summary>
    public static bool? ReadCurrent()
    {
        var hklm = ReadDword(Registry.LocalMachine, HklmKey, ValueName);
        var hkcu = ReadDword(Registry.CurrentUser, HkcuKey, ValueName);
        var bgAccess = ReadDword(Registry.CurrentUser, BgAccessKey, BgAccessVal);
        var aumid = ReadString(Registry.CurrentUser, BrandedKey, BrandedVal);
        bool offByPolicy = hklm == 1 || hkcu == 1 || bgAccess == 1 || (aumid is not null && string.IsNullOrWhiteSpace(aumid));
        return !offByPolicy;
    }

    public static void Apply(bool on)
    {
        if (on)
        {
            ElevatedRegistry.DeleteHklmValue(HklmKey, ValueName);
            DeleteHkcuValue(HkcuKey, ValueName);
            DeleteHkcuValue(ExplorerKey, ExplorerValue);
            DeleteHkcuValue(BrandedKey, BrandedVal);
            DeleteHkcuValue(BgAccessKey, BgAccessVal);
        }
        else
        {
            ElevatedRegistry.SetHklmDword(HklmKey, ValueName, 1);
            using (var k = Registry.CurrentUser.CreateSubKey(HkcuKey, writable: true)!)
                k.SetValue(ValueName, 1, RegistryValueKind.DWord);
            using (var k = Registry.CurrentUser.CreateSubKey(ExplorerKey, writable: true)!)
                k.SetValue(ExplorerValue, 0, RegistryValueKind.DWord);
            using (var k = Registry.CurrentUser.CreateSubKey(BrandedKey, writable: true)!)
                k.SetValue(BrandedVal, " ", RegistryValueKind.String);
            using (var k = Registry.CurrentUser.CreateSubKey(BgAccessKey, writable: true)!)
                k.SetValue(BgAccessVal, 1, RegistryValueKind.DWord);
        }
    }

    private static int? ReadDword(RegistryKey hive, string subkey, string name)
    {
        try
        {
            using var k = hive.OpenSubKey(subkey, writable: false);
            return k?.GetValue(name) as int?;
        }
        catch { return null; }
    }

    private static string? ReadString(RegistryKey hive, string subkey, string name)
    {
        try
        {
            using var k = hive.OpenSubKey(subkey, writable: false);
            return k?.GetValue(name) as string;
        }
        catch { return null; }
    }

    private static void DeleteHkcuValue(string subkey, string name)
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(subkey, writable: true);
            k?.DeleteValue(name, throwOnMissingValue: false);
        }
        catch { }
    }
}
