using System.Runtime.InteropServices;
using GamerGuardian.Models;
using GamerGuardian.Native;
using GamerGuardian.Services;

namespace GamerGuardian.Monitors;

public sealed class HdrMonitor : IMonitoredSetting
{
    public string Id => "hdr";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var active = DisplayHelper.EnumerateActiveDisplays();
        foreach (var display in active)
        {
            var pref = DisplayPreferenceResolver.Resolve(config, display, active);

            var state = ReadHdrState(display);
            if (state is null) continue;
            if (!state.Value.Supported) continue;

            bool current = state.Value.Enabled;
            bool desired = pref.Hdr.DesiredOn;
            if (current == desired) continue;

            yield return new DriftItem(
                SettingId: $"{Id}:{display.StableKey}",
                DisplayKey: display.StableKey,
                DisplayLabel: display.DisplayLabel,
                Description: $"HDR on {display.DisplayLabel}",
                CurrentValue: current ? "On" : "Off",
                DesiredValue: desired ? "On" : "Off",
                AutoApply: pref.Hdr.AutoApply,
                Apply: () => Task.Run(() => SetHdrState(display, desired)),
                IsMonitored: pref.Hdr.Monitor,
                RawBefore: $"advancedColorEnabled={(current ? 1 : 0)}",
                RawDesired: $"advancedColorEnabled={(desired ? 1 : 0)}");
        }
    }

    public static (bool Supported, bool Enabled)? ReadHdrState(DisplayInfo display)
    {
        var info = new DisplayConfig.DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO
        {
            header = new DisplayConfig.DISPLAYCONFIG_DEVICE_INFO_HEADER
            {
                type = DisplayConfig.DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO,
                size = (uint)Marshal.SizeOf<DisplayConfig.DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO>(),
                adapterId = display.AdapterId,
                id = display.TargetId,
            },
        };
        if (DisplayConfig.DisplayConfigGetDeviceInfo(ref info) != DisplayConfig.ERROR_SUCCESS) return null;
        return (info.AdvancedColorSupported, info.AdvancedColorEnabled);
    }

    public static bool SetHdrState(DisplayInfo display, bool enable)
    {
        var info = new DisplayConfig.DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE
        {
            header = new DisplayConfig.DISPLAYCONFIG_DEVICE_INFO_HEADER
            {
                type = DisplayConfig.DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE,
                size = (uint)Marshal.SizeOf<DisplayConfig.DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE>(),
                adapterId = display.AdapterId,
                id = display.TargetId,
            },
            flags = enable ? 1u : 0u,
        };
        return DisplayConfig.DisplayConfigSetDeviceInfo(ref info) == DisplayConfig.ERROR_SUCCESS;
    }
}
