using GamerGuardian.Models;
using GamerGuardian.Monitors;
using GamerGuardian.Native;
using Microsoft.Win32;

namespace GamerGuardian.Services;

/// <summary>One row of the Status page's "This PC" grid.</summary>
public sealed record SystemInfoCard(string Title, string Subtitle, IReadOnlyList<(string Label, string Value)> Rows);

/// <summary>
/// Read-only "what machine is this" summary for the Status page.
///
/// <para>Scoped to things that give context to the settings GamerGuardian actually
/// manages — the CPU behind the power-plan recipe, the GPU behind HAGS/VRR, the
/// Windows build that gates several policies, the displays whose HDR and refresh it
/// controls, and the active power plan. Storage is deliberately absent: the app
/// manages nothing about disks, so a disk card would be decoration.</para>
///
/// <para>Every read is best-effort and returns "Unknown" rather than throwing —
/// this is an informational surface and must never be able to break the window.</para>
/// </summary>
public static class SystemInfo
{
    private const string Unknown = "Unknown";

    /// <summary>Human-readable byte size. Pure, so it is unit-tested directly.</summary>
    public static string FormatBytes(ulong? bytes)
    {
        if (bytes is null || bytes == 0) return Unknown;
        double gb = bytes.Value / 1024d / 1024d / 1024d;
        if (gb >= 1024) return $"{gb / 1024d:0.##} TB";
        // Installed RAM reports slightly under the marketed figure (firmware
        // reserves some), so round to a sensible precision rather than pretending
        // to exactness: 31.93 GB reads better as "31.9 GB".
        return gb >= 100 ? $"{gb:0} GB" : $"{gb:0.#} GB";
    }

    /// <summary>
    /// Corrects the Windows edition string. <c>ProductName</c> under
    /// <c>CurrentVersion</c> still reads "Windows 10 ..." on Windows 11 — Microsoft
    /// never updated the value — so an unfiltered read shows "Windows 10 Pro" on a
    /// Windows 11 machine. Build 22000 is the 10-to-11 boundary. Pure, so it is
    /// unit-tested directly.
    /// </summary>
    public static string CorrectEdition(string? productName, string? currentBuild)
    {
        if (string.IsNullOrWhiteSpace(productName)) return Unknown;
        var name = productName.Trim();
        if (int.TryParse(currentBuild, out var build) && build >= 22000 &&
            name.StartsWith("Windows 10", StringComparison.OrdinalIgnoreCase))
        {
            return string.Concat("Windows 11", name.AsSpan("Windows 10".Length));
        }
        return name;
    }

    /// <summary>Collapses the GPU's registry description to something short enough
    /// for a card. Pure, so it is unit-tested directly.</summary>
    public static string ShortenGpuName(string? driverDesc)
    {
        if (string.IsNullOrWhiteSpace(driverDesc)) return Unknown;
        var s = driverDesc.Trim();
        // Registry descriptions carry vendor noise the card has no room for.
        foreach (var noise in new[] { "(R)", "(TM)", "®", "™" })
            s = s.Replace(noise, string.Empty, StringComparison.Ordinal);
        return string.Join(' ', s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    // ---- Cards ------------------------------------------------------------

    public static SystemInfoCard Cpu()
    {
        try
        {
            var cpu = CpuDetector.Current;
            var recipe = CpuTuneCatalog.Resolve(cpu);
            var name = cpu.IsDetected && !string.IsNullOrWhiteSpace(cpu.RawModel)
                ? cpu.RawModel.Trim()
                : Unknown;
            var tier = recipe.Definition.Tier switch
            {
                TuneTier.Exact => "exact match",
                TuneTier.Family => "family match",
                _ => "generic",
            };
            return new SystemInfoCard("Processor", "Drives the power-plan recipe", new[]
            {
                ("Model", name),
                ("Tuning recipe", tier),
            });
        }
        catch { return new SystemInfoCard("Processor", "Drives the power-plan recipe", new[] { ("Model", Unknown) }); }
    }

    public static SystemInfoCard Gpu()
    {
        var (name, vram) = ReadPrimaryGpu();
        return new SystemInfoCard("Graphics", "Behind HAGS and VRR", new[]
        {
            ("Adapter", name),
            ("Video memory", vram),
        });
    }

    public static SystemInfoCard Memory()
    {
        return new SystemInfoCard("Memory", "Installed physical RAM", new[]
        {
            ("Total", FormatBytes(SystemMetrics.TotalPhysicalBytes())),
        });
    }

    public static SystemInfoCard Windows()
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", writable: false);
            var product = k?.GetValue("ProductName") as string;
            var display = k?.GetValue("DisplayVersion") as string;   // e.g. 24H2
            var build = k?.GetValue("CurrentBuild") as string;
            var ubr = k?.GetValue("UBR");

            var version = string.IsNullOrWhiteSpace(display) ? Unknown : display!;
            var buildText = string.IsNullOrWhiteSpace(build)
                ? Unknown
                : (ubr is int u ? $"{build}.{u}" : build!);

            return new SystemInfoCard("Windows", "Gates which settings apply", new[]
            {
                ("Edition", CorrectEdition(product, build)),
                ("Version", $"{version}  (build {buildText})"),
            });
        }
        catch { return new SystemInfoCard("Windows", "Gates which settings apply", new[] { ("Edition", Unknown) }); }
    }

    public static SystemInfoCard Displays()
    {
        try
        {
            var list = DisplayHelper.EnumerateActiveDisplays();
            if (list.Count == 0)
                return new SystemInfoCard("Displays", "HDR, refresh and resolution", new[] { ("Detected", Unknown) });

            var first = list[0];
            var res = string.IsNullOrEmpty(first.GdiDeviceName)
                ? null
                : ResolutionMonitor.GetCurrent(first.GdiDeviceName);
            var hz = string.IsNullOrEmpty(first.GdiDeviceName)
                ? null
                : RefreshRateMonitor.GetCurrentRefresh(first.GdiDeviceName);

            var primary = res is { } r && hz is { } h
                ? $"{first.DisplayLabel} — {r.Width}x{r.Height} @ {h.Hz} Hz"
                : first.DisplayLabel;

            return new SystemInfoCard("Displays", "HDR, refresh and resolution", new[]
            {
                ("Connected", list.Count == 1 ? "1 display" : $"{list.Count} displays"),
                ("Primary", primary),
            });
        }
        catch { return new SystemInfoCard("Displays", "HDR, refresh and resolution", new[] { ("Detected", Unknown) }); }
    }

    public static SystemInfoCard PowerPlan()
    {
        try
        {
            var active = PowerPlanMonitor.GetActivePlan();
            var plans = PowerPlanMonitor.ListAvailablePlans();
            var name = active != Guid.Empty && plans.TryGetValue(active, out var n) ? n : Unknown;
            var recommended = CpuTuneCatalog.Resolve(CpuDetector.Current).RecommendedPrebuilt.ToString();
            return new SystemInfoCard("Power plan", "Active Windows scheme", new[]
            {
                ("Active", name),
                ("Recommended", recommended),
            });
        }
        catch { return new SystemInfoCard("Power plan", "Active Windows scheme", new[] { ("Active", Unknown) }); }
    }

    /// <summary>All cards, in display order.</summary>
    public static IReadOnlyList<SystemInfoCard> All() => new[]
    {
        Cpu(), Gpu(), Memory(), Windows(), Displays(), PowerPlan(),
    };

    // ---- GPU read ---------------------------------------------------------

    /// <summary>
    /// Primary display adapter from the display-class registry key. Registry rather
    /// than WMI so no new dependency is taken; the enumerated subkeys are the same
    /// ones Device Manager shows. Picks the first adapter that reports a driver
    /// description, skipping the Microsoft Basic Display Adapter when a real one is
    /// present.
    /// </summary>
    private static (string Name, string Vram) ReadPrimaryGpu()
    {
        try
        {
            const string classKey =
                @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";
            using var root = Registry.LocalMachine.OpenSubKey(classKey, writable: false);
            if (root is null) return (Unknown, Unknown);

            (string Name, string Vram)? fallback = null;
            foreach (var sub in root.GetSubKeyNames())
            {
                // Adapter instances are the four-digit numeric subkeys.
                if (sub.Length != 4 || !sub.All(char.IsDigit)) continue;
                using var k = root.OpenSubKey(sub, writable: false);
                if (k?.GetValue("DriverDesc") is not string desc || string.IsNullOrWhiteSpace(desc)) continue;

                var name = ShortenGpuName(desc);
                var vram = k.GetValue("HardwareInformation.qwMemorySize") switch
                {
                    long l when l > 0 => FormatBytes((ulong)l),
                    // Older drivers store a 32-bit MemorySize instead.
                    int i when i > 0 => FormatBytes((ulong)i),
                    _ => Unknown,
                };

                if (name.Contains("Basic Display", StringComparison.OrdinalIgnoreCase))
                {
                    fallback ??= (name, vram);
                    continue;
                }
                return (name, vram);
            }
            return fallback ?? (Unknown, Unknown);
        }
        catch
        {
            return (Unknown, Unknown);
        }
    }
}
