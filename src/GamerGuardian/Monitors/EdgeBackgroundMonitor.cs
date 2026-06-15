using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Microsoft Edge "startup boost" + "background mode" -- the pair that keeps
/// Edge processes resident at boot and running after every window is closed,
/// costing idle RAM/CPU on a machine where Edge isn't the daily browser. Set via
/// the HKLM Edge enterprise policies (which survive Edge updates, unlike the
/// in-app toggles). Intuitive Enabled/Disabled: <c>DesiredOn</c> = the
/// boost/background behavior enabled; recommended OFF. HKLM write needs elevation.
///
/// <para>This does not block Edge itself -- it still launches on demand and
/// WebView2-dependent apps keep working.</para>
/// </summary>
public sealed class EdgeBackgroundMonitor : IMonitoredSetting
{
    public string Id => "debloat.edge";
    private const string PolicyKey = @"SOFTWARE\Policies\Microsoft\Edge";
    private const string StartupBoost = "StartupBoostEnabled";
    private const string BackgroundMode = "BackgroundModeEnabled";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.EdgeBackground;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "debloat",
            DisplayLabel: "Debloat",
            Description: "Edge startup boost & background mode",
            CurrentValue: current.Value ? "Enabled" : "Disabled",
            DesiredValue: desired ? "Enabled" : "Disabled",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: current.Value ? "(default / Edge prelaunch on)" : "StartupBoostEnabled=0, BackgroundModeEnabled=0",
            RawDesired: desired ? "(policies deleted / Windows default)" : "StartupBoostEnabled=0, BackgroundModeEnabled=0");
    }

    /// <summary>True (on) unless the startup-boost policy explicitly disables it.</summary>
    public static bool? ReadCurrent()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(PolicyKey, writable: false);
            return (k?.GetValue(StartupBoost) as int?) != 0;
        }
        catch { return null; }
    }

    public static void Apply(bool on)
    {
        if (on)
            ElevatedRegistry.DeleteHklmMulti(new[]
            {
                (PolicyKey, StartupBoost),
                (PolicyKey, BackgroundMode),
            });
        else
            ElevatedRegistry.SetHklmMulti(new[]
            {
                (PolicyKey, StartupBoost, "REG_DWORD", "0"),
                (PolicyKey, BackgroundMode, "REG_DWORD", "0"),
            });
    }
}
