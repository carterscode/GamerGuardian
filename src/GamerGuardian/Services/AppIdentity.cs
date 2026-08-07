using System.IO;
using System.Reflection;

namespace GamerGuardian.Services;

/// <summary>
/// Everything that must differ between a stable build and a beta build, in one
/// place: where state is written, what the single-instance mutex is called, what
/// the Windows startup entry is named, and what marker the UI shows.
///
/// <para>Gated on the <c>BETA</c> compile constant, set by
/// <c>-p:Beta=true</c> (see <c>GamerGuardian.csproj</c>). A beta build keeps its
/// config, change log and diagnostics entirely separate from a stable install and
/// can run side by side with one. In a non-beta build every member below compiles
/// to exactly the literal it had before this type existed, so behavior is
/// unchanged.</para>
///
/// <para>This is the single source for these paths. <see cref="ConfigStore"/> and
/// <see cref="ChangeLogger"/> both used to build <c>%APPDATA%\GamerGuardian</c>
/// independently, which meant moving one would silently leave the other behind.</para>
/// </summary>
public static class AppIdentity
{
#if BETA
    /// <summary>Folder under %APPDATA% holding config.json and changes.log.</summary>
    public const string ProductFolderName = "GamerGuardian-Beta";

    /// <summary>Filename stem for the %TEMP% diagnostics, so a beta and a stable
    /// instance running together never interleave into the same log.</summary>
    public const string DiagnosticPrefix = "gamerguardian-beta";

    /// <summary>Distinct so a beta and a stable instance can run at the same time.
    /// Unqualified, therefore session-local, matching the original.</summary>
    public const string MutexName = "GamerGuardian.SingleInstance.Beta";

    /// <summary>HKCU Run value name. Distinct rather than suppressed: a beta tester
    /// wants launch-at-startup to actually work, and a shared name would have the
    /// two builds overwrite each other's entry.</summary>
    public const string StartupRegistryValueName = "GamerGuardian-Beta";
#else
    public const string ProductFolderName = "GamerGuardian";
    public const string DiagnosticPrefix = "gamerguardian";
    public const string MutexName = "GamerGuardian.SingleInstance";
    public const string StartupRegistryValueName = "GamerGuardian";
#endif

    /// <summary>%APPDATA%\&lt;ProductFolderName&gt;. Resolved once at type-init.</summary>
    public static string ConfigDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        ProductFolderName);

    public static string ConfigFile { get; } = Path.Combine(ConfigDirectory, "config.json");

    public static string ChangeLogFile { get; } = Path.Combine(ConfigDirectory, "changes.log");

    public static string ErrorLogFile { get; } =
        Path.Combine(Path.GetTempPath(), $"{DiagnosticPrefix}_error.log");

    public static string SelfTestFile { get; } =
        Path.Combine(Path.GetTempPath(), $"{DiagnosticPrefix}_selftest.txt");

    /// <summary>
    /// The stable install's config folder. A beta build reads this once on first
    /// launch to seed its own config; it is never written to. In a stable build it
    /// is the same as <see cref="ConfigDirectory"/>.
    /// </summary>
    public static string StableConfigDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GamerGuardian");

    /// <summary>
    /// Marker appended to the window title and tray tooltip. Empty in a stable
    /// build, so callers can concatenate it unconditionally.
    /// </summary>
#if BETA
    public static string DisplaySuffix { get; } = BuildBetaSuffix();

    /// <summary>" [BETA a1b2c3d]", or just " [BETA]" when no sha was stamped.
    /// The sha comes from the InformationalVersion the beta workflow sets as
    /// "&lt;base&gt;-beta.&lt;sha&gt;" (AssemblyVersion/FileVersion must stay a pure
    /// a.b.c.d, so the suffix can only live here).</summary>
    private static string BuildBetaSuffix()
    {
        try
        {
            var info = typeof(AppIdentity).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
            var plus = info.IndexOf('+');
            if (plus > 0) info = info[..plus];

            const string marker = "-beta.";
            var i = info.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            var sha = i >= 0 ? info[(i + marker.Length)..] : "";
            return string.IsNullOrWhiteSpace(sha) ? " [BETA]" : $" [BETA {sha}]";
        }
        catch
        {
            return " [BETA]";
        }
    }
#else
    public static string DisplaySuffix => string.Empty;
#endif
}
