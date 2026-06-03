using System.Text.Json;
using GamerGuardian.Models;
using GamerGuardian.Native;

namespace GamerGuardian.Services;

/// <summary>
/// Resolves the <see cref="DisplayPreference"/> for an active display, keyed by
/// <see cref="DisplayInfo.StableKey"/>.
///
/// <para><b>Why this exists.</b> A display's <c>StableKey</c> is its
/// <c>DevicePath</c> when Windows reports one, otherwise a
/// <c>FriendlyName|GdiDeviceName</c> fallback. The same physical monitor can
/// enumerate <em>with</em> a DevicePath on one tick and <em>without</em> one on
/// another (capture cards, some HDMI/TV targets, or transient driver state).
/// A naive <c>TryGetValue</c> then mints a fresh default <see cref="DisplayPreference"/>
/// under the new key — silently resetting per-display settings (e.g. a Fixed
/// 240&#160;Hz refresh target back to "Maximum"). This resolver instead reuses
/// the saved prefs by re-keying them onto the current key, and de-duplicates the
/// stale entries that earlier versions left behind.</para>
/// </summary>
public static class DisplayPreferenceResolver
{
    private static readonly JsonSerializerOptions ValueCompareOptions = new()
    {
        WriteIndented = false,
    };

    /// <summary>
    /// Returns the saved prefs for <paramref name="display"/>, reusing an entry
    /// stored under a different key for the same physical monitor when the label
    /// unambiguously identifies it. Creates (and stores) a default only when no
    /// match exists. Mutates <see cref="AppConfig.Displays"/> so the result is
    /// persisted on the next save.
    /// </summary>
    public static DisplayPreference Resolve(
        AppConfig config,
        DisplayInfo display,
        IReadOnlyList<DisplayInfo> activeDisplays)
    {
        var displays = config.Displays;

        if (displays.TryGetValue(display.StableKey, out var pref) && pref is not null)
        {
            if (string.IsNullOrEmpty(pref.DisplayLabel))
                pref.DisplayLabel = display.DisplayLabel;
            return pref;
        }

        // No entry under the current key. The same monitor may be saved under an
        // older/alternate key — reuse it instead of resetting to defaults, but
        // only when the label maps to exactly one active monitor (so two
        // identical panels never share a single pref block).
        var label = display.DisplayLabel;
        if (!string.IsNullOrEmpty(label)
            && activeDisplays.Count(d => d.DisplayLabel == label) == 1)
        {
            var activeKeys = new HashSet<string>(
                activeDisplays.Select(d => d.StableKey), StringComparer.Ordinal);

            string? matchKey = null;
            bool ambiguous = false;
            foreach (var kv in displays)
            {
                if (kv.Value is null || kv.Value.DisplayLabel != label) continue;
                if (activeKeys.Contains(kv.Key)) continue; // belongs to another active display
                if (matchKey is not null) { ambiguous = true; break; }
                matchKey = kv.Key;
            }

            if (!ambiguous && matchKey is not null)
            {
                var reused = displays[matchKey];
                displays.Remove(matchKey);
                displays[display.StableKey] = reused; // re-key onto the current StableKey
                return reused;
            }
        }

        pref = new DisplayPreference { DisplayLabel = label };
        displays[display.StableKey] = pref;
        return pref;
    }

    /// <summary>
    /// Collapses duplicate display entries that earlier, key-unstable versions
    /// left behind. Entries are grouped by <see cref="DisplayPreference.DisplayLabel"/>;
    /// within a group, entries that are value-identical are merged into a single
    /// canonical entry (the one whose key looks like a real DevicePath is kept).
    /// Non-identical entries are left untouched — they may be distinct monitors
    /// of the same model with genuinely different settings.
    /// </summary>
    public static void DedupeDisplays(AppConfig config)
    {
        var displays = config.Displays;
        if (displays.Count < 2) return;

        var groups = displays
            .Where(kv => kv.Value is not null && !string.IsNullOrEmpty(kv.Value.DisplayLabel))
            .GroupBy(kv => kv.Value.DisplayLabel, StringComparer.Ordinal)
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var byValue = group.GroupBy(kv => Serialize(kv.Value), StringComparer.Ordinal);
            foreach (var sameValue in byValue)
            {
                var entries = sameValue.ToList();
                if (entries.Count < 2) continue; // nothing to collapse

                var canonical = PickCanonical(entries.Select(e => e.Key));
                foreach (var e in entries)
                    if (!string.Equals(e.Key, canonical, StringComparison.Ordinal))
                        displays.Remove(e.Key);
            }
        }
    }

    /// <summary>Prefer a real DevicePath key (no <c>|</c> fallback separator); deterministic tie-break otherwise.</summary>
    private static string PickCanonical(IEnumerable<string> keys)
    {
        var ordered = keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        return ordered.FirstOrDefault(k => !k.Contains('|')) ?? ordered[0];
    }

    private static string Serialize(DisplayPreference pref) =>
        JsonSerializer.Serialize(pref, ValueCompareOptions);
}
