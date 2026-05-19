using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Snipping Tool "Click to Do" / screenshot-AI-actions feature. Set the HKLM
/// WindowsAI policy DisableClickToDo = 1 and the per-user Shell\ClickToDo
/// override at the same time.
/// </summary>
public sealed class ClickToDoMonitor : IMonitoredSetting
{
    public string Id => "ai.clicktodo";

    private const string HklmKey   = @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI";
    private const string HklmValue = "DisableClickToDo";
    private const string HkcuKey   = @"Software\Microsoft\Windows\Shell\ClickToDo";
    private const string HkcuValue = "DisableClickToDo";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.ClickToDo;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "ai",
            DisplayLabel: "Windows AI",
            Description: desired
                ? "Click-to-Do (Snipping Tool AI) -- remove disable policy"
                : "Click-to-Do (Snipping Tool AI) -- disable via policy",
            CurrentValue: current.Value ? "On" : "Off (policy)",
            DesiredValue: desired ? "On" : "Off (policy)",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore:  current.Value ? "(no policy)" : "DisableClickToDo=1 (HKLM + HKCU)",
            RawDesired: desired ? "(deleted)" : "DisableClickToDo=1 (HKLM + HKCU)");
    }

    public static bool? ReadCurrent()
    {
        var hklm = ReadDword(Registry.LocalMachine, HklmKey, HklmValue);
        var hkcu = ReadDword(Registry.CurrentUser, HkcuKey, HkcuValue);
        bool offByPolicy = hklm == 1 || hkcu == 1;
        return !offByPolicy;
    }

    public static void Apply(bool on)
    {
        if (on)
        {
            ElevatedRegistry.DeleteHklmValue(HklmKey, HklmValue);
            try
            {
                using var k = Registry.CurrentUser.OpenSubKey(HkcuKey, writable: true);
                k?.DeleteValue(HkcuValue, throwOnMissingValue: false);
            }
            catch { }
        }
        else
        {
            ElevatedRegistry.SetHklmDword(HklmKey, HklmValue, 1);
            using var k = Registry.CurrentUser.CreateSubKey(HkcuKey, writable: true)!;
            k.SetValue(HkcuValue, 1, RegistryValueKind.DWord);
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
}
