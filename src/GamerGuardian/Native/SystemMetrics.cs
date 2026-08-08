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

    // 'R','S','M','B' — the raw SMBIOS firmware table provider.
    private const uint RawSmbiosProvider = 0x52534D42;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetSystemFirmwareTable(
        uint firmwareTableProviderSignature,
        uint firmwareTableId,
        byte[]? firmwareTableBuffer,
        uint bufferSize);

    /// <summary>
    /// The raw SMBIOS table, or null when the firmware does not expose one. Called
    /// twice as the API requires: once with a null buffer to learn the size, once to
    /// fill it. Parsing lives in <see cref="Smbios"/> so it stays testable.
    /// </summary>
    public static byte[]? ReadRawSmbios()
    {
        try
        {
            uint size = GetSystemFirmwareTable(RawSmbiosProvider, 0, null, 0);
            if (size == 0) return null;

            var buffer = new byte[size];
            uint written = GetSystemFirmwareTable(RawSmbiosProvider, 0, buffer, size);
            // A second call returning 0, or more than we allocated, means the table
            // changed or the call failed; either way there is nothing safe to parse.
            return written == 0 || written > size ? null : buffer;
        }
        catch
        {
            return null;
        }
    }
}
