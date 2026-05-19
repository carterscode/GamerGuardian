using System.Text.Json;
using GamerGuardian.Models;

namespace GamerGuardian.Services;

/// <summary>
/// Deep-clones <see cref="AppConfig"/> by JSON round-trip. Used by
/// <see cref="UI.SettingsWindow"/> to maintain a draft copy that the UI mutates
/// freely while the committed config (and the background <see cref="MonitorService"/>)
/// see the unchanged state until Apply / Save&amp;close.
///
/// <para>JSON serialization is the simplest deep-clone option here because
/// <see cref="AppConfig"/> is already JSON-roundtripped by <see cref="ConfigStore"/>,
/// so we know every property is reachable.</para>
/// </summary>
public static class AppConfigCloner
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static AppConfig Clone(AppConfig source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        var json = JsonSerializer.Serialize(source, Options);
        return JsonSerializer.Deserialize<AppConfig>(json, Options)
               ?? throw new InvalidOperationException("AppConfig round-trip produced null.");
    }

    /// <summary>
    /// Overwrites <paramref name="target"/>'s fields from <paramref name="source"/>
    /// without changing the <paramref name="target"/> reference. Used when the
    /// draft is committed back into the live config so existing references in
    /// background services keep working.
    /// </summary>
    public static void CopyInto(AppConfig source, AppConfig target)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (target is null) throw new ArgumentNullException(nameof(target));

        target.LaunchAtStartup = source.LaunchAtStartup;
        target.PollIntervalSeconds = source.PollIntervalSeconds;
        target.ConsolidateNotifications = source.ConsolidateNotifications;
        target.Theme = source.Theme;
        target.CheckForUpdatesOnStartup = source.CheckForUpdatesOnStartup;
        target.SkippedUpdateVersion = source.SkippedUpdateVersion;

        target.Displays = source.Displays;
        target.Global = source.Global;
        target.Services = source.Services;
    }
}
