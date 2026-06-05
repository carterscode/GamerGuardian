using System.Runtime.InteropServices;
using System.Text;

namespace GamerGuardian.Native;

internal static class Powrprof
{
    public const uint ERROR_SUCCESS = 0;
    public const uint ACCESS_SCHEME = 16;

    // ---- Well-known processor power subgroup + setting GUIDs ----
    // These are Microsoft well-known POWER SETTING GUIDs (documented in the
    // Windows SDK / powrprof.h), distinct from the per-machine SCHEME GUIDs that
    // the "never hardcode scheme GUIDs" rule governs. Setting GUIDs are stable
    // across machines and safe to hardcode.
    public static readonly Guid SubProcessor = new("54533251-82be-4824-96c1-47b60b740d00");
    public static readonly Guid SettingCoreParkingMinCores = new("0cc5b647-c1df-4637-891a-dec35c318583");
    public static readonly Guid SettingCoreParkingMaxCores = new("ea062031-0e34-4ff1-9b6d-eb1059334028");
    public static readonly Guid SettingBoostMode = new("be337238-0d82-4146-a960-4f3749d470c7");
    public static readonly Guid SettingPerfIncreaseThreshold = new("06cadf0e-64ed-448a-8927-ce7bf90eb35d");
    public static readonly Guid SettingIdleDemoteThreshold = new("4b92d758-5a24-4851-a470-815d78aee119");
    public static readonly Guid SettingMinProcessorState = new("893dee8e-2bef-41e0-89c6-b55d0929964c");
    public static readonly Guid SettingMaxProcessorState = new("bc5038f7-23e0-4960-96da-33abaf5935ec");

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

    // ---- Scheme authoring surface (U1) ----

    // The destination GUID is allocated by the OS (LocalAlloc) and returned via
    // an out-pointer; the caller must LocalFree it (mirror GetActiveScheme).
    [DllImport("powrprof.dll")]
    public static extern uint PowerDuplicateScheme(
        IntPtr RootPowerKey,
        ref Guid SourceSchemeGuid,
        ref IntPtr DestinationSchemeGuid);

    [DllImport("powrprof.dll")]
    public static extern uint PowerWriteACValueIndex(
        IntPtr RootPowerKey,
        ref Guid SchemeGuid,
        ref Guid SubGroupOfPowerSettingsGuid,
        ref Guid PowerSettingGuid,
        uint AcValueIndex);

    [DllImport("powrprof.dll")]
    public static extern uint PowerWriteDCValueIndex(
        IntPtr RootPowerKey,
        ref Guid SchemeGuid,
        ref Guid SubGroupOfPowerSettingsGuid,
        ref Guid PowerSettingGuid,
        uint DcValueIndex);

    [DllImport("powrprof.dll")]
    public static extern uint PowerReadACValueIndex(
        IntPtr RootPowerKey,
        ref Guid SchemeGuid,
        ref Guid SubGroupOfPowerSettingsGuid,
        ref Guid PowerSettingGuid,
        out uint AcValueIndex);

    [DllImport("powrprof.dll")]
    public static extern uint PowerReadDCValueIndex(
        IntPtr RootPowerKey,
        ref Guid SchemeGuid,
        ref Guid SubGroupOfPowerSettingsGuid,
        ref Guid PowerSettingGuid,
        out uint DcValueIndex);

    // Buffer is a Unicode, null-terminated byte buffer plus its byte count — NOT
    // a marshalled string. Sub/Setting are NULL (IntPtr.Zero) to name a scheme.
    [DllImport("powrprof.dll")]
    public static extern uint PowerWriteFriendlyName(
        IntPtr RootPowerKey,
        ref Guid SchemeGuid,
        IntPtr SubGroupOfPowerSettingsGuid,
        IntPtr PowerSettingGuid,
        byte[] Buffer,
        uint BufferSize);

    [DllImport("powrprof.dll")]
    public static extern uint PowerDeleteScheme(IntPtr RootPowerKey, ref Guid SchemeGuid);

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

    /// <summary>Duplicate a source scheme; returns the new scheme GUID, or
    /// <see cref="Guid.Empty"/> on failure.</summary>
    public static Guid DuplicateScheme(Guid source)
    {
        IntPtr dest = IntPtr.Zero;
        if (PowerDuplicateScheme(IntPtr.Zero, ref source, ref dest) != ERROR_SUCCESS || dest == IntPtr.Zero)
            return Guid.Empty;
        try { return Marshal.PtrToStructure<Guid>(dest); }
        finally { LocalFree(dest); }
    }

    /// <summary>Write a processor power-setting value to both the AC and DC rails
    /// of a scheme. Returns true only if both rails wrote successfully.</summary>
    public static bool WriteValue(Guid scheme, Guid subgroup, Guid setting, uint value)
    {
        var ac = PowerWriteACValueIndex(IntPtr.Zero, ref scheme, ref subgroup, ref setting, value);
        var dc = PowerWriteDCValueIndex(IntPtr.Zero, ref scheme, ref subgroup, ref setting, value);
        return ac == ERROR_SUCCESS && dc == ERROR_SUCCESS;
    }

    /// <summary>Read the AC value of a processor power setting. Returns null if
    /// the read fails.</summary>
    public static uint? ReadAcValue(Guid scheme, Guid subgroup, Guid setting)
    {
        if (PowerReadACValueIndex(IntPtr.Zero, ref scheme, ref subgroup, ref setting, out var value) != ERROR_SUCCESS)
            return null;
        return value;
    }

    /// <summary>Read the DC value of a processor power setting. Returns null if
    /// the read fails.</summary>
    public static uint? ReadDcValue(Guid scheme, Guid subgroup, Guid setting)
    {
        if (PowerReadDCValueIndex(IntPtr.Zero, ref scheme, ref subgroup, ref setting, out var value) != ERROR_SUCCESS)
            return null;
        return value;
    }

    public static bool WriteFriendlyName(Guid scheme, string name)
    {
        // Unicode bytes including the null terminator.
        var bytes = Encoding.Unicode.GetBytes(name + "\0");
        return PowerWriteFriendlyName(IntPtr.Zero, ref scheme, IntPtr.Zero, IntPtr.Zero, bytes, (uint)bytes.Length)
            == ERROR_SUCCESS;
    }

    public static bool DeleteScheme(Guid scheme) =>
        PowerDeleteScheme(IntPtr.Zero, ref scheme) == ERROR_SUCCESS;
}
