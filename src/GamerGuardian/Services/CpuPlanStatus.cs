using GamerGuardian.Monitors;
using Microsoft.Win32;

namespace GamerGuardian.Services;

/// <summary>
/// What the app could learn about the AMD 3D V-Cache Optimizer service.
///
/// <para><see cref="Stopped"/> is split from <see cref="Disabled"/> deliberately.
/// The service is installed with StartMode=Auto and sits Stopped until it has
/// routing work to do, so "stopped" is its normal idle state and is not something
/// the user needs to fix. Only a Disabled start mode is actually broken.</para>
/// </summary>
public enum CcdServiceState
{
    /// <summary>No service matching the optimizer was found at all.</summary>
    NotInstalled,
    /// <summary>Present and currently running.</summary>
    Running,
    /// <summary>Present, start mode is Automatic or Manual, not running right now.
    /// This is the expected idle state, not a fault.</summary>
    Idle,
    /// <summary>Present but its start mode is Disabled, so it can never run.</summary>
    Disabled,
}

/// <summary>What was found, so the UI can name it instead of guessing.</summary>
/// <param name="State">Installed/running/idle/disabled.</param>
/// <param name="ServiceName">The service's real name, or null when nothing matched.</param>
/// <param name="DisplayName">Its display name, or null.</param>
public sealed record CcdServiceInfo(CcdServiceState State, string? ServiceName, string? DisplayName);

/// <summary>Status of the asymmetric dual-CCD X3D routing dependencies.</summary>
public enum CcdDependencyStatus
{
    /// <summary>Checkable dependencies look good (BIOS CPPC still unverifiable —
    /// never an unqualified "optimized").</summary>
    Met,
    /// <summary>At least one checkable dependency is unmet.</summary>
    PartlyUnmet,
    /// <summary>Can't tell (the AMD service isn't installed/readable).</summary>
    Unknown,
}

/// <summary>
/// Pure status logic for the dual-CCD X3D dependency stack, plus best-effort
/// readers. The app can never claim full "optimized" because the BIOS CPPC setting
/// is not readable from user mode — it surfaces what it can and stays honest.
/// </summary>
public static class CpuPlanStatus
{
    /// <summary>Pure: combine the checkable signals into a status. Unit-tested.</summary>
    public static CcdDependencyStatus DependencyStatus(
        bool planActive, CcdServiceState service, bool? gameBarEnabled)
    {
        if (service == CcdServiceState.NotInstalled)
            return CcdDependencyStatus.Unknown;
        // A Disabled start mode is the only service state the user must act on.
        // Idle is normal: the optimizer is demand-driven and sits stopped until a
        // game gives it something to route. Reporting that as an unmet dependency
        // sent people off to reinstall drivers that were already installed.
        if (!planActive || service == CcdServiceState.Disabled)
            return CcdDependencyStatus.PartlyUnmet;
        if (gameBarEnabled == false)
            return CcdDependencyStatus.PartlyUnmet;
        return CcdDependencyStatus.Met;
    }

    // ---- Best-effort readers (not unit-tested) ----

    /// <summary>
    /// Finds the AMD 3D V-Cache Optimizer service by enumerating the service list
    /// and matching, rather than probing a list of guessed names.
    ///
    /// <para>The previous version tried four hardcoded names, two of which
    /// ("AmdV3DCacheSvc", "AMDProvisioningPackagesSvc") do not exist on a real
    /// install — the actual names are <c>amd3dvcacheSvc</c> and <c>AmdPpkgSvc</c>.
    /// Any name AMD ships that a future driver renames would break it again, and the
    /// failure mode was the worst possible one: "not detected (install AMD chipset
    /// drivers)" on a machine where they were already installed.</para>
    /// </summary>
    public static CcdServiceInfo ReadAmdVCacheService()
    {
        try
        {
            foreach (var sc in System.ServiceProcess.ServiceController.GetServices())
            {
                using (sc)
                {
                    if (!LooksLikeVCacheOptimizer(sc.ServiceName, sc.DisplayName)) continue;

                    var state = sc.Status == System.ServiceProcess.ServiceControllerStatus.Running
                        ? CcdServiceState.Running
                        : IsDisabled(sc.ServiceName) ? CcdServiceState.Disabled : CcdServiceState.Idle;

                    return new CcdServiceInfo(state, sc.ServiceName, sc.DisplayName);
                }
            }
        }
        catch { /* enumeration denied or unavailable -- fall through */ }

        return new CcdServiceInfo(CcdServiceState.NotInstalled, null, null);
    }

    /// <summary>Matches the optimizer by service name or display name. Pure, so the
    /// matching rule is unit-tested without touching the service manager.</summary>
    public static bool LooksLikeVCacheOptimizer(string? serviceName, string? displayName)
    {
        // Match on the distinctive part rather than a full name: the shipped service
        // is "amd3dvcacheSvc" / "AMD 3D V-Cache Performance Optimizer Service", and
        // matching the middle survives casing and Svc/Service suffix changes.
        return Contains(serviceName, "3dvcache")
               || Contains(displayName, "3d v-cache")
               || Contains(displayName, "3d vcache");

        static bool Contains(string? haystack, string needle) =>
            haystack is not null &&
            haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Start mode from the registry. ServiceController exposes StartType
    /// only on .NET Core 3.0+ for some platforms, and reading the key is the same
    /// registry-first approach the rest of the app uses.</summary>
    private static bool IsDisabled(string serviceName)
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Services\{serviceName}", writable: false);
            // 4 == SERVICE_DISABLED
            return k?.GetValue("Start") is int start && start == 4;
        }
        catch { return false; }
    }

    /// <summary>
    /// Whether Windows Game Mode is on — the signal the AMD optimizer's game
    /// detection rides on.
    ///
    /// <para>Reuses <see cref="GameModeMonitor.ReadCurrent"/> rather than reading the
    /// registry again. The duplicate reader here checked only
    /// <c>AutoGameModeEnabled</c> and returned null when it was absent, which is why
    /// the panel said "unknown" on machines that had simply never had the value
    /// written. Absent now resolves to the Windows default instead.</para>
    /// </summary>
    public static bool? ReadGameBarEnabled()
    {
        try
        {
            // Neither value present means the user has never changed it, so the
            // effective state is the Windows 11 default: Game Mode on.
            return GameModeMonitor.ReadCurrent() ?? true;
        }
        catch { return null; }
    }
}
