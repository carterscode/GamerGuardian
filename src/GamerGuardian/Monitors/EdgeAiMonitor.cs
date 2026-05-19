using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Microsoft Edge in-browser AI: the Copilot/Hubs sidebar and the
/// foundational-model generative-AI feature. Three HKLM Policies values
/// flipped together via a single elevation prompt.
///
/// <para>HubsSidebarEnabled=0 hides the right-edge Copilot icon.
/// CopilotPageContext=0 stops sending page contents to Copilot.
/// GenAILocalFoundationalModelSettings=1 disables local generative AI features.</para>
/// </summary>
public sealed class EdgeAiMonitor : IMonitoredSetting
{
    public string Id => "ai.edge";

    private const string PolicyKey = @"SOFTWARE\Policies\Microsoft\Edge";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.EdgeAi;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "ai",
            DisplayLabel: "Windows AI",
            Description: desired
                ? "Edge Copilot / Hubs sidebar / GenAI -- remove disable policy"
                : "Edge Copilot / Hubs sidebar / GenAI -- disable via policy",
            CurrentValue: current.Value ? "On" : "Off (policy)",
            DesiredValue: desired ? "On" : "Off (policy)",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore:  current.Value ? "(default)" : "HubsSidebarEnabled=0, CopilotPageContext=0, GenAILocalFoundationalModelSettings=1",
            RawDesired: desired ? "(deleted)" : "HubsSidebarEnabled=0, CopilotPageContext=0, GenAILocalFoundationalModelSettings=1");
    }

    public static bool? ReadCurrent()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(PolicyKey, writable: false);
            var hubs = k?.GetValue("HubsSidebarEnabled") as int?;
            var ctx  = k?.GetValue("CopilotPageContext") as int?;
            var gen  = k?.GetValue("GenAILocalFoundationalModelSettings") as int?;
            // "Off" when all three policy values match the disable-by-policy posture.
            bool off = hubs == 0 && ctx == 0 && gen == 1;
            return !off;
        }
        catch { return null; }
    }

    public static void Apply(bool on)
    {
        if (on)
        {
            ElevatedRegistry.DeleteHklmValue(PolicyKey, "HubsSidebarEnabled");
            ElevatedRegistry.DeleteHklmValue(PolicyKey, "CopilotPageContext");
            ElevatedRegistry.DeleteHklmValue(PolicyKey, "GenAILocalFoundationalModelSettings");
        }
        else
        {
            ElevatedRegistry.SetHklmMulti(new[]
            {
                (subkey: PolicyKey, name: "HubsSidebarEnabled", kind: "REG_DWORD", data: "0"),
                (subkey: PolicyKey, name: "CopilotPageContext", kind: "REG_DWORD", data: "0"),
                (subkey: PolicyKey, name: "GenAILocalFoundationalModelSettings", kind: "REG_DWORD", data: "1"),
            });
        }
    }
}
