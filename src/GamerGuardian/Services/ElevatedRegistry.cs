using System.ComponentModel;
using System.Diagnostics;

namespace GamerGuardian.Services;

public static class ElevatedRegistry
{
    public static bool SetHklmDword(string subkey, string value, uint data)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "reg.exe",
            Arguments = $"add \"HKLM\\{subkey}\" /v \"{value}\" /t REG_DWORD /d {data} /f",
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
