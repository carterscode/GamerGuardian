using System.Net.NetworkInformation;
using GamerGuardian.Native;
using Xunit;

namespace GamerGuardian.Tests;

public class NetworkAdaptersTests
{
    [Theory]
    [InlineData(NetworkInterfaceType.Ethernet, OperationalStatus.Up, true)]
    [InlineData(NetworkInterfaceType.GigabitEthernet, OperationalStatus.Up, true)]
    [InlineData(NetworkInterfaceType.Wireless80211, OperationalStatus.Up, true)]
    [InlineData(NetworkInterfaceType.Ethernet, OperationalStatus.Down, false)]   // not up
    [InlineData(NetworkInterfaceType.Loopback, OperationalStatus.Up, false)]      // not physical
    [InlineData(NetworkInterfaceType.Tunnel, OperationalStatus.Up, false)]        // tunnel excluded
    [InlineData(NetworkInterfaceType.Ppp, OperationalStatus.Up, false)]
    public void IsActivePhysical_FiltersByTypeAndStatus(
        NetworkInterfaceType type, OperationalStatus status, bool expected)
    {
        Assert.Equal(expected, NetworkAdapters.IsActivePhysical(type, status));
    }
}
