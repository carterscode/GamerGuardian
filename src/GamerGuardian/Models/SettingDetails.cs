namespace GamerGuardian.Models;

/// <summary>
/// Long-form documentation for a single setting GamerGuardian manages. One
/// entry per <c>settingId</c>; populated in
/// <see cref="GamerGuardian.Services.SettingDocsCatalog"/>. Surfaced in the
/// UI via the per-row "Learn more" expander and also dumped into
/// <c>docs/SETTINGS-REFERENCE.md</c> so the docs and the app can't drift apart.
///
/// <para>The shape is opinionated: every entry is expected to answer the same
/// four questions in the same order so users can scan across settings without
/// re-learning a layout each time.</para>
/// </summary>
/// <param name="SettingId">Matches <c>IMonitoredSetting.Id</c> (e.g. "hags",
/// "service:DiagTrack", "ai.copilot"). Used as the lookup key.</param>
/// <param name="DisplayName">Human-readable name for the docs page header.</param>
/// <param name="What">One paragraph: what this setting actually controls inside
/// Windows. Not a recommendation, not a why -- just plain mechanics.</param>
/// <param name="Why">When and why a user would care about changing this. The
/// "do I need to think about this?" answer.</param>
/// <param name="HowItHelps">The concrete benefit if the recommended value is
/// applied -- frame ratability, input latency, CPU headroom, etc.</param>
/// <param name="Scenarios">Per-use-case recommendations. Keys are scenario
/// names ("Competitive FPS", "Streaming + game", "Casual single-player",
/// "Productivity / not gaming"); values are the recommended state in plain
/// English.</param>
/// <param name="Recommended">The single default GamerGuardian ships with --
/// what the "Gaming optimized" preset (or equivalent) sets.</param>
/// <param name="Risks">What can break if the user changes this away from the
/// Windows default. Honest list of consequences, not marketing.</param>
/// <param name="ReversibleVia">How to undo this change if the user decides to.
/// Often a registry-value delete or a service-startup-type reset.</param>
public sealed record SettingDetails(
    string SettingId,
    string DisplayName,
    string What,
    string Why,
    string HowItHelps,
    IReadOnlyDictionary<string, string> Scenarios,
    string Recommended,
    string Risks,
    string ReversibleVia);
