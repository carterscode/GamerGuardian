using System.Text.Json.Serialization;

namespace GamerGuardian.Models;

public sealed class AppConfig
{
    public bool LaunchAtStartup { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 30;
    public bool ConsolidateNotifications { get; set; } = true;

    public Dictionary<string, DisplayPreference> Displays { get; set; } = new();
    public GlobalPreferences Global { get; set; } = new();
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
    public PowerPlanPref PowerPlan { get; set; } = new();
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
