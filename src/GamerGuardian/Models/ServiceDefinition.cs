using System.Text.Json.Serialization;

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
    ServiceTargetState? RecommendedTarget = null);

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

/// <summary>
/// What state the user wants a service kept in. <see cref="Default"/> means
/// "don't manage this; restore it to <see cref="ServiceDefinition.DefaultStartType"/>
/// if it ever ends up Disabled or Manual via prior management." Manual and
/// Disabled are explicit targets that include stopping the service if it's running.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServiceTargetState
{
    Default,
    Manual,
    Disabled,
}
