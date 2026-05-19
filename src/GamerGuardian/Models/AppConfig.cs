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
    public ToggleSettingPref SystemResponsiveness { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref NetworkThrottling { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref UsbSelectiveSuspend { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref GamesTaskProfile { get; set; } = new() { DesiredOn = true };
    public PowerPlanPref PowerPlan { get; set; } = new();

    // ---- Windows AI lockdown toggles (DesiredOn = true keeps Windows defaults) ----
    // Default DesiredOn=true + Monitor=false: zero behavior change for existing users
    // until they open the Windows AI tab and opt into managing one of these.
    public ToggleSettingPref Copilot { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref Recall { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref ClickToDo { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref EdgeAi { get; set; } = new() { DesiredOn = true };
    public ToggleSettingPref NotepadPaintAi { get; set; } = new() { DesiredOn = true };
}

public sealed class ToggleSettingPref
{
    public bool Monitor { get; set; } = false;
    public bool DesiredOn { get; set; } = true;
    public bool AutoApply { get; set; } = false;
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
