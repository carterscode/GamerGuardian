using System.Runtime.InteropServices;

namespace GamerGuardian.Native;

internal static class Powrprof
{
    public const uint ERROR_SUCCESS = 0;
    public const uint ACCESS_SCHEME = 16;

    [DllImport("powrprof.dll")]
    public static extern uint PowerGetActiveScheme(IntPtr UserRootPowerKey, out IntPtr ActivePolicyGuid);

    [DllImport("powrprof.dll")]
    public static extern uint PowerSetActiveScheme(IntPtr UserRootPowerKey, ref Guid SchemeGuid);

    [DllImport("powrprof.dll")]
    public static extern uint PowerEnumerate(
        IntPtr RootPowerKey,
        IntPtr SchemeGuid,
        IntPtr SubGroupOfPowerSettingsGuid,
        uint AccessFlags,
        uint Index,
        IntPtr Buffer,
        ref uint BufferSize);

    [DllImport("powrprof.dll", CharSet = CharSet.Unicode)]
    public static extern uint PowerReadFriendlyName(
        IntPtr RootPowerKey,
        ref Guid SchemeGuid,
        IntPtr SubGroupOfPowerSettingsGuid,
        IntPtr PowerSettingGuid,
        IntPtr Buffer,
        ref uint BufferSize);

    [DllImport("kernel32.dll")]
    public static extern IntPtr LocalFree(IntPtr hMem);

    public static Guid GetActiveScheme()
    {
        if (PowerGetActiveScheme(IntPtr.Zero, out var ptr) != ERROR_SUCCESS || ptr == IntPtr.Zero)
            return Guid.Empty;
        try { return Marshal.PtrToStructure<Guid>(ptr); }
        finally { LocalFree(ptr); }
    }

    public static bool SetActiveScheme(Guid g) =>
        PowerSetActiveScheme(IntPtr.Zero, ref g) == ERROR_SUCCESS;

    public static IEnumerable<Guid> EnumerateSchemes()
    {
        int guidSize = Marshal.SizeOf<Guid>();
        IntPtr buffer = Marshal.AllocHGlobal(guidSize);
        try
        {
            for (uint i = 0; ; i++)
            {
                uint size = (uint)guidSize;
                var rc = PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ACCESS_SCHEME, i, buffer, ref size);
                if (rc != ERROR_SUCCESS) yield break;
                yield return Marshal.PtrToStructure<Guid>(buffer);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static string? ReadFriendlyName(Guid scheme)
    {
        uint size = 0;
        PowerReadFriendlyName(IntPtr.Zero, ref scheme, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref size);
        if (size == 0) return null;
        var buf = Marshal.AllocHGlobal((int)size);
        try
        {
            if (PowerReadFriendlyName(IntPtr.Zero, ref scheme, IntPtr.Zero, IntPtr.Zero, buf, ref size) != ERROR_SUCCESS)
                return null;
            return Marshal.PtrToStringUni(buf);
        }
        finally
        {
            Marshal.FreeHGlobal(buf);
        }
    }
}
