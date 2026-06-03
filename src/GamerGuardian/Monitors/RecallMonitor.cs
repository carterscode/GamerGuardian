using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Windows Recall + AI data analysis policy. When DesiredOn = false we set
/// <c>AllowRecallEnablement = 0</c> and <c>DisableAIDataAnalysis = 1</c> under
/// the HKLM WindowsAI policy key. When DesiredOn = true we delete both values.
///
/// <para>This is the policy-based switch -- it stops new Recall snapshotting
/// but does not delete prior snapshots or uninstall the feature itself. For
/// the latter, see WindowsAiAppCatalog / WindowsAiAppMonitor.</para>
/// </summary>
public sealed class RecallMonitor : IMonitoredSetting
{
    public string Id => "ai.recall";

    private const string PolicyKey = @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI";
    private const string RecallVal = "AllowRecallEnablement";
    private const string AiAnalyVal = "DisableAIDataAnalysis";
    private const string SnapshotsVal = "TurnOffSavingSnapshots";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.Recall;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "ai",
            DisplayLabel: "Windows AI",
            Description: desired
                ? "Windows Recall + AI data analysis -- remove disable policy"
                : "Windows Recall + AI data analysis -- block via policy",
            CurrentValue: current.Value ? "On" : "Off (policy)",
            DesiredValue: desired ? "On" : "Off (policy)",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore:  current.Value ? "(default / policy unset)" : "AllowRecallEnablement=0, DisableAIDataAnalysis=1, TurnOffSavingSnapshots=1",
            RawDesired: desired ? "(deleted)" : "AllowRecallEnablement=0, DisableAIDataAnalysis=1, TurnOffSavingSnapshots=1");
    }

    public static bool? ReadCurrent()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(PolicyKey, writable: false);
            var recall = k?.GetValue(RecallVal) as int?;
            var analy  = k?.GetValue(AiAnalyVal) as int?;
            var snap   = k?.GetValue(SnapshotsVal) as int?;
            // Snapshots is the per-feature kill switch (zoicware adds it on top of AllowRecallEnablement
            // because some Windows builds respect one but not the other). Treat ANY of the three being
            // missing-or-permissive as "On" so users get drift if Windows clears one.
            bool blocked = recall == 0 && analy == 1 && snap == 1;
            return !blocked;
        }
        catch { return null; }
    }

    public static void Apply(bool on)
    {
        if (on)
        {
            ElevatedRegistry.DeleteHklmValue(PolicyKey, RecallVal);
            ElevatedRegistry.DeleteHklmValue(PolicyKey, AiAnalyVal);
            ElevatedRegistry.DeleteHklmValue(PolicyKey, SnapshotsVal);
        }
        else
        {
            // Single elevation prompt for all three values via the multi helper.
            ElevatedRegistry.SetHklmMulti(new[]
            {
                (subkey: PolicyKey, name: RecallVal,    kind: "REG_DWORD", data: "0"),
                (subkey: PolicyKey, name: AiAnalyVal,   kind: "REG_DWORD", data: "1"),
                (subkey: PolicyKey, name: SnapshotsVal, kind: "REG_DWORD", data: "1"),
            });
        }
    }
}
