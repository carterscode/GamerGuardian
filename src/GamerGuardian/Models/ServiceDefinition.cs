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
    ServiceTargetState? RecommendedTarget = null,
    PolicyOverride? PolicyOverride = null);

/// <summary>
/// Some Windows services (DoSvc, etc.) are protected by Windows Update's
/// self-healing pipeline (WaaSMedicSvc) — sc.exe writes to the service
/// start type are accepted but reverted within seconds. The right way to
/// influence them is via the documented Group Policy registry surface
/// instead. When a <see cref="ServiceDefinition"/> has a non-null
/// <see cref="ServiceDefinition.PolicyOverride"/>, the monitor stops
/// trying to change the service start type and writes this policy value
/// instead — Windows Update respects its own policy and doesn't revert.
/// </summary>
public sealed record PolicyOverride(
    string PolicyKey,
    string PolicyValue,
    uint DisabledValue,
    string Description);

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
