using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// The Start menu "Recommended" section -- AI/Iris-driven app and web
/// suggestions plus the list of recently opened files (a privacy leak on a
/// shared screen). Per-user Explorer\Advanced DWORDs (0 = off). Intuitive
/// Enabled/Disabled: <c>DesiredOn</c> = the recommendations enabled; recommended OFF.
/// </summary>
public sealed class StartRecommendationsMonitor : IMonitoredSetting
{
    public string Id => "debloat.startrecommend";
    private const string SubKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";

    private static readonly string[] Values =
    {
        "Start_IrisRecommendations", // AI app/web suggestions in Start
        "Start_TrackDocs",           // recently opened files in Start / jump lists
    };

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.StartRecommendations;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "debloat",
            DisplayLabel: "Debloat",
            Description: "Start menu recommendations & recent files",
            CurrentValue: current.Value ? "Enabled" : "Disabled",
            DesiredValue: desired ? "Enabled" : "Disabled",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: current.Value ? "(default / recommendations on)" : "Start_IrisRecommendations=0, Start_TrackDocs=0",
            RawDesired: desired ? "(deleted / Windows default)" : "Start_IrisRecommendations=0, Start_TrackDocs=0");
    }

    public static bool? ReadCurrent()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(SubKey, writable: false);
            foreach (var v in Values)
            {
                var cur = k?.GetValue(v) as int?;
                if (cur != 0) return true;
            }
            return false;
        }
        catch { return null; }
    }

    public static void Apply(bool on)
    {
        try
        {
            if (on)
            {
                using var k = Registry.CurrentUser.OpenSubKey(SubKey, writable: true);
                if (k is null) return;
                foreach (var v in Values) k.DeleteValue(v, throwOnMissingValue: false);
            }
            else
            {
                using var k = Registry.CurrentUser.CreateSubKey(SubKey, writable: true)!;
                foreach (var v in Values) k.SetValue(v, 0, RegistryValueKind.DWord);
            }
        }
        catch { }
    }
}
