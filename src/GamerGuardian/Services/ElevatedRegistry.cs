using System.ComponentModel;
using System.Diagnostics;

namespace GamerGuardian.Services;

public static class ElevatedRegistry
{
    public static bool SetHklmDword(string subkey, string value, uint data) =>
        Run($"add \"HKLM\\{subkey}\" /v \"{value}\" /t REG_DWORD /d {data} /f");

    public static bool SetHklmString(string subkey, string value, string data) =>
        Run($"add \"HKLM\\{subkey}\" /v \"{value}\" /t REG_SZ /d \"{data}\" /f");

    /// <summary>
    /// Deletes a single value from an HKLM subkey. Used by policy-based
    /// service overrides when the user wants to restore Windows defaults —
    /// removing the policy value is the documented way to "unset" a Group
    /// Policy entry. Reg.exe returns non-zero if the value doesn't exist;
    /// callers should only invoke this after confirming the value is set.
    /// </summary>
    public static bool DeleteHklmValue(string subkey, string value) =>
        Run($"delete \"HKLM\\{subkey}\" /v \"{value}\" /f");

    public static bool SetHklmMulti(IEnumerable<(string subkey, string name, string kind, string data)> values)
    {
        // Combines multiple writes into a single elevation prompt by chaining them in cmd.
        var sb = new System.Text.StringBuilder();
        foreach (var (subkey, name, kind, data) in values)
        {
            if (sb.Length > 0) sb.Append(" && ");
            var d = kind == "REG_SZ" ? $"\"{data}\"" : data;
            sb.Append($"reg add \"HKLM\\{subkey}\" /v \"{name}\" /t {kind} /d {d} /f");
        }
        return Run(sb.ToString(), useCmd: true);
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
