using System.Diagnostics;
using System.Text;
using GamerGuardian.Models;

namespace GamerGuardian.Monitors;

/// <summary>
/// Monitors a single Windows-AI UWP package. One instance per
/// <see cref="WindowsAiAppDefinition"/>; registered N times in <c>App.xaml.cs</c>
/// from <see cref="Services.WindowsAiAppCatalog.All"/>, mirroring the
/// <see cref="WindowsServiceMonitor"/> pattern.
///
/// <para>Drift surfaces when the user has marked the package for removal
/// (<see cref="WindowsAiAppPref.DesiredRemoved"/> = true) and the package is
/// still installed for the current user. The Apply lambda shells out to
/// <c>powershell.exe Remove-AppxPackage</c> -- AppX APIs require WinRT bindings
/// the rest of the app doesn't pull in, and shelling out costs ~150 ms once.</para>
///
/// <para>There is intentionally no "re-install" drift: GamerGuardian can't
/// restore a removed UWP package, and pretending to "drift toward installed"
/// would just spam notifications. Users who change their mind reinstall via
/// the Microsoft Store.</para>
/// </summary>
public sealed class WindowsAiAppMonitor : IMonitoredSetting
{
    private readonly WindowsAiAppDefinition _def;

    public WindowsAiAppMonitor(WindowsAiAppDefinition def) { _def = def; }

    public string Id => $"ai.app:{_def.PackageName}";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        if (!config.WindowsAiApps.TryGetValue(_def.PackageName, out var pref) || pref is null)
            yield break;
        if (!pref.DesiredRemoved) yield break;

        bool? installed = IsInstalled(_def.PackageName);
        if (installed != true) yield break;  // already gone -> no drift

        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "ai-app",
            DisplayLabel: _def.DisplayName,
            Description: $"{_def.DisplayName} -- uninstall UWP package",
            CurrentValue: "Installed",
            DesiredValue: "Removed",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Remove(_def.PackageName)),
            IsMonitored: pref.Monitor,
            RawBefore: "(installed for current user)",
            RawDesired: "(Remove-AppxPackage)");
    }

    /// <summary>
    /// Cheap probe: piping Get-AppxPackage through PowerShell costs ~120 ms; we
    /// run it once per monitor per tick and accept that. Returns null if the
    /// probe itself fails (rare -- PowerShell missing).
    /// </summary>
    public static bool? IsInstalled(string packageName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"if (Get-AppxPackage -Name '{packageName.Replace("'", "''")}' -ErrorAction SilentlyContinue) {{ 'yes' }} else {{ 'no' }}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return null;
            if (!p.WaitForExit(10_000))
            {
                try { p.Kill(); } catch { }
                return null;
            }
            var output = p.StandardOutput.ReadToEnd().Trim();
            return output.Equals("yes", StringComparison.OrdinalIgnoreCase) ? true
                 : output.Equals("no", StringComparison.OrdinalIgnoreCase) ? false
                 : (bool?)null;
        }
        catch { return null; }
    }

    /// <summary>
    /// Removes the package for the current user. Not run elevated -- AppX
    /// removal is user-scoped by default and works without UAC for non-system
    /// packages. For provisioned packages that re-appear after Windows Update,
    /// users should also tick AutoApply so the next tick removes them again.
    /// </summary>
    public static bool Remove(string packageName)
    {
        try
        {
            var sb = new StringBuilder();
            sb.Append("-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"");
            sb.Append($"Get-AppxPackage -Name '{packageName.Replace("'", "''")}' -ErrorAction SilentlyContinue | ");
            sb.Append("Remove-AppxPackage -ErrorAction Stop");
            sb.Append('"');
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = sb.ToString(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return false;
            p.WaitForExit(30_000);
            return p.HasExited && p.ExitCode == 0;
        }
        catch { return false; }
    }
}
