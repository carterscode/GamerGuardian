using GamerGuardian.Models;

namespace GamerGuardian.Services;

/// <summary>
/// The Windows "Application Experience" scheduled tasks GamerGuardian can keep
/// disabled. These tasks drive CompatTelRunner.exe (the "Microsoft Compatibility
/// Telemetry" process), whose periodic compatibility/telemetry scans spike CPU and
/// disk — an unpredictable hitch source during gaming. Disabling them is safe
/// (Windows Update keeps working) and reversible.
///
/// All entries carry <see cref="ScheduledTaskTarget.Disabled"/> as their
/// RecommendedTarget, so the one-click Recommended preset disables the whole family.
/// The two functional tasks (StartupAppTask, PcaPatchDbTask) are included per the
/// design decision to cover the whole folder; their minor functional cost is called
/// out in each entry's description and SettingDocs.
///
/// Catalog-driven monitors are projected in App.xaml.cs, not added to fixedMonitors.
/// </summary>
public static class ScheduledTaskCatalog
{
    private const string Folder = @"\Microsoft\Windows\Application Experience\";

    public static IReadOnlyList<ScheduledTaskDefinition> All { get; } = new ScheduledTaskDefinition[]
    {
        new(
            TaskPath: Folder + "Microsoft Compatibility Appraiser",
            DisplayName: "Microsoft Compatibility Appraiser",
            Description: "Runs CompatTelRunner.exe to scan installed apps/drivers and write Windows upgrade-readiness + telemetry markers. The main source of the periodic 'Microsoft Compatibility Telemetry' CPU/disk spikes. Safe to disable on a gaming PC; you only lose pre-update compatibility checks.",
            RecommendedTarget: ScheduledTaskTarget.Disabled),

        new(
            TaskPath: Folder + "Microsoft Compatibility Appraiser Exp",
            DisplayName: "Microsoft Compatibility Appraiser (Exp)",
            Description: "An experimental variant of the Compatibility Appraiser on newer Windows 11 builds; same telemetry/compat scan. Absent on some builds — it's only disabled if present.",
            RecommendedTarget: ScheduledTaskTarget.Disabled),

        new(
            TaskPath: Folder + "ProgramDataUpdater",
            DisplayName: "ProgramDataUpdater",
            Description: "Collects program-inventory telemetry for the Application Experience pipeline. Background data collection with no user-facing function; safe to disable.",
            RecommendedTarget: ScheduledTaskTarget.Disabled),

        new(
            TaskPath: Folder + "StartupAppTask",
            DisplayName: "StartupAppTask",
            Description: "Scans startup apps for the Application Experience pipeline. A functional helper (not pure telemetry); disabling it stops the periodic startup-app scan. Minor cost: Windows may not refresh startup-impact data.",
            RecommendedTarget: ScheduledTaskTarget.Disabled),

        new(
            TaskPath: Folder + "PcaPatchDbTask",
            DisplayName: "PcaPatchDbTask",
            Description: "Updates the Program Compatibility Assistant patch database. A functional helper (not pure telemetry); disabling it stops PCA database refreshes. Minor cost: PCA may apply fewer app-compat shims.",
            RecommendedTarget: ScheduledTaskTarget.Disabled),
    };
}
