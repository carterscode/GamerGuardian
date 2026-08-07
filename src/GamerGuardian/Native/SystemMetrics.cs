using System.Runtime.InteropServices;

namespace GamerGuardian.Native;

/// <summary>
/// Installed-memory read. Uses <c>GlobalMemoryStatusEx</c> rather than WMI so the
/// app keeps its "pure user-mode P/Invoke + registry" shape and picks up no new
/// package dependency for one number.
/// </summary>
internal static class SystemMetrics
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    /// <summary>Total installed physical memory in bytes, or null when unreadable.</summary>
    public static ulong? TotalPhysicalBytes()
    {
        try
        {
            var s = new MEMORYSTATUSEX();
            s.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
            return GlobalMemoryStatusEx(ref s) ? s.ullTotalPhys : null;
        }
        catch
        {
            return null;
        }
    }
}
