using System.Net.NetworkInformation;

namespace GamerGuardian.Native;

/// <summary>
/// Enumerates active physical network adapters (Ethernet / Wi-Fi that are up),
/// used by the per-interface Nagle and NIC power-management monitors. Backed by
/// <see cref="NetworkInterface.GetAllNetworkInterfaces"/> (the managed wrapper
/// over IP Helper's GetAdaptersAddresses) -- far lighter than WMI.
///
/// <para>Results are cached for a short window so the two NIC monitors share one
/// enumeration per poll tick and the apply+verify retry loop doesn't re-enumerate
/// repeatedly. The cache is time-boxed (no network-change event subscription, so
/// no new always-running listener -- R20).</para>
/// </summary>
public static class NetworkAdapters
{
    /// <param name="Guid">Interface GUID string ("{...}"), used as the
    /// Tcpip\Parameters\Interfaces\&lt;GUID&gt; and adapter-class subkey.</param>
    public readonly record struct AdapterInfo(string Guid, string Name);

    private static readonly TimeSpan CacheWindow = TimeSpan.FromSeconds(3);
    private static readonly object Gate = new();
    private static IReadOnlyList<AdapterInfo>? _cache;
    private static DateTime _cacheStampUtc;

    /// <summary>Pure predicate: an adapter counts as active+physical when it is up
    /// and an Ethernet or Wi-Fi type (excludes loopback, tunnel, virtual-only types).</summary>
    public static bool IsActivePhysical(NetworkInterfaceType type, OperationalStatus status)
    {
        if (status != OperationalStatus.Up) return false;
        return type is NetworkInterfaceType.Ethernet
            or NetworkInterfaceType.GigabitEthernet
            or NetworkInterfaceType.FastEthernetT
            or NetworkInterfaceType.FastEthernetFx
            or NetworkInterfaceType.Wireless80211;
    }

    /// <summary>Active physical adapters, cached for a short window. Empty list on error.</summary>
    public static IReadOnlyList<AdapterInfo> GetActivePhysical()
    {
        lock (Gate)
        {
            if (_cache is not null && DateTime.UtcNow - _cacheStampUtc < CacheWindow)
                return _cache;

            var list = new List<AdapterInfo>();
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                    if (IsActivePhysical(ni.NetworkInterfaceType, ni.OperationalStatus))
                        list.Add(new AdapterInfo(ni.Id, ni.Name));
            }
            catch { /* return whatever we gathered (possibly empty) */ }

            _cache = list;
            _cacheStampUtc = DateTime.UtcNow;
            return list;
        }
    }
}
