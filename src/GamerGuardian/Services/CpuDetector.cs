using System.Text.RegularExpressions;
using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Services;

/// <summary>
/// Detects the installed CPU once at startup via a lightweight registry read
/// (no WMI, no CPUID, no new dependency) and caches it for the process lifetime.
/// The classification logic is a pure static <see cref="Parse"/> so it is
/// unit-testable without touching the registry.
/// </summary>
public static class CpuDetector
{
    private const string CpuKeyPath = @"HARDWARE\DESCRIPTION\System\CentralProcessor\0";

    private static readonly Lazy<CpuInfo> _cached = new(ReadAndParse);

    /// <summary>The detected CPU, read once and cached (no timer/thread).</summary>
    public static CpuInfo Current => _cached.Value;

    private static CpuInfo ReadAndParse()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(CpuKeyPath, writable: false);
            var name = key?.GetValue("ProcessorNameString") as string ?? string.Empty;
            var vendor = key?.GetValue("VendorIdentifier") as string ?? string.Empty;
            var ident = key?.GetValue("Identifier") as string ?? string.Empty;
            return Parse(name, vendor, ident);
        }
        catch
        {
            return CpuInfo.Unknown();
        }
    }

    // AMD desktop model token: 4 digits + an optional suffix. X3D2 must precede
    // X3D in the alternation so "9950X3D2" is not truncated to "9950X3D".
    private static readonly Regex AmdModel =
        new(@"\b(\d{4}(?:X3D2|X3D|XT|X|GE|G|F)?)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Intel hybrid 12th–14th gen mainstream model (e.g. i7-14700K -> 14700K).
    private static readonly Regex IntelHybridModel =
        new(@"\bi[3579]-(1[2-4]\d{3}[A-Z]{0,2})\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Intel Core Ultra (e.g. "Core Ultra 9 285K") -> 285K.
    private static readonly Regex IntelUltraModel =
        new(@"\bUltra\s+\d\s+(\d{3}[A-Z]{0,2})\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static CpuInfo Parse(string? nameString, string? vendorId, string? identifier)
    {
        nameString ??= string.Empty;
        vendorId ??= string.Empty;

        var vendor = ClassifyVendor(vendorId, nameString);
        var (model, family) = vendor switch
        {
            CpuVendor.Amd => ParseAmd(nameString),
            CpuVendor.Intel => ParseIntel(nameString),
            _ => (string.Empty, string.Empty),
        };

        return new CpuInfo(vendor, nameString.Trim(), model, family);
    }

    private static CpuVendor ClassifyVendor(string vendorId, string nameString)
    {
        if (vendorId.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
            nameString.Contains("AMD", StringComparison.OrdinalIgnoreCase) ||
            nameString.Contains("Ryzen", StringComparison.OrdinalIgnoreCase))
            return CpuVendor.Amd;

        if (vendorId.Contains("Intel", StringComparison.OrdinalIgnoreCase) ||
            nameString.Contains("Intel", StringComparison.OrdinalIgnoreCase))
            return CpuVendor.Intel;

        return CpuVendor.Unknown;
    }

    private static (string model, string family) ParseAmd(string nameString)
    {
        var m = AmdModel.Match(nameString);
        if (!m.Success) return (string.Empty, string.Empty);

        var model = m.Groups[1].Value.ToUpperInvariant();
        // Coarse Zen generation from the leading digit (desktop Ryzen).
        var family = model[0] switch
        {
            '9' => "Zen5",
            '8' => "Zen4",
            '7' => "Zen4",
            '5' => "Zen3",
            _ => string.Empty,
        };
        return (model, family);
    }

    private static (string model, string family) ParseIntel(string nameString)
    {
        var ultra = IntelUltraModel.Match(nameString);
        if (ultra.Success)
            return (ultra.Groups[1].Value.ToUpperInvariant(), "IntelHybrid");

        var hybrid = IntelHybridModel.Match(nameString);
        if (hybrid.Success)
            return (hybrid.Groups[1].Value.ToUpperInvariant(), "IntelHybrid");

        // Recognized Intel but not a known hybrid generation.
        var any = Regex.Match(nameString, @"\b(\d{4,5}[A-Z]{0,2})\b");
        return (any.Success ? any.Groups[1].Value.ToUpperInvariant() : string.Empty, "IntelOther");
    }
}
