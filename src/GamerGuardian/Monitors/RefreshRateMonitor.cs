using GamerGuardian.Models;
using GamerGuardian.Native;

namespace GamerGuardian.Monitors;

public sealed class RefreshRateMonitor : IMonitoredSetting
{
    public string Id => "refresh";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        foreach (var display in DisplayHelper.EnumerateActiveDisplays())
        {
            if (!config.Displays.TryGetValue(display.StableKey, out var pref))
            {
                pref = new DisplayPreference { DisplayLabel = display.DisplayLabel };
                config.Displays[display.StableKey] = pref;
            }
            if (!pref.RefreshRate.Monitor) continue;
            if (string.IsNullOrEmpty(display.GdiDeviceName)) continue;

            var current = GetCurrentRefresh(display.GdiDeviceName);
            if (current is null) continue;

            uint desired = pref.RefreshRate.Target == RefreshRateTarget.Fixed && pref.RefreshRate.FixedHz.HasValue
                ? pref.RefreshRate.FixedHz.Value
                : GetMaxSupportedRefresh(display.GdiDeviceName, current.Value.Width, current.Value.Height);

            if (desired == 0) continue;
            if (current.Value.Hz == desired) continue;

            var captured = display;
            uint capturedDesired = desired;
            yield return new DriftItem(
                SettingId: $"{Id}:{display.StableKey}",
                DisplayKey: display.StableKey,
                DisplayLabel: display.DisplayLabel,
                Description: $"Refresh rate on {display.DisplayLabel}",
                CurrentValue: $"{current.Value.Hz} Hz",
                DesiredValue: $"{desired} Hz",
                AutoApply: pref.RefreshRate.AutoApply,
                Apply: () => Task.Run(() => SetRefresh(captured.GdiDeviceName, capturedDesired)));
        }
    }

    public static (uint Hz, uint Width, uint Height)? GetCurrentRefresh(string gdiDeviceName)
    {
        var dm = new User32.DEVMODE { dmSize = (ushort)System.Runtime.InteropServices.Marshal.SizeOf<User32.DEVMODE>() };
        if (!User32.EnumDisplaySettingsEx(gdiDeviceName, User32.ENUM_CURRENT_SETTINGS, ref dm, 0)) return null;
        return (dm.dmDisplayFrequency, dm.dmPelsWidth, dm.dmPelsHeight);
    }

    public static uint GetMaxSupportedRefresh(string gdiDeviceName, uint width, uint height)
    {
        uint max = 0;
        var dm = new User32.DEVMODE { dmSize = (ushort)System.Runtime.InteropServices.Marshal.SizeOf<User32.DEVMODE>() };
        for (int mode = 0; User32.EnumDisplaySettingsEx(gdiDeviceName, mode, ref dm, 0); mode++)
        {
            if (dm.dmPelsWidth == width && dm.dmPelsHeight == height && dm.dmDisplayFrequency > max)
                max = dm.dmDisplayFrequency;
        }
        return max;
    }

    public static IReadOnlyList<uint> GetSupportedRefreshRates(string gdiDeviceName, uint width, uint height)
    {
        var set = new SortedSet<uint>();
        var dm = new User32.DEVMODE { dmSize = (ushort)System.Runtime.InteropServices.Marshal.SizeOf<User32.DEVMODE>() };
        for (int mode = 0; User32.EnumDisplaySettingsEx(gdiDeviceName, mode, ref dm, 0); mode++)
        {
            if (dm.dmPelsWidth == width && dm.dmPelsHeight == height && dm.dmDisplayFrequency > 1)
                set.Add(dm.dmDisplayFrequency);
        }
        return set.ToList();
    }

    public static int SetRefresh(string gdiDeviceName, uint hz)
    {
        var dm = new User32.DEVMODE { dmSize = (ushort)System.Runtime.InteropServices.Marshal.SizeOf<User32.DEVMODE>() };
        if (!User32.EnumDisplaySettingsEx(gdiDeviceName, User32.ENUM_CURRENT_SETTINGS, ref dm, 0))
            return User32.DISP_CHANGE_FAILED;
        dm.dmDisplayFrequency = hz;
        dm.dmFields = User32.DM_DISPLAYFREQUENCY | User32.DM_PELSWIDTH | User32.DM_PELSHEIGHT;
        return User32.ChangeDisplaySettingsEx(gdiDeviceName, ref dm, IntPtr.Zero, User32.CDS_UPDATEREGISTRY, IntPtr.Zero);
    }
}
