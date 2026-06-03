using Microsoft.Win32;

namespace GamerGuardian.Services;

/// <summary>
/// Lightweight CPU introspection used by the Recommended preset to pick a
/// CPU-appropriate power plan and (in the future) other CPU-aware defaults.
/// Reads from the HARDWARE\DESCRIPTION\System\CentralProcessor\0 registry
/// key -- fast (microseconds), no WMI, no admin.
///
/// <para>The single value we currently key off is the friendly name string
/// (e.g. <c>"AMD Ryzen 9 9950X3D 16-Core Processor"</c>), specifically the
/// <c>X3D</c> substring. AMD's 3D V-Cache SKUs (5800X3D, 7800X3D, 9800X3D,
/// 7950X3D, 9950X3D, etc.) all end in X3D in the OEM name string, and AMD
/// officially recommends the Balanced power plan for them rather than High
/// Performance because the V-Cache CCD has a lower max clock and aggressive
/// CPPC scheduling matters more than raw clock-pegging.</para>
/// </summary>
public static class CpuInfo
{
    /// <summary>
    /// Returns the registry's ProcessorNameString (trimmed), or null if
    /// the registry key isn't readable. Example:
    /// <c>"AMD Ryzen 9 9950X3D 16-Core Processor"</c>.
    /// </summary>
    public static string? GetName()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(
                @"HARDWARE\DESCRIPTION\System\CentralProcessor\0", writable: false);
            return (k?.GetValue("ProcessorNameString") as string)?.Trim();
        }
        catch { return null; }
    }

    /// <summary>
    /// True when the CPU name contains "X3D" (AMD's 3D V-Cache marker).
    /// Matches 5800X3D, 7800X3D, 9800X3D, 7900X3D, 7950X3D, 9900X3D,
    /// 9950X3D, and any future X3D SKU.
    /// </summary>
    public static bool IsAmdX3D(string? cpuName) =>
        !string.IsNullOrEmpty(cpuName) &&
        cpuName.Contains("X3D", StringComparison.OrdinalIgnoreCase);
}
