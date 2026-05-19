namespace GamerGuardian.Models;

/// <summary>
/// Static metadata for a Windows-AI UWP package the user might want gone.
/// Lives in <see cref="GamerGuardian.Services.WindowsAiAppCatalog"/>; per-user
/// prefs live in <see cref="AppConfig.WindowsAiApps"/> keyed by <see cref="PackageName"/>.
/// </summary>
public sealed record WindowsAiAppDefinition(
    string PackageName,
    string DisplayName,
    string Description);

/// <summary>
/// Per-package preference for the UWP-AI section of the Windows AI tab.
/// <para>"DesiredRemoved = true" + the package being installed = drift.
/// Apply runs <c>Get-AppxPackage | Remove-AppxPackage</c>.</para>
///
/// <para>There is no inverse (re-install) path: GamerGuardian can't restore a
/// removed UWP package. CheckDrift only flags the install -> remove direction.</para>
/// </summary>
public sealed class WindowsAiAppPref
{
    public bool Monitor { get; set; } = false;
    public bool DesiredRemoved { get; set; } = false;
    public bool AutoApply { get; set; } = false;
}
