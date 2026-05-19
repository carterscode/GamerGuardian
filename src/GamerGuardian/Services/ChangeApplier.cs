using System.Diagnostics;
using GamerGuardian.Models;
using GamerGuardian.Monitors;

namespace GamerGuardian.Services;

/// <summary>
/// Shared apply+verify logic used by both the manual Apply button and the
/// background auto-apply loop. Runs the Apply lambdas, re-runs CheckDrift to
/// verify, and produces <see cref="ApplyResult"/> records suitable for both the
/// UI and the change log.
///
/// <para>The <c>source</c> string ("manual" / "auto" / "auto-revert") and the
/// <c>sessionId</c> flow through every record in the batch so log readers can
/// correlate cause and effect — e.g. "every change with sessionId X happened
/// because the user clicked Apply at HH:MM:SS".</para>
/// </summary>
public static class ChangeApplier
{
    public static Task<List<ApplyResult>> ApplyAndVerifyAsync(
        IReadOnlyList<DriftItem> drifted,
        IReadOnlyList<IMonitoredSetting> monitors,
        AppConfig config)
        => ApplyAndVerifyAsync(drifted, monitors, config, source: "manual", sessionId: NewSessionId());

    public static async Task<List<ApplyResult>> ApplyAndVerifyAsync(
        IReadOnlyList<DriftItem> drifted,
        IReadOnlyList<IMonitoredSetting> monitors,
        AppConfig config,
        string source,
        string sessionId)
    {
        // Per-change elapsed time + exception capture. Awaiting one at a time
        // (not Task.WhenAll) so the UAC prompts that come from elevated children
        // don't all flash at once — and so timings are isolated per change.
        var elapsed = new long[drifted.Count];
        var errors = new string?[drifted.Count];
        for (int i = 0; i < drifted.Count; i++)
        {
            var sw = Stopwatch.StartNew();
            try { await drifted[i].Apply(); }
            catch (Exception ex) { errors[i] = $"{ex.GetType().Name}: {ex.Message}"; }
            sw.Stop();
            elapsed[i] = sw.ElapsedMilliseconds;
        }

        var afterDrift = new List<DriftItem>();
        foreach (var m in monitors)
        {
            try { afterDrift.AddRange(m.CheckDrift(config)); }
            catch { }
        }

        var results = new List<ApplyResult>(drifted.Count);
        for (int i = 0; i < drifted.Count; i++)
        {
            var d = drifted[i];
            var stillDrifted = afterDrift.FirstOrDefault(a => a.SettingId == d.SettingId);
            var verified = stillDrifted is null && errors[i] is null;
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
                RawAfter: rawAfter,
                ApplyCommand: SettingDocs.ApplyCommandFor(d.SettingId, d.RawDesired),
                ElapsedMs: elapsed[i],
                Source: source,
                SessionId: sessionId,
                ErrorMessage: errors[i]));
        }
        return results;
    }

    /// <summary>
    /// Short, log-friendly identifier shared across every record in one Apply
    /// batch. Eight hex chars is enough to grep on without taking up half a line.
    /// </summary>
    public static string NewSessionId() => Guid.NewGuid().ToString("N").Substring(0, 8);
}
