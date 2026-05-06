using System.IO;

namespace GamerGuardian.Services;

/// <summary>
/// Best-effort cleanup of stale GamerGuardian artifacts in %TEMP%:
///  - Old installer EXEs left behind by the auto-update flow.
///  - Old trace.log files from earlier dev builds.
/// Files newer than 1 day are kept so an in-progress install isn't disturbed.
/// </summary>
public static class TempCleanup
{
    private const int KeepDays = 1;

    public static void Run()
    {
        try
        {
            var temp = Path.GetTempPath();
            var cutoff = DateTime.Now.AddDays(-KeepDays);

            foreach (var path in Directory.EnumerateFiles(temp, "GamerGuardian-Setup-*.exe"))
            {
                TryDeleteIfOlder(path, cutoff);
            }

            var stale = Path.Combine(temp, "gamerguardian_trace.log");
            TryDeleteIfOlder(stale, DateTime.MaxValue); // always remove — code no longer writes it
        }
        catch { /* best-effort */ }
    }

    private static void TryDeleteIfOlder(string path, DateTime cutoff)
    {
        try
        {
            var fi = new FileInfo(path);
            if (!fi.Exists) return;
            if (fi.LastWriteTime > cutoff) return;
            fi.Delete();
        }
        catch { }
    }
}
