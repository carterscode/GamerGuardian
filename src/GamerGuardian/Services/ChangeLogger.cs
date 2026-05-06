using System.IO;
using System.Text;
using GamerGuardian.Models;

namespace GamerGuardian.Services;

/// <summary>
/// Persists every applied change (manual or auto) to %APPDATA%\GamerGuardian\changes.log
/// in append-only multi-line plain text. Rotated when the file grows past ~1 MB.
/// Each entry includes timestamp, source, status, the exact registry path / API
/// being touched, the raw before/after values, and a verify-yourself snippet.
/// </summary>
public static class ChangeLogger
{
    public static string LogPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GamerGuardian", "changes.log");

    private const long MaxBytes = 1_000_000;

    public static void LogApplyResults(IEnumerable<ApplyResult> results, string source)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            RotateIfNeeded();

            var sb = new StringBuilder();
            foreach (var r in results)
            {
                sb.Append(Format(r, source));
            }
            File.AppendAllText(LogPath, sb.ToString(), Encoding.UTF8);
        }
        catch { /* logging is best-effort */ }
    }

    /// <summary>
    /// Records a preference toggle (Monitor / AutoApply / Want) made in the Settings UI.
    /// Single line per change so the log stays scannable.
    /// </summary>
    public static void LogPreferenceChange(string settingName, string field, string before, string after)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            RotateIfNeeded();
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ui    ] PREF   {settingName}  |  {field}: {before} -> {after}";
            File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
        }
        catch { }
    }

    /// <summary>
    /// Records the start or end of a polling pause (fullscreen / benchmark / user).
    /// </summary>
    public static void LogPauseEvent(string action, string reason)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            RotateIfNeeded();
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [pause ] {action,-7} {reason}";
            File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
        }
        catch { }
    }

    private static string Format(ApplyResult r, string source)
    {
        var status = r.Verified ? "OK" : "FAILED";
        var sb = new StringBuilder();
        sb.AppendLine("--------------------------------------------------------------------------------");
        sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source,-6}] {status,-6} {r.Description}");
        sb.AppendLine($"  Mechanism : {r.Mechanism}");
        if (!string.IsNullOrEmpty(r.RawBefore))
            sb.AppendLine($"  Before    : {r.RawBefore}  ({r.Before})");
        else
            sb.AppendLine($"  Before    : {r.Before}");
        if (!string.IsNullOrEmpty(r.RawDesired))
            sb.AppendLine($"  Wrote     : {r.RawDesired}  ({r.Desired})");
        else
            sb.AppendLine($"  Wrote     : {r.Desired}");
        if (!string.IsNullOrEmpty(r.RawAfter))
            sb.AppendLine($"  After     : {r.RawAfter}  ({r.After})  {(r.Verified ? "<- verified" : "<- NOT VERIFIED")}");
        else
            sb.AppendLine($"  After     : {r.After}  {(r.Verified ? "<- verified" : "<- NOT VERIFIED")}");
        if (r.RequiresReboot)
            sb.AppendLine($"  Reboot    : required to take effect");
        if (!string.IsNullOrEmpty(r.VerifyCommand))
            sb.AppendLine($"  Verify    : {r.VerifyCommand}");
        return sb.ToString();
    }

    private static void RotateIfNeeded()
    {
        try
        {
            var fi = new FileInfo(LogPath);
            if (!fi.Exists || fi.Length < MaxBytes) return;
            var prev = LogPath + ".1";
            if (File.Exists(prev)) File.Delete(prev);
            File.Move(LogPath, prev);
        }
        catch { }
    }
}
