using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace GamerGuardian.Services;

public static class ElevatedRegistry
{
    // Characters that must never appear in a registry path/value/data we hand to an
    // elevated cmd.exe. The batched helpers chain commands with " && " inside a single
    // UAC-approved cmd.exe; an unescaped one of these in any caller-supplied segment
    // could break out of the intended reg command and run arbitrary code with the
    // elevation the user just granted. All current callers pass GUID-shaped or static
    // paths and numeric/short data, so this guard never trips in practice — it exists so
    // a future caller can't silently open a command-injection hole.
    private static readonly char[] ShellMeta = { '&', '|', '<', '>', '^', '"', '`', '%', ';', '\n', '\r' };

    private static void GuardSegment(string? segment, string paramName)
    {
        if (string.IsNullOrEmpty(segment))
            throw new ArgumentException($"Registry segment '{paramName}' must be non-empty.", paramName);
        if (segment.IndexOfAny(ShellMeta) >= 0)
            throw new ArgumentException(
                $"Registry segment '{paramName}' contains a disallowed shell metacharacter.", paramName);
    }

    public static bool SetHklmDword(string subkey, string value, uint data)
    {
        GuardSegment(subkey, nameof(subkey));
        GuardSegment(value, nameof(value));
        return Run($"add \"HKLM\\{subkey}\" /v \"{value}\" /t REG_DWORD /d {data} /f");
    }

    public static bool SetHklmString(string subkey, string value, string data)
    {
        GuardSegment(subkey, nameof(subkey));
        GuardSegment(value, nameof(value));
        GuardSegment(data, nameof(data));
        return Run($"add \"HKLM\\{subkey}\" /v \"{value}\" /t REG_SZ /d \"{data}\" /f");
    }

    /// <summary>
    /// Deletes a single value from an HKLM subkey. Used by policy-based
    /// service overrides when the user wants to restore Windows defaults —
    /// removing the policy value is the documented way to "unset" a Group
    /// Policy entry. Reg.exe returns non-zero if the value doesn't exist;
    /// callers should only invoke this after confirming the value is set.
    /// </summary>
    public static bool DeleteHklmValue(string subkey, string value)
    {
        GuardSegment(subkey, nameof(subkey));
        GuardSegment(value, nameof(value));
        return Run($"delete \"HKLM\\{subkey}\" /v \"{value}\" /f");
    }

    /// <summary>
    /// Combines multiple HKLM writes into a single elevation prompt by chaining
    /// them in cmd.exe. Use this instead of N <see cref="SetHklmDword"/> calls
    /// when one logical setting writes several values (e.g. the full Game DVR
    /// lockdown or per-interface Nagle), so the user sees one UAC prompt.
    /// </summary>
    public static bool SetHklmMulti(IEnumerable<(string subkey, string name, string kind, string data)> values)
    {
        var cmd = BuildHklmMultiAdd(values);
        return cmd.Length == 0 || Run(cmd, useCmd: true);
    }

    /// <summary>
    /// Deletes multiple HKLM values in a single elevation prompt. The reversal
    /// counterpart to <see cref="SetHklmMulti"/>: a multi-value or multi-interface
    /// reversal (e.g. removing TcpAckFrequency + TCPNoDelay across N adapters)
    /// stays one UAC prompt instead of one per value.
    /// </summary>
    public static bool DeleteHklmMulti(IEnumerable<(string subkey, string name)> values)
    {
        var cmd = BuildHklmMultiDelete(values);
        return cmd.Length == 0 || Run(cmd, useCmd: true);
    }

    /// <summary>
    /// Mixed add + delete batch in a single elevation prompt. Used when one logical
    /// setting both writes values and removes others (e.g. the VBS disable: zeros
    /// across DeviceGuard plus deletion of the HVCI re-enable metadata). Adds run
    /// before deletes; cmd.exe chains with <c>&amp;&amp;</c>, so callers must only
    /// pass deletes for values confirmed present (a failed delete aborts the rest).
    /// </summary>
    public static bool ApplyHklmBatch(
        IEnumerable<(string subkey, string name, string kind, string data)> adds,
        IEnumerable<(string subkey, string name)> deletes)
    {
        var cmd = BuildHklmBatch(adds, deletes);
        return cmd.Length == 0 || Run(cmd, useCmd: true);
    }

    /// <summary>
    /// Builds the chained command string for <see cref="ApplyHklmBatch"/> (adds first,
    /// then deletes). Exposed for unit testing the command shape and the injection
    /// guard without spawning an elevated process.
    /// </summary>
    public static string BuildHklmBatch(
        IEnumerable<(string subkey, string name, string kind, string data)> adds,
        IEnumerable<(string subkey, string name)> deletes)
    {
        var add = BuildHklmMultiAdd(adds);
        var del = BuildHklmMultiDelete(deletes);
        if (add.Length == 0) return del;
        if (del.Length == 0) return add;
        return add + " && " + del;
    }

    /// <summary>
    /// Builds the chained <c>reg add</c> command string for <see cref="SetHklmMulti"/>.
    /// Exposed for unit testing the command shape and the injection guard without
    /// spawning an elevated process. Throws if any segment contains a shell metacharacter.
    /// </summary>
    public static string BuildHklmMultiAdd(IEnumerable<(string subkey, string name, string kind, string data)> values)
    {
        var sb = new StringBuilder();
        foreach (var (subkey, name, kind, data) in values)
        {
            GuardSegment(subkey, nameof(subkey));
            GuardSegment(name, nameof(name));
            GuardSegment(data, nameof(data));
            if (sb.Length > 0) sb.Append(" && ");
            var d = kind == "REG_SZ" ? $"\"{data}\"" : data;
            sb.Append($"reg add \"HKLM\\{subkey}\" /v \"{name}\" /t {kind} /d {d} /f");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Builds the chained <c>reg delete</c> command string for <see cref="DeleteHklmMulti"/>.
    /// Exposed for unit testing the command shape and the injection guard without
    /// spawning an elevated process. Throws if any segment contains a shell metacharacter.
    /// </summary>
    public static string BuildHklmMultiDelete(IEnumerable<(string subkey, string name)> values)
    {
        var sb = new StringBuilder();
        foreach (var (subkey, name) in values)
        {
            GuardSegment(subkey, nameof(subkey));
            GuardSegment(name, nameof(name));
            if (sb.Length > 0) sb.Append(" && ");
            sb.Append($"reg delete \"HKLM\\{subkey}\" /v \"{name}\" /f");
        }
        return sb.ToString();
    }

    private static bool Run(string args, bool useCmd = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = useCmd ? "cmd.exe" : "reg.exe",
            Arguments = useCmd ? $"/c {args}" : args,
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
}
