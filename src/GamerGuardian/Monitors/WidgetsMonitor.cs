using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Windows 11 Widgets / "News and interests" -- the left-edge weather button
/// that opens a web-connected MSN feed and fetches data in the background.
/// Disabled machine-wide via the HKLM <c>Dsh\AllowNewsAndInterests</c> policy
/// (the durable kill that stops the process), plus the per-user taskbar button
/// flag. Intuitive Enabled/Disabled: <c>DesiredOn</c> = Widgets enabled;
/// recommended OFF. The HKLM write needs elevation (one UAC prompt).
/// </summary>
public sealed class WidgetsMonitor : IMonitoredSetting
{
    public string Id => "debloat.widgets";
    private const string PolicyKey = @"SOFTWARE\Policies\Microsoft\Dsh";
    private const string PolicyVal = "AllowNewsAndInterests";
    private const string TaskbarKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string TaskbarVal = "TaskbarDa";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.Widgets;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "debloat",
            DisplayLabel: "Debloat",
            Description: "Widgets / News and interests",
            CurrentValue: current.Value ? "Enabled" : "Disabled",
            DesiredValue: desired ? "Enabled" : "Disabled",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: current.Value ? "(default / Widgets on)" : "AllowNewsAndInterests=0, TaskbarDa=0",
            RawDesired: desired ? "(policy deleted / Windows default)" : "AllowNewsAndInterests=0, TaskbarDa=0");
    }

    /// <summary>True (on) unless the HKLM policy explicitly disables Widgets.
    /// Reading HKLM doesn't require elevation.</summary>
    public static bool? ReadCurrent()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(PolicyKey, writable: false);
            return (k?.GetValue(PolicyVal) as int?) != 0;
        }
        catch { return null; }
    }

    public static void Apply(bool on)
    {
        // Per-user taskbar button flag (no elevation).
        try
        {
            using var k = Registry.CurrentUser.CreateSubKey(TaskbarKey, writable: true)!;
            if (on) k.DeleteValue(TaskbarVal, throwOnMissingValue: false);
            else k.SetValue(TaskbarVal, 0, RegistryValueKind.DWord);
        }
        catch { }

        // Machine-wide policy (elevated; one UAC prompt).
        if (on)
            ElevatedRegistry.DeleteHklmValue(PolicyKey, PolicyVal);
        else
            ElevatedRegistry.SetHklmDword(PolicyKey, PolicyVal, 0);
    }
}
