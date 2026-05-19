using GamerGuardian.Models;

namespace GamerGuardian.Services;

/// <summary>
/// Curated list of Windows AI UWP packages GamerGuardian can offer to remove.
/// Removal is per-user (Remove-AppxPackage scoped to the current user); the
/// package stays available in the system store and may be re-provisioned by
/// Windows Update -- which is precisely why each entry can also be set to
/// AutoApply, so the next background tick after a reinstall yanks it again.
/// </summary>
public static class WindowsAiAppCatalog
{
    public static IReadOnlyList<WindowsAiAppDefinition> All { get; } = new WindowsAiAppDefinition[]
    {
        new(
            PackageName: "Microsoft.Copilot",
            DisplayName: "Microsoft Copilot",
            Description: "The standalone Copilot UWP app. Removing it does not affect the in-OS Copilot key combo if you've also flipped the Copilot policy toggle above."),

        new(
            PackageName: "Microsoft.Windows.Ai.Copilot.Provider",
            DisplayName: "Windows AI Copilot Provider",
            Description: "Background provider for the Windows AI Copilot surface. Safe to remove if you don't use Copilot."),

        new(
            PackageName: "MicrosoftWindows.Client.AIX",
            DisplayName: "Windows AI Experience",
            Description: "Windows AI Experience component shipped on Copilot+ PCs. Backs the AI settings panel."),
    };
}
