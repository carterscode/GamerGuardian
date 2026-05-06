using GamerGuardian.Models;
using GamerGuardian.Monitors;

namespace GamerGuardian.Services;

/// <summary>
/// Shared apply+verify logic used by both the manual Apply button and the
/// background auto-apply loop. Runs the Apply lambdas, re-runs CheckDrift to
/// verify, and produces ApplyResult records suitable for both the UI and the
/// change log.
/// </summary>
public static class ChangeApplier
{
    public static async Task<List<ApplyResult>> ApplyAndVerifyAsync(
        IReadOnlyList<DriftItem> drifted,
        IReadOnlyList<IMonitoredSetting> monitors,
        Models.AppConfig config)
    {
        foreach (var d in drifted)
        {
            try { await d.Apply(); }
            catch { /* keep going; verify will catch failures */ }
        }

        var afterDrift = new List<DriftItem>();
        foreach (var m in monitors)
        {
            try { afterDrift.AddRange(m.CheckDrift(config)); }
            catch { }
        }

        var results = new List<ApplyResult>();
        foreach (var d in drifted)
        {
            var stillDrifted = afterDrift.FirstOrDefault(a => a.SettingId == d.SettingId);
            var verified = stillDrifted is null;
            var afterValue = verified ? d.DesiredValue : (stillDrifted?.CurrentValue ?? d.CurrentValue);
            var rawAfter = verified ? d.RawDesired : (stillDrifted?.RawBefore ?? d.RawBefore);
            results.Add(new ApplyResult(
                SettingId: d.SettingId,
                Description: d.Description,
                Before: d.CurrentValue,
                Desired: d.DesiredValue,
                After: afterValue,
                Verified: verified,
                RequiresReboot: d.RequiresReboot,
                Mechanism: SettingDocs.MechanismFor(d.SettingId),
                VerifyCommand: SettingDocs.VerifyCommandFor(d.SettingId),
                RawBefore: d.RawBefore,
                RawDesired: d.RawDesired,
                RawAfter: rawAfter));
        }
        return results;
    }
}
