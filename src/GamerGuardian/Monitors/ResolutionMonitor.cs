using System.Runtime.InteropServices;
using GamerGuardian.Models;
using GamerGuardian.Native;
using GamerGuardian.Services;

namespace GamerGuardian.Monitors;

public sealed class ResolutionMonitor : IMonitoredSetting
{
    public string Id => "resolution";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var active = DisplayHelper.EnumerateActiveDisplays();
        foreach (var display in active)
        {
            var pref = DisplayPreferenceResolver.Resolve(config, display, active);
            if (pref.Resolution.DesiredWidth is null || pref.Resolution.DesiredHeight is null) continue;
            if (string.IsNullOrEmpty(display.GdiDeviceName)) continue;

            var current = GetCurrent(display.GdiDeviceName);
            if (current is null) continue;

            uint dw = pref.Resolution.DesiredWidth.Value;
            uint dh = pref.Resolution.DesiredHeight.Value;
            if (current.Value.Width == dw && current.Value.Height == dh) continue;

            var captured = display;
            yield return new DriftItem(
                SettingId: $"{Id}:{display.StableKey}",
                DisplayKey: display.StableKey,
                DisplayLabel: display.DisplayLabel,
                Description: $"Resolution on {display.DisplayLabel}",
                CurrentValue: $"{current.Value.Width}x{current.Value.Height}",
                DesiredValue: $"{dw}x{dh}",
                AutoApply: pref.Resolution.AutoApply,
                Apply: () => Task.Run(() => Apply(captured.GdiDeviceName, dw, dh)),
                IsMonitored: pref.Resolution.Monitor,
                RawBefore: $"dmPelsWidth={current.Value.Width}, dmPelsHeight={current.Value.Height}",
                RawDesired: $"dmPelsWidth={dw}, dmPelsHeight={dh}");
        }
    }

    public static (uint Width, uint Height)? GetCurrent(string gdiDeviceName)
    {
        var dm = new User32.DEVMODE { dmSize = (ushort)Marshal.SizeOf<User32.DEVMODE>() };
        if (!User32.EnumDisplaySettingsEx(gdiDeviceName, User32.ENUM_CURRENT_SETTINGS, ref dm, 0)) return null;
        return (dm.dmPelsWidth, dm.dmPelsHeight);
    }

    public static IReadOnlyList<(uint Width, uint Height)> ListSupported(string gdiDeviceName)
    {
        var set = new SortedSet<(uint, uint)>(Comparer<(uint w, uint h)>.Create((a, b) =>
            a.w != b.w ? a.w.CompareTo(b.w) : a.h.CompareTo(b.h)));
        var dm = new User32.DEVMODE { dmSize = (ushort)Marshal.SizeOf<User32.DEVMODE>() };
        for (int i = 0; User32.EnumDisplaySettingsEx(gdiDeviceName, i, ref dm, 0); i++)
        {
            if (dm.dmPelsWidth >= 800 && dm.dmPelsHeight >= 600)
                set.Add((dm.dmPelsWidth, dm.dmPelsHeight));
        }
        return set.ToList();
    }

    public static int Apply(string gdiDeviceName, uint width, uint height)
    {
        var dm = new User32.DEVMODE { dmSize = (ushort)Marshal.SizeOf<User32.DEVMODE>() };
        if (!User32.EnumDisplaySettingsEx(gdiDeviceName, User32.ENUM_CURRENT_SETTINGS, ref dm, 0))
            return User32.DISP_CHANGE_FAILED;
        dm.dmPelsWidth = width;
        dm.dmPelsHeight = height;
        dm.dmFields = User32.DM_PELSWIDTH | User32.DM_PELSHEIGHT | User32.DM_DISPLAYFREQUENCY;
        return User32.ChangeDisplaySettingsEx(gdiDeviceName, ref dm, IntPtr.Zero, User32.CDS_UPDATEREGISTRY, IntPtr.Zero);
    }
}
