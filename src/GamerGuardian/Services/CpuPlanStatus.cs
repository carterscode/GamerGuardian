using Microsoft.Win32;

namespace GamerGuardian.Services;

public enum CcdServiceState { Running, Stopped, NotFoundOrUnknown }

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
/// readers. The app can never claim full "optimized" because BIOS CPPC=Driver is
/// not readable from user mode — it surfaces what it can and stays honest.
/// </summary>
public static class CpuPlanStatus
{
    /// <summary>Pure: combine the checkable signals into a status. Unit-tested.</summary>
    public static CcdDependencyStatus DependencyStatus(
        bool planActive, CcdServiceState service, bool? gameBarEnabled)
    {
        if (service == CcdServiceState.NotFoundOrUnknown)
            return CcdDependencyStatus.Unknown;
        if (!planActive || service == CcdServiceState.Stopped)
            return CcdDependencyStatus.PartlyUnmet;
        if (gameBarEnabled == false)
            return CcdDependencyStatus.PartlyUnmet;
        return CcdDependencyStatus.Met;
    }

    // ---- Best-effort readers (not unit-tested) ----

    private static readonly string[] AmdServiceNames =
        { "AMD3DVCacheSvc", "Amd3DVCacheSvc", "AmdV3DCacheSvc", "AMDProvisioningPackagesSvc" };

    public static CcdServiceState ReadAmdVCacheService()
    {
        foreach (var name in AmdServiceNames)
        {
            try
            {
                using var sc = new System.ServiceProcess.ServiceController(name);
                var status = sc.Status; // throws if the service does not exist
                return status == System.ServiceProcess.ServiceControllerStatus.Running
                    ? CcdServiceState.Running
                    : CcdServiceState.Stopped;
            }
            catch { /* not this name; try the next */ }
        }
        return CcdServiceState.NotFoundOrUnknown;
    }

    public static bool? ReadGameBarEnabled()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\GameBar");
            var v = k?.GetValue("AutoGameModeEnabled");
            return v is int i ? i != 0 : null;
        }
        catch { return null; }
    }
}
