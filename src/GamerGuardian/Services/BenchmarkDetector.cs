using System.Diagnostics;

namespace GamerGuardian.Services;

public static class BenchmarkDetector
{
    private static readonly HashSet<string> KnownProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "3DMark", "3DMarkLauncher", "3DMarkCmd",
        "PCMark10", "PCMark10Cmd",
        "Cinebench", "Cinebench R23", "CinebenchR23", "Cinebench2024",
        "geekbench", "geekbench4", "geekbench5", "geekbench6",
        "aida64", "aida_bench64",
        "FurMark", "FurMark2",
        "Heaven", "Valley", "superposition",
        "OCCT", "OCCTPT", "OCCT_Tester",
        "prime95",
        "y-cruncher",
        "DiskMark64", "DiskMark32",
        "NovaBench",
        "BasemarkGPU",
        "PerformanceTest", "passmark",
        "RealBench",
        "Time Spy", "TimeSpy", "PortRoyal",
        "VRMark",
        "BlenderBenchmark",
        "phoronix-test-suite",
        "linpack", "intelxtu",
        "memtest86", "memtestpro",
    };

    public static string? GetRunningBenchmark()
    {
        Process[] all;
        try { all = Process.GetProcesses(); }
        catch { return null; }

        try
        {
            foreach (var p in all)
            {
                try
                {
                    if (KnownProcessNames.Contains(p.ProcessName))
                        return p.ProcessName;
                }
                catch { /* process may have exited mid-iteration */ }
            }
        }
        finally
        {
            foreach (var p in all)
            {
                try { p.Dispose(); } catch { }
            }
        }
        return null;
    }
}
