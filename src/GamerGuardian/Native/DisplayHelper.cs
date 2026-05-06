using System.Runtime.InteropServices;

namespace GamerGuardian.Native;

public sealed record DisplayInfo(
    string GdiDeviceName,
    string FriendlyName,
    string DevicePath,
    DisplayConfig.LUID AdapterId,
    uint TargetId,
    uint SourceId)
{
    public string StableKey => string.IsNullOrEmpty(DevicePath) ? $"{FriendlyName}|{GdiDeviceName}" : DevicePath;
    public string DisplayLabel => string.IsNullOrEmpty(FriendlyName) ? GdiDeviceName : FriendlyName;
}

public static class DisplayHelper
{
    public static List<DisplayInfo> EnumerateActiveDisplays()
    {
        var result = new List<DisplayInfo>();

        int rc = DisplayConfig.GetDisplayConfigBufferSizes(
            DisplayConfig.QDC_ONLY_ACTIVE_PATHS,
            out uint pathCount,
            out uint modeCount);
        if (rc != DisplayConfig.ERROR_SUCCESS) return result;

        var paths = new DisplayConfig.DISPLAYCONFIG_PATH_INFO[pathCount];
        var modes = new DisplayConfig.DISPLAYCONFIG_MODE_INFO[modeCount];

        rc = DisplayConfig.QueryDisplayConfig(
            DisplayConfig.QDC_ONLY_ACTIVE_PATHS,
            ref pathCount,
            paths,
            ref modeCount,
            modes,
            IntPtr.Zero);
        if (rc != DisplayConfig.ERROR_SUCCESS) return result;

        for (int i = 0; i < pathCount; i++)
        {
            var path = paths[i];

            var sourceName = new DisplayConfig.DISPLAYCONFIG_SOURCE_DEVICE_NAME
            {
                header = new DisplayConfig.DISPLAYCONFIG_DEVICE_INFO_HEADER
                {
                    type = DisplayConfig.DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
                    size = (uint)Marshal.SizeOf<DisplayConfig.DISPLAYCONFIG_SOURCE_DEVICE_NAME>(),
                    adapterId = path.sourceInfo.adapterId,
                    id = path.sourceInfo.id,
                },
                viewGdiDeviceName = string.Empty,
            };
            if (DisplayConfig.DisplayConfigGetDeviceInfo(ref sourceName) != DisplayConfig.ERROR_SUCCESS)
                continue;

            var targetName = new DisplayConfig.DISPLAYCONFIG_TARGET_DEVICE_NAME
            {
                header = new DisplayConfig.DISPLAYCONFIG_DEVICE_INFO_HEADER
                {
                    type = DisplayConfig.DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
                    size = (uint)Marshal.SizeOf<DisplayConfig.DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
                    adapterId = path.targetInfo.adapterId,
                    id = path.targetInfo.id,
                },
                monitorFriendlyDeviceName = string.Empty,
                monitorDevicePath = string.Empty,
            };
            DisplayConfig.DisplayConfigGetDeviceInfo(ref targetName);

            result.Add(new DisplayInfo(
                GdiDeviceName: sourceName.viewGdiDeviceName ?? string.Empty,
                FriendlyName: targetName.monitorFriendlyDeviceName ?? string.Empty,
                DevicePath: targetName.monitorDevicePath ?? string.Empty,
                AdapterId: path.targetInfo.adapterId,
                TargetId: path.targetInfo.id,
                SourceId: path.sourceInfo.id));
        }

        return result;
    }
}
