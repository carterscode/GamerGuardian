using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace GamerGuardian.Services;

/// <summary>Runtime enabled/disabled state of a scheduled task.</summary>
public enum ScheduledTaskState
{
    NotPresent,
    Enabled,
    Disabled,
}

/// <summary>
/// Read/modify Windows scheduled-task enabled state via schtasks.exe.
///
/// Reads use <c>schtasks /Query</c> without elevation, so drift detection stays
/// promptless. Writes use <c>schtasks /Change /Disable|/Enable</c> via cmd.exe with
/// Verb=runas — same one-UAC-prompt-per-call pattern as <see cref="ElevatedRegistry"/>
/// and <see cref="WindowsServiceController"/>. Absolute System32 paths + a System32
/// working directory stop a planted schtasks.exe/cmd.exe from running with the
/// elevation the user just granted.
/// </summary>
public static class ScheduledTaskController
{
    // Task paths come from ScheduledTaskCatalog (app-controlled), but every segment
    // handed to the elevated cmd.exe is still guarded — matching ElevatedRegistry — so
    // a future caller can't open a command-injection hole. Backslash and space are
    // legal in task paths and intentionally allowed.
    private static readonly char[] ShellMeta = { '&', '|', '<', '>', '^', '"', '`', '%', ';', '(', ')', '\n', '\r' };

    private static void GuardPath(string taskPath)
    {
        if (string.IsNullOrWhiteSpace(taskPath))
            throw new ArgumentException("Task path must be non-empty.", nameof(taskPath));
        if (taskPath.IndexOfAny(ShellMeta) >= 0)
            throw new ArgumentException("Task path contains a disallowed shell metacharacter.", nameof(taskPath));
    }

    /// <summary>
    /// Reads the task's enabled state without elevation via <c>schtasks /Query /XML</c>.
    /// The XML representation is locale-independent (unlike <c>/FO LIST</c>'s localized
    /// "Status:" field). A task that doesn't exist on this build (schtasks returns
    /// non-zero) maps to <see cref="ScheduledTaskState.NotPresent"/>, which the monitor
    /// treats as "not drift".
    /// </summary>
    public static ScheduledTaskState QueryState(string taskPath)
    {
        try
        {
            var system32 = Environment.SystemDirectory;
            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(system32, "schtasks.exe"),
                Arguments = $"/Query /TN \"{taskPath}\" /XML ONE",
                WorkingDirectory = system32,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                // stderr is intentionally NOT redirected: we don't use it, and an
                // un-drained redirected stderr pipe can deadlock the child if it
                // writes more than the pipe buffer. Inheriting (no console -> discarded)
                // is safe and avoids the deadlock.
                RedirectStandardError = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return ScheduledTaskState.NotPresent;
            var stdout = p.StandardOutput.ReadToEnd();
            p.WaitForExit(10_000);
            if (!p.HasExited) { try { p.Kill(entireProcessTree: true); } catch { } return ScheduledTaskState.NotPresent; }
            if (p.ExitCode != 0) return ScheduledTaskState.NotPresent;
            return ParseEnabledState(stdout);
        }
        catch
        {
            return ScheduledTaskState.NotPresent;
        }
    }

    /// <summary>
    /// Parses <c>schtasks /Query ... /XML</c> output and returns the task's state from
    /// the locale-independent <c>Task/Settings/Enabled</c> element: <c>false</c> means
    /// Disabled; <c>true</c> or absent means Enabled. Trigger elements can also carry an
    /// <c>Enabled</c> flag, so this targets <c>Settings</c> specifically. Exposed for
    /// unit testing without spawning schtasks.
    /// </summary>
    public static ScheduledTaskState ParseEnabledState(string taskXml)
    {
        if (string.IsNullOrWhiteSpace(taskXml)) return ScheduledTaskState.NotPresent;
        try
        {
            var root = XDocument.Parse(taskXml).Root;
            if (root is null) return ScheduledTaskState.NotPresent;
            var ns = root.Name.Namespace;
            var enabled = root.Element(ns + "Settings")?.Element(ns + "Enabled");
            return enabled is not null
                   && string.Equals(enabled.Value.Trim(), "false", StringComparison.OrdinalIgnoreCase)
                ? ScheduledTaskState.Disabled
                : ScheduledTaskState.Enabled;
        }
        catch
        {
            return ScheduledTaskState.NotPresent;
        }
    }

    /// <summary>Disable one or more tasks in a single elevation (one UAC prompt).</summary>
    public static bool DisableElevated(IEnumerable<string> taskPaths) =>
        RunBatch(BuildChangeBatch(taskPaths, enable: false));

    /// <summary>Re-enable one or more tasks in a single elevation (one UAC prompt).</summary>
    public static bool EnableElevated(IEnumerable<string> taskPaths) =>
        RunBatch(BuildChangeBatch(taskPaths, enable: true));

    /// <summary>
    /// Builds the chained <c>schtasks /Change</c> command string (one entry per task,
    /// joined with <c>&amp;</c> so every task is attempted regardless of prior failures —
    /// a task that vanished between snapshot and approval can't abort the rest). Exposed
    /// for unit testing the command shape and the injection guard without spawning an
    /// elevated process. Returns empty string when no tasks are supplied.
    /// </summary>
    public static string BuildChangeBatch(IEnumerable<string> taskPaths, bool enable)
    {
        var flag = enable ? "/Enable" : "/Disable";
        var sb = new StringBuilder();
        foreach (var path in taskPaths)
        {
            GuardPath(path);
            if (sb.Length > 0) sb.Append(" & ");
            sb.Append($"schtasks /Change /TN \"{path}\" {flag}");
        }
        return sb.ToString();
    }

    private static bool RunBatch(string command)
    {
        if (string.IsNullOrEmpty(command)) return true;
        var system32 = Environment.SystemDirectory;
        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(system32, "cmd.exe"),
            Arguments = $"/c {command}",
            WorkingDirectory = system32,
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
            if (!p.HasExited) { try { p.Kill(entireProcessTree: true); } catch { } return false; }
            return p.ExitCode == 0;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }
}
