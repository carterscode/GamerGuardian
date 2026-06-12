using System.Text.Json.Serialization;

namespace GamerGuardian.Models;

public sealed class AppConfig
{
    public bool LaunchAtStartup { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 30;
    public bool ConsolidateNotifications { get; set; } = true;
    public AppThemeChoice Theme { get; set; } = AppThemeChoice.System;
    public bool CheckForUpdatesOnStartup { get; set; } = true;
    public string? SkippedUpdateVersion { get; set; }

    public Dictionary<string, DisplayPreference> Displays { get; set; } = new();
    public GlobalPreferences Global { get; set; } = new();
    public Dictionary<string, ServicePref> Services { get; set; } = new();
    /// <summary>Per-package preferences for the Windows AI UWP removal feature.
    /// Keyed by <c>WindowsAiAppDefinition.PackageName</c>.</summary>
    public Dictionary<string, WindowsAiAppPref> WindowsAiApps { get; set; } = new();
}

public sealed class ServicePref
{
    public bool Monitor { get; set; } = false;
    public ServiceTargetState Desired { get; set; } = ServiceTargetState.Default;
    public bool AutoApply { get; set; } = false;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppThemeChoice
{
    System,
    Light,
    Dark,
}

public sealed class DisplayPreference
{
    public string DisplayLabel { get; set; } = "";

    public HdrPref Hdr { get; set; } = new();
    public RefreshRatePref RefreshRate { get; set; } = new();
    public ResolutionPref Resolution { get; set; } = new();
    public DrrPref Drr { get; set; } = new();
}

public sealed class DrrPref
{
    public bool Monitor { get; set; } = false;
    // DRR is content-driven; gaming-recommended direction is user choice, so the
    // desired value defaults to enabled and Monitor is off (zero behavior change
    // until the user opts in on a DRR-capable display).
    public bool DesiredOn { get; set; } = true;
    public bool AutoApply { get; set; } = false;
}

public sealed class HdrPref
{
    public bool Monitor { get; set; } = true;
    public bool DesiredOn { get; set; } = true;
    public bool AutoApply { get; set; } = false;
}

public sealed class RefreshRatePref
{
    public bool Monitor { get; set; } = true;
    public RefreshRateTarget Target { get; set; } = RefreshRateTarget.Maximum;
    public uint? FixedHz { get; set; }
    public bool AutoApply { get; set; } = false;
}

public sealed class ResolutionPref
{
    public bool Monitor { get; set; } = false;
    public uint? DesiredWidth { get; set; }
    public uint? DesiredHeight { get; set; }
    public bool AutoApply { get; set; } = false;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RefreshRateTarget
{
    Maximum,
    Fixed,
}

public sealed class GlobalPreferences
{
    public ToggleSettingPref Hags { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref GameMode { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref GameDvr { get; set; } = new() { DesiredOn = false };
    public ToggleSettingPref MousePrecision { get; set; } = new() { DesiredOn = false };
    public ToggleSettingPref FullscreenOptimizations { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref Vrr { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref MemoryIntegrity { get; set; } = new() { DesiredOn = false };
    /// <summary>Full VBS stack (superset of MemoryIntegrity — all DeviceGuard
    /// scenarios + Credential Guard + policy mirror). Monitor=false default: zero
    /// behavior change until the user opts in; the UI syncs DesiredOn from the
    /// live system state while unmonitored. DesiredOn follows literal feature
    /// state (true = VBS enabled/secure), matching the MemoryIntegrity precedent —
    /// an intentional exception to the DesiredOn=gaming-optimized convention for
    /// security toggles.</summary>
    public ToggleSettingPref Vbs { get; set; } = new() { DesiredOn = false };
    public ToggleSettingPref SystemResponsiveness { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref NetworkThrottling { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref UsbSelectiveSuspend { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref GamesTaskProfile { get; set; } = new() { DesiredOn = true };
    public PowerPlanPref PowerPlan { get; set; } = new();
    /// <summary>Identity of the GamerGuardian-authored CPU-optimized power plan,
    /// for idempotent rebuild/re-tune. Nested under Global so it rides the
    /// existing config clone.</summary>
    public CpuPlanPref CpuPlan { get; set; } = new();

    // ---- Windows AI lockdown toggles (DesiredOn = true keeps Windows defaults) ----
    // Default DesiredOn=true + Monitor=false: zero behavior change for existing users
    // until they open the Windows AI tab and opt into managing one of these.
    public ToggleSettingPref Copilot { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref Recall { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref ClickToDo { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref EdgeAi { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref NotepadPaintAi { get; set; } = new() { DesiredOn = true };
    // v0.1.39 additions for closer parity with zoicware/RemoveWindowsAI:
    public ToggleSettingPref SettingsSearchAi { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref AiActions { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref InputInsights { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref OfficeCopilot { get; set; } = new() { DesiredOn = true };

    // ---- Privacy / telemetry toggles (Privacy tab). Monitor=false by default ----
    // Intuitive Enabled/Disabled settings: DesiredOn maps to the feature being
    // enabled, so the privacy-optimized default is DesiredOn=false (off).
    public ToggleSettingPref AdvertisingId { get; set; } = new() { DesiredOn = false };
    public ToggleSettingPref TailoredExperiences { get; set; } = new() { DesiredOn = false };
    // Inverted Gaming/Default settings (HKLM policy): DesiredOn=true is the gaming
    // state (disabled-by-policy), matching the NetworkThrottling/USB convention.
    public ToggleSettingPref Cdp { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref ActivityHistory { get; set; } = new() { DesiredOn = true };

    // ---- System toggles (inverted Gaming/Default; DesiredOn=true = gaming) ----
    public ToggleSettingPref PowerThrottling { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref FastStartup { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref VisualFx { get; set; } = new() { DesiredOn = true };

    // ---- Network toggles (Network tab; contested per-hardware tweaks) ----
    public ToggleSettingPref Nagle { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref NicPower { get; set; } = new() { DesiredOn = true };
}

public sealed class ToggleSettingPref
{
    public bool Monitor { get; set; } = false;
    public bool DesiredOn { get; set; } = true;
    public bool AutoApply { get; set; } = false;
}

/// <summary>
/// Identity of the GamerGuardian-authored CPU-optimized scheme so a rebuild can
/// reuse / re-tune it instead of stacking duplicates. Power-scheme GUIDs are
/// machine-local, so the machine token guards against a roamed config deleting a
/// foreign machine's plan.
/// </summary>
public sealed class CpuPlanPref
{
    /// <summary>GUID of the GG-authored scheme this app built, if any.</summary>
    public string? BuiltSchemeGuid { get; set; }
    /// <summary>Content hash of the override set the scheme was tuned to.</summary>
    public string? ContentHash { get; set; }
    /// <summary>Machine identity the scheme was built on (MachineGuid).</summary>
    public string? MachineToken { get; set; }
    /// <summary>CPU model the scheme was built for (display + sanity).</summary>
    public string? CpuModel { get; set; }
}

public sealed class PowerPlanPref
{
    public bool Monitor { get; set; } = false;
    public PowerPlanChoice Desired { get; set; } = PowerPlanChoice.HighPerformance;
    /// <summary>Specific power scheme GUID the user picked. Takes precedence over <see cref="Desired"/> when set.</summary>
    public string? DesiredGuid { get; set; }
    /// <summary>Friendly name cached at selection time (for display when the GUID isn't currently installed).</summary>
    public string? DesiredName { get; set; }
    public bool AutoApply { get; set; } = false;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PowerPlanChoice
{
    Balanced,
    HighPerformance,
    PowerSaver,
    UltimatePerformance,
}
