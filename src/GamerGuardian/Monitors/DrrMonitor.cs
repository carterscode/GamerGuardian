using GamerGuardian.Models;
using GamerGuardian.Native;
using GamerGuardian.Services;

namespace GamerGuardian.Monitors;

/// <summary>
/// Dynamic Refresh Rate (DRR), per display. Win11 22H2+ feature distinct from VRR:
/// it boosts the refresh rate between a low virtual rate and the panel's physical
/// rate based on content. Read/written via <see cref="DrrInterop"/> (the public CCD
/// API, user-mode, no elevation). Per-display SettingId (<c>drr:&lt;key&gt;</c>) so
/// MonitorService's per-SettingId backoff/verify state stays independent per display.
/// Displays that don't support DRR are skipped (not drift) so no no-op apply is offered.
/// </summary>
public sealed class DrrMonitor : IMonitoredSetting
{
    public string Id => "drr";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var active = DisplayHelper.EnumerateActiveDisplays();
        foreach (var display in active)
        {
            var read = DrrInterop.ReadState(display.AdapterId, display.TargetId);
            if (!read.Found) continue;
            if (!DrrInterop.IsSupported(display.AdapterId, display.TargetId)) continue;

            var pref = DisplayPreferenceResolver.Resolve(config, display, active);
            bool current = read.Enabled;
            bool desired = pref.Drr.DesiredOn;
            if (current == desired) continue;

            yield return new DriftItem(
                SettingId: $"{Id}:{display.StableKey}",
                DisplayKey: display.StableKey,
                DisplayLabel: display.DisplayLabel,
                Description: $"Dynamic Refresh Rate on {display.DisplayLabel}",
                CurrentValue: current ? "On" : "Off",
                DesiredValue: desired ? "On" : "Off",
                AutoApply: pref.Drr.AutoApply,
                Apply: () => Task.Run(() => DrrInterop.SetState(display.AdapterId, display.TargetId, desired)),
                IsMonitored: pref.Drr.Monitor,
                RawBefore: $"BOOST_REFRESH_RATE={(current ? 1 : 0)}",
                RawDesired: $"BOOST_REFRESH_RATE={(desired ? 1 : 0)}");
        }
    }

    /// <summary>Per-display read for the UI: (Supported, Enabled). Supported=false
    /// when the panel/driver/OS doesn't offer DRR on this target.</summary>
    public static (bool Supported, bool Enabled)? ReadState(DisplayInfo display)
    {
        var read = DrrInterop.ReadState(display.AdapterId, display.TargetId);
        if (!read.Found) return null;
        bool supported = DrrInterop.IsSupported(display.AdapterId, display.TargetId);
        return (supported, read.Enabled);
    }
}
