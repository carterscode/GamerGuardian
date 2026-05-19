using GamerGuardian.Models;

namespace GamerGuardian.Services;

/// <summary>
/// Curated list of Windows services that GamerGuardian knows about.
/// <see cref="ServiceDefinition.RecommendedTarget"/> drives the "Gaming optimized"
/// preset: services with a non-null target are flipped when the preset is selected.
/// Items with RecommendedTarget = null are listed but require explicit per-service
/// opt-in (typical for high-impact services like the Print Spooler, or Xbox /
/// gaming-adjacent services some users actually need).
/// </summary>
public static class ServiceCatalog
{
    public static IReadOnlyList<ServiceDefinition> All { get; } = new ServiceDefinition[]
    {
        // ---------------- In Gaming-optimized preset ----------------
        new(
            Name: "DiagTrack",
            DisplayName: "Connected User Experiences and Telemetry",
            Description: "Sends diagnostic and usage data to Microsoft. Disabling cuts background CPU/network and is safe on consumer Windows.",
            DefaultStartType: ServiceStartType.Automatic,
            RecommendedTarget: ServiceTargetState.Disabled),

        new(
            Name: "MapsBroker",
            DisplayName: "Downloaded Maps Manager",
            Description: "Background downloads for offline maps. Useless if you don't use the Maps app.",
            DefaultStartType: ServiceStartType.AutomaticDelayed,
            RecommendedTarget: ServiceTargetState.Disabled),

        new(
            Name: "WMPNetworkSvc",
            DisplayName: "Windows Media Player Network Sharing",
            Description: "Shares Windows Media Player libraries on the network. Almost no one uses this in 2026.",
            DefaultStartType: ServiceStartType.Manual,
            RecommendedTarget: ServiceTargetState.Disabled),

        new(
            Name: "Fax",
            DisplayName: "Fax",
            Description: "Sends and receives faxes via a connected fax machine. Disable unless you actually fax.",
            DefaultStartType: ServiceStartType.Manual,
            RecommendedTarget: ServiceTargetState.Disabled),

        new(
            Name: "RetailDemo",
            DisplayName: "Retail Demo Service",
            Description: "Powers Windows demo mode in retail stores. Has no purpose on a personal machine.",
            DefaultStartType: ServiceStartType.Manual,
            RecommendedTarget: ServiceTargetState.Disabled),

        new(
            Name: "WerSvc",
            DisplayName: "Windows Error Reporting",
            Description: "Collects crash data and sends it to Microsoft. Crashes still happen; only the upload is suppressed.",
            DefaultStartType: ServiceStartType.Manual,
            RecommendedTarget: ServiceTargetState.Disabled),

        new(
            Name: "lfsvc",
            DisplayName: "Geolocation Service",
            Description: "Provides location data to apps. Disable if no app on your machine needs your location.",
            DefaultStartType: ServiceStartType.Manual,
            RecommendedTarget: ServiceTargetState.Disabled),

        // ---------------- Listed but NOT in preset ----------------
        new(
            Name: "WSearch",
            DisplayName: "Windows Search",
            Description: "Indexes files for fast Start-menu/Explorer search. Disabling drops background I/O but breaks search-as-you-type. Opt in if you don't rely on it.",
            DefaultStartType: ServiceStartType.AutomaticDelayed),

        new(
            Name: "SysMain",
            DisplayName: "SysMain (Superfetch)",
            Description: "Pre-loads frequently-used apps into RAM. Controversial on SSDs — some report better latency disabled, others see slower app launches.",
            DefaultStartType: ServiceStartType.Automatic),

        new(
            Name: "TabletInputService",
            DisplayName: "Touch Keyboard and Handwriting Panel",
            Description: "On-screen keyboard and pen input. Useless on a non-touch desktop.",
            DefaultStartType: ServiceStartType.Manual),

        new(
            Name: "Spooler",
            DisplayName: "Print Spooler",
            Description: "Sends print jobs to printers. Disable only if you have no printer or print server. Some games / apps will hang for several seconds at launch trying to enumerate printers if this is disabled — try Manual first.",
            DefaultStartType: ServiceStartType.Automatic),

        new(
            Name: "RemoteRegistry",
            DisplayName: "Remote Registry",
            Description: "Lets remote machines read/write your registry. Disabled by default on modern Windows; included here so you can confirm it stays that way.",
            DefaultStartType: ServiceStartType.Disabled),

        new(
            Name: "DoSvc",
            DisplayName: "Delivery Optimization",
            Description: "Peer-to-peer Windows Update / Store delivery. Windows actively reverts attempts to disable this service via standard tools. Instead, GamerGuardian uses the documented Group Policy (DODownloadMode = 0) to put DoSvc in HTTP-only mode — your machine still gets updates but stops uploading them to other PCs. Reboot recommended for the policy to apply across all OS components.",
            DefaultStartType: ServiceStartType.AutomaticDelayed,
            RequiresReboot: true,
            PolicyOverride: new PolicyOverride(
                PolicyKey: @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization",
                PolicyValue: "DODownloadMode",
                DisabledValue: 0,
                Description: "DODownloadMode = 0 disables P2P sharing (HTTP-only mode).")),

        new(
            Name: "iphlpsvc",
            DisplayName: "IP Helper",
            Description: "IPv6 transition (Teredo, 6to4, ISATAP). Most home networks don't need it — Manual is the safer choice over Disabled if anything on your network ever uses IPv6 tunneling.",
            DefaultStartType: ServiceStartType.Automatic),

        // ---------------- Xbox / gaming-related (NOT in preset) ----------------
        new(
            Name: "XboxGipSvc",
            DisplayName: "Xbox Accessory Management Service",
            Description: "Firmware updates and configuration for Xbox accessories (controllers, headsets). Leave alone if you use an Xbox controller.",
            DefaultStartType: ServiceStartType.Manual),

        new(
            Name: "XblAuthManager",
            DisplayName: "Xbox Live Auth Manager",
            Description: "Authentication for Xbox Live. Required for Microsoft Store games and Game Pass. Disable only if you don't use either.",
            DefaultStartType: ServiceStartType.Manual),

        new(
            Name: "XblGameSave",
            DisplayName: "Xbox Live Game Save",
            Description: "Cloud saves for Microsoft Store / Game Pass titles. Disable only if you don't use those.",
            DefaultStartType: ServiceStartType.Manual),

        new(
            Name: "XboxNetApiSvc",
            DisplayName: "Xbox Live Networking Service",
            Description: "Multiplayer / networking glue for Microsoft Store games. Disable only if you don't use Game Pass / Store games online.",
            DefaultStartType: ServiceStartType.Manual),

        // ---------------- Windows AI ----------------
        new(
            Name: "WSAIFabricSvc",
            DisplayName: "Windows AI Fabric Service",
            Description: "Backs the on-device AI runtime (Copilot+ / Recall / Click-to-Do). Disable only if you've decided you don't want Windows AI features at all -- otherwise those features will fail to launch. Pairs naturally with the Windows AI tab toggles.",
            DefaultStartType: ServiceStartType.Manual),
    };
}
