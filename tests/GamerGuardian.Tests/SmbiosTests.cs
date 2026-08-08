using System;
using System.Linq;
using GamerGuardian.Native;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

/// <summary>
/// Covers the SMBIOS memory parse behind the Status page's Memory card. The parser
/// is a pure function over a byte buffer, so it is driven here with synthetic tables
/// shaped exactly like the ones <c>GetSystemFirmwareTable('RSMB')</c> returns.
/// </summary>
public class SmbiosTests
{
    private const int FullLength = 0x22;   // Type 17 formatted area, SMBIOS 2.7+
    private const byte Ddr5 = 0x22;
    private const byte Ddr4 = 0x1A;

    /// <summary>Wraps structures in the RawSMBIOSData header the provider prepends.</summary>
    private static byte[] Table(params byte[][] structures)
    {
        var body = structures.SelectMany(s => s).ToArray();
        var buffer = new byte[8 + body.Length];
        buffer[0] = 0;   // Used20CallingMethod
        buffer[1] = 3;   // major
        buffer[2] = 4;   // minor
        buffer[3] = 0;   // DmiRevision
        BitConverter.GetBytes((uint)body.Length).CopyTo(buffer, 4);
        body.CopyTo(buffer, 8);
        return buffer;
    }

    private static byte[] MemoryDevice(
        ushort rawSize,
        byte memoryType = Ddr5,
        ushort ratedSpeed = 5600,
        ushort configuredSpeed = 6000,
        uint extendedSizeMb = 0,
        byte length = FullLength)
    {
        var s = new byte[length + 2];   // formatted area + the empty string set's double NUL
        s[0] = 17;
        s[1] = length;
        BitConverter.GetBytes((ushort)0x1000).CopyTo(s, 2);
        if (length >= 0x0E) BitConverter.GetBytes(rawSize).CopyTo(s, 0x0C);
        if (length >= 0x13) s[0x12] = memoryType;
        if (length >= 0x17) BitConverter.GetBytes(ratedSpeed).CopyTo(s, 0x15);
        if (length >= 0x20) BitConverter.GetBytes(extendedSizeMb).CopyTo(s, 0x1C);
        if (length >= 0x22) BitConverter.GetBytes(configuredSpeed).CopyTo(s, 0x20);
        return s;
    }

    private static byte[] EndOfTable() => new byte[] { 127, 4, 0x20, 0x00, 0, 0 };

    /// <summary>A structure carrying a string set, to prove the walker skips it.</summary>
    private static byte[] BiosInformation()
    {
        var formatted = new byte[] { 0, 0x12, 0x01, 0x00 }.Concat(new byte[0x12 - 4]).ToArray();
        var strings = new byte[] { (byte)'A', (byte)'C', (byte)'M', (byte)'E', 0, 0 };
        return formatted.Concat(strings).ToArray();
    }

    // ---- ParseMemoryDevices ------------------------------------------------

    [Fact]
    public void ParseMemoryDevices_NullOrTruncated_ReturnsEmpty()
    {
        Assert.Empty(Smbios.ParseMemoryDevices(null));
        Assert.Empty(Smbios.ParseMemoryDevices(Array.Empty<byte>()));
        Assert.Empty(Smbios.ParseMemoryDevices(new byte[8]));
    }

    [Fact]
    public void ParseMemoryDevices_ReadsSizeTypeAndConfiguredSpeed()
    {
        // 16384 MB with bit 15 clear = 16 GB.
        var raw = Table(MemoryDevice(16384), MemoryDevice(16384), EndOfTable());

        var modules = Smbios.ParseMemoryDevices(raw);

        Assert.Equal(2, modules.Count);
        Assert.All(modules, m =>
        {
            Assert.Equal(16UL * 1024 * 1024 * 1024, m.SizeBytes);
            Assert.Equal("DDR5", m.TypeName);
            // Configured speed wins over the module's rated 5600.
            Assert.Equal(6000, m.SpeedMts);
        });
    }

    [Fact]
    public void ParseMemoryDevices_FallsBackToRatedSpeedWhenConfiguredIsUnreported()
    {
        var raw = Table(MemoryDevice(8192, configuredSpeed: 0), EndOfTable());
        Assert.Equal(5600, Assert.Single(Smbios.ParseMemoryDevices(raw)).SpeedMts);

        // 0xFFFF is SMBIOS for "unknown" and must not be read as a speed.
        raw = Table(MemoryDevice(8192, configuredSpeed: 0xFFFF, ratedSpeed: 0xFFFF), EndOfTable());
        Assert.Equal(0, Assert.Single(Smbios.ParseMemoryDevices(raw)).SpeedMts);
    }

    [Fact]
    public void ParseMemoryDevices_SkipsEmptyAndUnknownSlots()
    {
        // Size 0 = slot present but empty; 0xFFFF = size unknown.
        var raw = Table(
            MemoryDevice(0),
            MemoryDevice(16384),
            MemoryDevice(0xFFFF),
            MemoryDevice(0),
            EndOfTable());

        Assert.Single(Smbios.ParseMemoryDevices(raw));
    }

    [Fact]
    public void ParseMemoryDevices_HonoursTheKilobyteUnitBit()
    {
        // Bit 15 set means the value is in KB rather than MB.
        var raw = Table(MemoryDevice(unchecked((ushort)(0x8000 | 2048))), EndOfTable());
        Assert.Equal(2048UL * 1024, Assert.Single(Smbios.ParseMemoryDevices(raw)).SizeBytes);
    }

    [Fact]
    public void ParseMemoryDevices_UsesExtendedSizeForLargeModules()
    {
        // 0x7FFF is the sentinel for "read Extended Size instead" — 65536 MB = 64 GB.
        var raw = Table(MemoryDevice(0x7FFF, extendedSizeMb: 65536), EndOfTable());
        Assert.Equal(64UL * 1024 * 1024 * 1024, Assert.Single(Smbios.ParseMemoryDevices(raw)).SizeBytes);
    }

    [Fact]
    public void ParseMemoryDevices_WalksPastStructuresThatCarryStrings()
    {
        var raw = Table(BiosInformation(), MemoryDevice(16384), EndOfTable());
        Assert.Single(Smbios.ParseMemoryDevices(raw));
    }

    [Fact]
    public void ParseMemoryDevices_StopsAtEndOfTable()
    {
        var raw = Table(MemoryDevice(16384), EndOfTable(), MemoryDevice(16384));
        Assert.Single(Smbios.ParseMemoryDevices(raw));
    }

    [Fact]
    public void ParseMemoryDevices_ShortStructureIsIgnoredRatherThanRead()
    {
        // An SMBIOS 2.1 entry stops before the speed fields; the size still parses.
        var raw = Table(MemoryDevice(16384, length: 0x15), EndOfTable());
        var module = Assert.Single(Smbios.ParseMemoryDevices(raw));
        Assert.Equal(16UL * 1024 * 1024 * 1024, module.SizeBytes);
        Assert.Equal(0, module.SpeedMts);
    }

    [Fact]
    public void ParseMemoryDevices_GarbageNeverThrows()
    {
        var rng = new Random(1234);
        for (int i = 0; i < 200; i++)
        {
            var buffer = new byte[rng.Next(0, 256)];
            rng.NextBytes(buffer);
            var ex = Record.Exception(() => Smbios.ParseMemoryDevices(buffer));
            Assert.Null(ex);
        }
    }

    [Fact]
    public void ParseMemoryDevices_DeclaredLengthLongerThanBufferDoesNotOverrun()
    {
        var raw = Table(MemoryDevice(16384), EndOfTable());
        BitConverter.GetBytes(uint.MaxValue).CopyTo(raw, 4);
        var ex = Record.Exception(() => Smbios.ParseMemoryDevices(raw));
        Assert.Null(ex);
    }

    // ---- Presentation ------------------------------------------------------

    [Fact]
    public void DescribeModules_UnknownWhenNothingWasReadable()
    {
        Assert.Equal("Unknown", SystemInfo.DescribeModules(Array.Empty<MemoryModule>()));
    }

    [Fact]
    public void DescribeModules_CollapsesMatchedModulesToACount()
    {
        var m = new[]
        {
            new MemoryModule(16UL * 1024 * 1024 * 1024, 6000, "DDR5"),
            new MemoryModule(16UL * 1024 * 1024 * 1024, 6000, "DDR5"),
        };
        Assert.Equal("2 × 16 GB @ 6000 MT/s", SystemInfo.DescribeModules(m));
    }

    [Fact]
    public void DescribeModules_SingleModuleIsNotWrittenAsOneTimes()
    {
        var m = new[] { new MemoryModule(32UL * 1024 * 1024 * 1024, 5600, "DDR5") };
        Assert.Equal("32 GB @ 5600 MT/s", SystemInfo.DescribeModules(m));
    }

    [Fact]
    public void DescribeModules_ListsMismatchedCapacitiesRatherThanHidingThem()
    {
        var m = new[]
        {
            new MemoryModule(16UL * 1024 * 1024 * 1024, 6000, "DDR5"),
            new MemoryModule(8UL * 1024 * 1024 * 1024, 6000, "DDR5"),
        };
        Assert.Equal("16 GB + 8 GB @ 6000 MT/s", SystemInfo.DescribeModules(m));
    }

    [Fact]
    public void DescribeModules_ReportsTheSlowestSpeedBecauseThatIsWhatTheMachineRunsAt()
    {
        var m = new[]
        {
            new MemoryModule(16UL * 1024 * 1024 * 1024, 6000, "DDR5"),
            new MemoryModule(16UL * 1024 * 1024 * 1024, 4800, "DDR5"),
        };
        Assert.Equal("2 × 16 GB @ 4800 MT/s", SystemInfo.DescribeModules(m));
    }

    [Fact]
    public void DescribeModules_OmitsSpeedWhenTheFirmwareReportsNone()
    {
        var m = new[] { new MemoryModule(16UL * 1024 * 1024 * 1024, 0, "DDR4") };
        Assert.Equal("16 GB", SystemInfo.DescribeModules(m));
    }

    [Fact]
    public void SharedTypeName_OnlyWhenEveryModuleAgrees()
    {
        var ddr5 = new MemoryModule(1, 0, "DDR5");
        var ddr4 = new MemoryModule(1, 0, "DDR4");
        var untyped = new MemoryModule(1, 0, null);

        Assert.Equal("DDR5", SystemInfo.SharedTypeName(new[] { ddr5, ddr5 }));
        Assert.Null(SystemInfo.SharedTypeName(new[] { ddr5, ddr4 }));
        Assert.Null(SystemInfo.SharedTypeName(new[] { untyped }));
        Assert.Null(SystemInfo.SharedTypeName(Array.Empty<MemoryModule>()));
    }

    [Fact]
    public void MemoryCard_AlwaysHasBothRows_EvenWithoutSmbios()
    {
        // The card must keep its shape on a machine whose firmware exposes nothing,
        // otherwise the This PC grid goes ragged.
        var card = SystemInfo.Memory();
        Assert.Equal(new[] { "Installed", "Modules" }, card.Rows.Select(r => r.Label).ToArray());
        Assert.All(card.Rows, r => Assert.False(string.IsNullOrWhiteSpace(r.Value)));
    }
}
