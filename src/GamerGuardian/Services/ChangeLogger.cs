using System.IO;
using GamerGuardian.Models;

namespace GamerGuardian.Services;

/// <summary>
/// Persists every applied change (manual or auto) to %APPDATA%\GamerGuardian\changes.log
/// in append-only plain text. Rotated when the file grows past ~1 MB.
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

            var lines = results.Select(r => Format(r, source));
            File.AppendAllLines(LogPath, lines, System.Text.Encoding.UTF8);
        }
        catch { /* logging is best-effort */ }
    }

    private static string Format(ApplyResult r, string source)
    {
        var status = r.Verified ? "OK" : "FAILED";
        var reboot = r.RequiresReboot ? " | reboot pending" : "";
        return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source,-6}] {status,-6} {r.Description}  |  {r.Before} -> {r.Desired}  |  now: {r.After}{reboot}";
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
