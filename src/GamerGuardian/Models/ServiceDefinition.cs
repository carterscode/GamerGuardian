namespace GamerGuardian.Models;

/// <summary>
/// Static metadata for a Windows service that GamerGuardian knows about.
/// Lives in <see cref="GamerGuardian.Services.ServiceCatalog"/>; per-user prefs
/// live in <see cref="AppConfig.Services"/> keyed by <see cref="Name"/>.
/// </summary>
public sealed record ServiceDefinition(
    string Name,
    string DisplayName,
    string Description,
    ServiceStartType DefaultStartType,
    bool RequiresReboot = false,
    bool RecommendedDisable = false);

public enum ServiceStartType
{
    Unknown = 0,
    Boot = 1,
    System = 2,
    Automatic = 3,
    Manual = 4,
    Disabled = 5,
    AutomaticDelayed = 6,
}
