namespace GamerGuardian.Native;

/// <summary>One populated memory slot, as reported by SMBIOS.</summary>
/// <param name="SizeBytes">Module capacity.</param>
/// <param name="SpeedMts">Configured speed in MT/s, or 0 when the firmware does not report one.</param>
/// <param name="TypeName">"DDR5", "DDR4", … or null when the type code is unrecognised.</param>
public sealed record MemoryModule(ulong SizeBytes, int SpeedMts, string? TypeName);

/// <summary>
/// Minimal SMBIOS reader: just enough to describe installed memory.
///
/// <para>The raw table comes from <c>GetSystemFirmwareTable('RSMB')</c>, which is a
/// plain P/Invoke — no WMI, no new package — so it fits the app's "user-mode
/// P/Invoke + registry" shape. The parse is a pure function over a byte buffer and
/// is unit-tested directly against synthetic tables.</para>
///
/// <para>Reference: DMTF DSP0134, structure Type 17 (Memory Device).</para>
/// </summary>
public static class Smbios
{
    // RawSMBIOSData header the provider prepends: Used20CallingMethod,
    // SMBIOSMajorVersion, SMBIOSMinorVersion, DmiRevision, then a DWORD Length.
    private const int RawHeaderLength = 8;

    private const byte MemoryDeviceType = 17;

    // Field offsets inside a Type 17 structure.
    private const int OffsetSize = 0x0C;             // WORD
    private const int OffsetMemoryType = 0x12;       // BYTE
    private const int OffsetSpeed = 0x15;            // WORD, MT/s      (SMBIOS 2.3+)
    private const int OffsetExtendedSize = 0x1C;     // DWORD, MB       (SMBIOS 2.7+)
    private const int OffsetConfiguredSpeed = 0x20;  // WORD, MT/s      (SMBIOS 2.7+)

    /// <summary>
    /// Every populated memory slot in the table, in firmware order. Empty slots
    /// (Size == 0) and unreadable entries are skipped. Returns an empty list rather
    /// than throwing on any malformed input — this feeds an informational card.
    /// </summary>
    public static IReadOnlyList<MemoryModule> ParseMemoryDevices(byte[]? rawSmbios)
    {
        var modules = new List<MemoryModule>();
        if (rawSmbios is null || rawSmbios.Length <= RawHeaderLength) return modules;

        try
        {
            uint declared = BitConverter.ToUInt32(rawSmbios, 4);
            int end = RawHeaderLength + (int)Math.Min(declared, (uint)(rawSmbios.Length - RawHeaderLength));

            int i = RawHeaderLength;
            while (i + 4 <= end)
            {
                byte type = rawSmbios[i];
                byte formattedLength = rawSmbios[i + 1];

                // A structure is at least its 4-byte header; anything shorter means
                // the table is corrupt and walking further would be guesswork.
                if (formattedLength < 4 || i + formattedLength > end) break;

                if (type == MemoryDeviceType)
                {
                    var module = ReadMemoryDevice(rawSmbios, i, formattedLength);
                    if (module is not null) modules.Add(module);
                }

                // Type 127 (End-of-Table) terminates the table.
                if (type == 127) break;

                i = SkipStrings(rawSmbios, i + formattedLength, end);
                if (i < 0) break;
            }
        }
        catch
        {
            return modules;
        }

        return modules;
    }

    /// <summary>
    /// Advances past a structure's unformatted string set, which ends at a double
    /// NUL. A structure with no strings is a single pair of NULs. Returns -1 when
    /// the terminator is missing.
    /// </summary>
    private static int SkipStrings(byte[] data, int start, int end)
    {
        int i = start;
        while (i + 1 < end)
        {
            if (data[i] == 0 && data[i + 1] == 0) return i + 2;
            i++;
        }
        return -1;
    }

    private static MemoryModule? ReadMemoryDevice(byte[] data, int start, int length)
    {
        if (length < OffsetSize + 2) return null;

        ushort rawSize = BitConverter.ToUInt16(data, start + OffsetSize);
        if (rawSize == 0) return null;       // slot present but empty
        if (rawSize == 0xFFFF) return null;  // size unknown — nothing worth showing

        ulong sizeBytes;
        if (rawSize == 0x7FFF)
        {
            // 0x7FFF means "too large for the WORD, read Extended Size (in MB)".
            if (length < OffsetExtendedSize + 4) return null;
            uint extendedMb = BitConverter.ToUInt32(data, start + OffsetExtendedSize) & 0x7FFFFFFF;
            if (extendedMb == 0) return null;
            sizeBytes = (ulong)extendedMb * 1024UL * 1024UL;
        }
        else
        {
            // Bit 15 selects the unit: set means KB, clear means MB.
            ulong unit = (rawSize & 0x8000) != 0 ? 1024UL : 1024UL * 1024UL;
            sizeBytes = (ulong)(rawSize & 0x7FFF) * unit;
        }

        // Configured speed is what the modules actually run at (the XMP/EXPO figure);
        // Speed is the module's rated maximum. Prefer the former, fall back to the
        // latter, and treat 0 / 0xFFFF as "not reported".
        int speed = 0;
        if (length >= OffsetConfiguredSpeed + 2)
            speed = Normalise(BitConverter.ToUInt16(data, start + OffsetConfiguredSpeed));
        if (speed == 0 && length >= OffsetSpeed + 2)
            speed = Normalise(BitConverter.ToUInt16(data, start + OffsetSpeed));

        string? typeName = length >= OffsetMemoryType + 1
            ? MemoryTypeName(data[start + OffsetMemoryType])
            : null;

        return new MemoryModule(sizeBytes, speed, typeName);

        static int Normalise(ushort v) => v == 0xFFFF ? 0 : v;
    }

    /// <summary>Type codes from DSP0134 Table 76. Only the ones a machine running
    /// this app could plausibly report are named; anything else stays null.</summary>
    private static string? MemoryTypeName(byte code) => code switch
    {
        0x13 => "DDR",
        0x14 => "DDR2",
        0x18 => "DDR3",
        0x1A => "DDR4",
        0x1B => "LPDDR",
        0x1C => "LPDDR2",
        0x1D => "LPDDR3",
        0x1E => "LPDDR4",
        0x22 => "DDR5",
        0x23 => "LPDDR5",
        _ => null,
    };
}
