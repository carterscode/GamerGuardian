using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Windows Copilot system-wide policy. When DesiredOn = false we set
/// <c>TurnOffWindowsCopilot = 1</c> in both HKLM and HKCU Policies hives
/// and hide the taskbar button. When DesiredOn = true we delete those
/// values to restore the Windows default.
///
/// <para>One of the registry-policy lockdowns inspired by
/// github.com/zoicware/RemoveWindowsAI; staying in the safe policy-toggle
/// lane because every value here is undone by a plain registry delete.</para>
/// </summary>
public sealed class CopilotMonitor : IMonitoredSetting
{
    public string Id => "ai.copilot";

    private const string HklmKey   = @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot";
    private const string HkcuKey   = @"Software\Policies\Microsoft\Windows\WindowsCopilot";
    private const string ValueName = "TurnOffWindowsCopilot";
    private const string ExplorerKey   = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string ExplorerValue = "ShowCopilotButton";

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
                : "Windows Copilot -- disable system-wide via policy",
            CurrentValue: current.Value ? "On" : "Off (policy)",
            DesiredValue: desired ? "On" : "Off (policy)",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore:  current.Value ? "(no policy / =0)" : "TurnOffWindowsCopilot=1",
            RawDesired: desired ? "(deleted)" : "TurnOffWindowsCopilot=1");
    }

    /// <summary>
    /// True when Copilot is allowed (no off-policy is set). False when ANY of
    /// the HKLM/HKCU policy keys explicitly turns it off. Null if neither key
    /// is readable (no Policies subkey at all -- treat as default/on).
    /// </summary>
    public static bool? ReadCurrent()
    {
        var hklm = ReadDword(Registry.LocalMachine, HklmKey, ValueName);
        var hkcu = ReadDword(Registry.CurrentUser, HkcuKey, ValueName);
        bool offByPolicy = hklm == 1 || hkcu == 1;
        return !offByPolicy;
    }

    public static void Apply(bool on)
    {
        if (on)
        {
            // Restore default: drop both policy values + restore the taskbar button.
            ElevatedRegistry.DeleteHklmValue(HklmKey, ValueName);
            DeleteHkcuValue(HkcuKey, ValueName);
            using var k = Registry.CurrentUser.CreateSubKey(ExplorerKey, writable: true)!;
            k.DeleteValue(ExplorerValue, throwOnMissingValue: false);
        }
        else
        {
            // Off via policy in both hives + hide the taskbar button so the
            // user doesn't see a button that fails to launch anything.
            ElevatedRegistry.SetHklmDword(HklmKey, ValueName, 1);
            using (var k = Registry.CurrentUser.CreateSubKey(HkcuKey, writable: true)!)
                k.SetValue(ValueName, 1, RegistryValueKind.DWord);
            using (var k = Registry.CurrentUser.CreateSubKey(ExplorerKey, writable: true)!)
                k.SetValue(ExplorerValue, 0, RegistryValueKind.DWord);
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
