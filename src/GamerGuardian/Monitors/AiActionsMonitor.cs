using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Disables Windows' "AI Actions" feature via the FeatureManagement
/// override hive. AI Actions is the right-click "rewrite with AI / summarize /
/// search the web" surface that started appearing in Windows 11 24H2.
///
/// <para>Implementation note: FeatureManagement overrides live under numeric
/// feature IDs (the IDs come from MS's internal feature flag service, not
/// from any docs we can cite). EnabledState=1 means "force disabled"; 2 means
/// "force enabled"; absent means "use server-provided default."
/// We use the two feature IDs zoicware/RemoveWindowsAI documents: 1853569164
/// (AI Actions shell surface) and 4098520719 (AI Actions data plumbing).</para>
/// </summary>
public sealed class AiActionsMonitor : IMonitoredSetting
{
    public string Id => "ai.actions";

    private const string OverridesRoot = @"SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\8";
    private static readonly uint[] FeatureIds = { 1853569164u, 4098520719u };

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.AiActions;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "ai",
            DisplayLabel: "Windows AI",
            Description: desired
                ? "Windows AI Actions (right-click rewrite / summarize / search) -- re-enable"
                : "Windows AI Actions -- disable via FeatureManagement override",
            CurrentValue: current.Value ? "On" : "Off (override)",
            DesiredValue: desired ? "On" : "Off (override)",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore:  current.Value ? "(no override)" : "EnabledState=1 on both feature IDs",
            RawDesired: desired ? "(deleted)" : "EnabledState=1 on both feature IDs");
    }

    public static bool? ReadCurrent()
    {
        try
        {
            bool allDisabled = true;
            foreach (var id in FeatureIds)
            {
                using var k = Registry.LocalMachine.OpenSubKey(OverridesRoot + "\\" + id, writable: false);
                if ((k?.GetValue("EnabledState") as int?) != 1) { allDisabled = false; break; }
            }
            return !allDisabled;
        }
        catch { return null; }
    }

    public static void Apply(bool on)
    {
        if (on)
        {
            foreach (var id in FeatureIds)
            {
                ElevatedRegistry.DeleteHklmValue(OverridesRoot + "\\" + id, "EnabledState");
            }
        }
        else
        {
            ElevatedRegistry.SetHklmMulti(FeatureIds.Select(id =>
                (subkey: OverridesRoot + "\\" + id, name: "EnabledState", kind: "REG_DWORD", data: "1")));
        }
    }
}
