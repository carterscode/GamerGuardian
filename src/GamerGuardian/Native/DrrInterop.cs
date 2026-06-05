using static GamerGuardian.Native.DisplayConfig;

namespace GamerGuardian.Native;

/// <summary>
/// Dynamic Refresh Rate (DRR) read/write via the public CCD API. DRR is the
/// Win11 feature that boosts the refresh rate between a low "virtual" rate and
/// the panel's physical rate based on content. It is set by toggling the
/// <see cref="DisplayConfig.DISPLAYCONFIG_PATH_BOOST_REFRESH_RATE"/> flag on a
/// path and calling <see cref="DisplayConfig.SetDisplayConfig"/> with the
/// virtual-refresh-rate-aware flag -- user-mode, no elevation. Distinct from VRR
/// (the GraphicsDrivers\VRROptimizeEnable registry flag).
/// </summary>
public static class DrrInterop
{
    public readonly record struct ReadResult(bool Found, bool Enabled);

    /// <summary>Pure: a path is running DRR when the boost-refresh-rate flag is set.</summary>
    public static bool IsDrrEnabled(uint pathFlags) =>
        (pathFlags & DISPLAYCONFIG_PATH_BOOST_REFRESH_RATE) != 0;

    private static bool SameAdapter(LUID a, LUID b) => a.LowPart == b.LowPart && a.HighPart == b.HighPart;

    private static bool QueryActive(out DISPLAYCONFIG_PATH_INFO[] paths, out uint pathCount,
                                    out DISPLAYCONFIG_MODE_INFO[] modes, out uint modeCount)
    {
        paths = Array.Empty<DISPLAYCONFIG_PATH_INFO>();
        modes = Array.Empty<DISPLAYCONFIG_MODE_INFO>();
        pathCount = 0; modeCount = 0;

        const uint flags = QDC_ONLY_ACTIVE_PATHS | QDC_VIRTUAL_REFRESH_RATE_AWARE;
        if (GetDisplayConfigBufferSizes(flags, out pathCount, out modeCount) != ERROR_SUCCESS)
            return false;
        paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
        return QueryDisplayConfig(flags, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero) == ERROR_SUCCESS;
    }

    private static int FindPath(DISPLAYCONFIG_PATH_INFO[] paths, uint count, LUID adapterId, uint targetId)
    {
        for (int i = 0; i < count; i++)
            if (SameAdapter(paths[i].targetInfo.adapterId, adapterId) && paths[i].targetInfo.id == targetId)
                return i;
        return -1;
    }

    /// <summary>Reads the current DRR state for a target. Found=false when the
    /// target isn't in the active topology (treat as not-drift).</summary>
    public static ReadResult ReadState(LUID adapterId, uint targetId)
    {
        try
        {
            if (!QueryActive(out var paths, out var pathCount, out _, out _))
                return new ReadResult(false, false);
            int idx = FindPath(paths, pathCount, adapterId, targetId);
            if (idx < 0) return new ReadResult(false, false);
            return new ReadResult(true, IsDrrEnabled(paths[idx].flags));
        }
        catch { return new ReadResult(false, false); }
    }

    /// <summary>True when DRR can be set on this target (the driver/panel accepts
    /// the boost-refresh-rate flag under SDC_VALIDATE).</summary>
    public static bool IsSupported(LUID adapterId, uint targetId)
    {
        try
        {
            if (!QueryActive(out var paths, out var pathCount, out var modes, out var modeCount))
                return false;
            int idx = FindPath(paths, pathCount, adapterId, targetId);
            if (idx < 0) return false;
            // Toggle the flag to the opposite of current and ask the OS to validate.
            bool currentlyOn = IsDrrEnabled(paths[idx].flags);
            paths[idx].flags = currentlyOn
                ? paths[idx].flags & ~DISPLAYCONFIG_PATH_BOOST_REFRESH_RATE
                : paths[idx].flags | DISPLAYCONFIG_PATH_BOOST_REFRESH_RATE;
            uint flags = SDC_VALIDATE | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_VIRTUAL_REFRESH_RATE_AWARE;
            return SetDisplayConfig(pathCount, paths, modeCount, modes, flags) == ERROR_SUCCESS;
        }
        catch { return false; }
    }

    /// <summary>Sets DRR on/off for a target. Returns true on success; false if
    /// the target isn't found, validation fails (unsupported), or the apply fails.</summary>
    public static bool SetState(LUID adapterId, uint targetId, bool enable)
    {
        try
        {
            if (!QueryActive(out var paths, out var pathCount, out var modes, out var modeCount))
                return false;
            int idx = FindPath(paths, pathCount, adapterId, targetId);
            if (idx < 0) return false;

            paths[idx].flags = enable
                ? paths[idx].flags | DISPLAYCONFIG_PATH_BOOST_REFRESH_RATE
                : paths[idx].flags & ~DISPLAYCONFIG_PATH_BOOST_REFRESH_RATE;

            uint validateFlags = SDC_VALIDATE | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_VIRTUAL_REFRESH_RATE_AWARE;
            if (SetDisplayConfig(pathCount, paths, modeCount, modes, validateFlags) != ERROR_SUCCESS)
                return false; // not supported on this path

            uint applyFlags = SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_SAVE_TO_DATABASE | SDC_VIRTUAL_REFRESH_RATE_AWARE;
            return SetDisplayConfig(pathCount, paths, modeCount, modes, applyFlags) == ERROR_SUCCESS;
        }
        catch { return false; }
    }
}
