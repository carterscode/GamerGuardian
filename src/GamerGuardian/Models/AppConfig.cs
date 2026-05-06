using System.Text.Json.Serialization;

namespace GamerGuardian.Models;

public sealed class AppConfig
{
    public bool LaunchAtStartup { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 30;
    public bool ConsolidateNotifications { get; set; } = true;

    public Dictionary<string, DisplayPreference> Displays { get; set; } = new();
}

public sealed class DisplayPreference
{
    public string DisplayLabel { get; set; } = "";

    public HdrPref Hdr { get; set; } = new();
    public RefreshRatePref RefreshRate { get; set; } = new();
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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RefreshRateTarget
{
    Maximum,
    Fixed,
}
