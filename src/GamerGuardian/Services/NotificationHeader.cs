using GamerGuardian.Models;

namespace GamerGuardian.Services;

/// <summary>
/// Picks the notification window's header text based on what's actually in
/// the drift report. Replaces the old hard-coded "Display settings have
/// drifted" string that appeared even when no displays were involved (e.g.
/// a Windows-AI policy drifting). The drift item's DisplayKey is the
/// grouping signal; if a single category dominates the report we name it,
/// otherwise we use a category-agnostic "Monitored settings" header.
/// </summary>
public static class NotificationHeader
{
    public static string For(DriftReport report)
    {
        if (report is null || report.Items.Count == 0)
            return "Monitored settings have drifted";

        var keys = report.Items.Select(i => i.DisplayKey).Distinct().ToList();
        if (keys.Count == 1)
        {
            var single = report.Items.Count == 1;
            var noun = keys[0] switch
            {
                "global"  => "Global gaming setting",
                "ai"      => "Windows AI setting",
                "ai-app"  => "Windows AI app",
                "service" => "Windows service",
                "display" => "Display setting",
                _         => "Monitored setting"
            };
            // Pluralize the noun when there are multiple items, except for
            // proper-noun "Windows AI app" which we'll just leave as singular
            // since multiples are rare.
            var pluralNoun = single ? noun
                : keys[0] switch
                {
                    "service" => "Windows services",
                    "display" => "Display settings",
                    "global"  => "Global gaming settings",
                    "ai"      => "Windows AI settings",
                    "ai-app"  => "Windows AI apps",
                    _         => "Monitored settings"
                };
            return $"{(single ? noun : pluralNoun)} {(single ? "has" : "have")} drifted";
        }
        return $"{report.Items.Count} monitored settings have drifted";
    }
}
