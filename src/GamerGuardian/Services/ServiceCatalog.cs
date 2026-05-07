using GamerGuardian.Models;

namespace GamerGuardian.Services;

/// <summary>
/// Curated list of Windows services that are commonly disabled for gaming
/// setups. Conservative picks only — services here either have no impact on a
/// typical desktop gaming user or have a well-understood tradeoff. Items with
/// <see cref="ServiceDefinition.RecommendedDisable"/> = true are toggled by the
/// "Gaming optimized" preset; the rest are listed but require explicit opt-in.
/// </summary>
public static class ServiceCatalog
{
    public static IReadOnlyList<ServiceDefinition> All { get; } = new ServiceDefinition[]
    {
        new(
            Name: "DiagTrack",
            DisplayName: "Connected User Experiences and Telemetry",
            Description: "Sends diagnostic and usage data to Microsoft. Disabling cuts background CPU/network and is safe on consumer Windows.",
            DefaultStartType: ServiceStartType.Automatic,
            RecommendedDisable: true),

        new(
            Name: "MapsBroker",
            DisplayName: "Downloaded Maps Manager",
            Description: "Background downloads for offline maps. Useless if you don't use the Maps app.",
            DefaultStartType: ServiceStartType.AutomaticDelayed,
            RecommendedDisable: true),

        new(
            Name: "WMPNetworkSvc",
            DisplayName: "Windows Media Player Network Sharing",
            Description: "Shares Windows Media Player libraries on the network. Almost no one uses this in 2026.",
            DefaultStartType: ServiceStartType.Manual,
            RecommendedDisable: true),

        new(
            Name: "Fax",
            DisplayName: "Fax",
            Description: "Sends and receives faxes via a connected fax machine. Disable unless you actually fax.",
            DefaultStartType: ServiceStartType.Manual,
            RecommendedDisable: true),

        new(
            Name: "RetailDemo",
            DisplayName: "Retail Demo Service",
            Description: "Powers Windows demo mode in retail stores. Has no purpose on a personal machine.",
            DefaultStartType: ServiceStartType.Manual,
            RecommendedDisable: true),

        new(
            Name: "WerSvc",
            DisplayName: "Windows Error Reporting",
            Description: "Collects crash data and sends it to Microsoft. Crashes still happen; only the upload is suppressed.",
            DefaultStartType: ServiceStartType.Manual,
            RecommendedDisable: true),

        new(
            Name: "lfsvc",
            DisplayName: "Geolocation Service",
            Description: "Provides location data to apps. Disable if no app on your machine needs your location.",
            DefaultStartType: ServiceStartType.Manual,
            RecommendedDisable: true),

        new(
            Name: "WSearch",
            DisplayName: "Windows Search",
            Description: "Indexes files for fast Start-menu/Explorer search. Disabling drops background I/O but breaks search-as-you-type. Off by default in the preset — opt in if you don't rely on it.",
            DefaultStartType: ServiceStartType.AutomaticDelayed,
            RecommendedDisable: false),

        new(
            Name: "SysMain",
            DisplayName: "SysMain (Superfetch)",
            Description: "Pre-loads frequently-used apps into RAM. Controversial on SSDs — some report better latency disabled, others see slower app launches. Off by default in the preset.",
            DefaultStartType: ServiceStartType.Automatic,
            RecommendedDisable: false),

        new(
            Name: "TabletInputService",
            DisplayName: "Touch Keyboard and Handwriting Panel",
            Description: "On-screen keyboard and pen input. Useless on a non-touch desktop.",
            DefaultStartType: ServiceStartType.Manual,
            RecommendedDisable: false),
    };
}
