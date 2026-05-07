using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using GamerGuardian.Models;

namespace GamerGuardian.Services;

/// <summary>
/// Read/modify Windows service start type and running state.
///
/// Reads use <see cref="ServiceController"/> for status and the registry for
/// start type (no elevation needed). Writes use sc.exe via Verb=runas — same
/// pattern as <see cref="ElevatedRegistry"/>, one UAC prompt per call.
/// </summary>
public static class WindowsServiceController
{
    public static bool Exists(string serviceName)
    {
        try
        {
            using var sc = new ServiceController(serviceName);
            // Touching Status forces a query that throws if the service isn't installed.
            _ = sc.Status;
            return true;
        }
        catch { return false; }
    }

    public static ServiceStartType ReadStartType(string serviceName)
    {
        try
        {
            using var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Services\{serviceName}", writable: false);
            if (k is null) return ServiceStartType.Unknown;
            if (k.GetValue("Start") is not int start) return ServiceStartType.Unknown;
            // 2 = Automatic; check DelayedAutostart for the "Automatic (Delayed)" variant.
            if (start == 2)
            {
                var delayed = k.GetValue("DelayedAutostart") is int d && d == 1;
                return delayed ? ServiceStartType.AutomaticDelayed : ServiceStartType.Automatic;
            }
            return start switch
            {
                0 => ServiceStartType.Boot,
                1 => ServiceStartType.System,
                3 => ServiceStartType.Manual,
                4 => ServiceStartType.Disabled,
                _ => ServiceStartType.Unknown,
            };
        }
        catch { return ServiceStartType.Unknown; }
    }

    public static ServiceControllerStatus? ReadStatus(string serviceName)
    {
        try
        {
            using var sc = new ServiceController(serviceName);
            return sc.Status;
        }
        catch { return null; }
    }

    /// <summary>
    /// Stop the service (if running) and set start type to Disabled. Single
    /// UAC prompt; tolerates "already stopped" / "stop pending" exit codes
    /// from sc.exe so the configure step still runs.
    /// </summary>
    public static bool DisableElevated(string serviceName) =>
        RunChained(
            $"sc stop \"{serviceName}\"",
            $"sc config \"{serviceName}\" start= disabled");

    /// <summary>
    /// Stop the service (if running) and set start type to Manual. Useful for
    /// services where Disabled is risky (IP Helper, etc.) — Manual lets a
    /// trigger or app start it on demand but it won't be loaded at boot.
    /// </summary>
    public static bool SetManualElevated(string serviceName) =>
        RunChained(
            $"sc stop \"{serviceName}\"",
            $"sc config \"{serviceName}\" start= demand");

    /// <summary>
    /// Restore a service's start type to its default. Does not start it — the
    /// next reboot or trigger will pick that up if/when needed.
    /// </summary>
    public static bool RestoreDefaultElevated(string serviceName, ServiceStartType defaultStart)
    {
        var startArg = defaultStart switch
        {
            ServiceStartType.Boot => "boot",
            ServiceStartType.System => "system",
            ServiceStartType.Automatic => "auto",
            ServiceStartType.AutomaticDelayed => "delayed-auto",
            ServiceStartType.Manual => "demand",
            ServiceStartType.Disabled => "disabled",
            _ => "demand",
        };
        return Run($"config \"{serviceName}\" start= {startArg}");
    }

    private static bool Run(string scArgs)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = scArgs,
            Verb = "runas",
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        try
        {
            using var p = Process.Start(psi);
            if (p is null) return false;
            p.WaitForExit(10_000);
            return p.HasExited && p.ExitCode == 0;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    private static bool RunChained(params string[] scCommands)
    {
        // Combine into one cmd /c call so the user only sees a single UAC prompt.
        // The first command (typically `sc stop`) is allowed to fail — `&` (not `&&`)
        // means cmd executes the second command regardless.
        var joined = string.Join(" & ", scCommands);
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {joined}",
            Verb = "runas",
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        try
        {
            using var p = Process.Start(psi);
            if (p is null) return false;
            p.WaitForExit(15_000);
            // Last command's exit code wins — that's the `sc config`, which is the one we care about.
            return p.HasExited && p.ExitCode == 0;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }
}
