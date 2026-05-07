using GamerGuardian.Models;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class WindowsServiceControllerTests
{
    private const string DefinitelyNotAService = "GamerGuardianFakeServiceForTests";

    [Fact]
    public void Exists_NonexistentService_ReturnsFalse()
    {
        Assert.False(WindowsServiceController.Exists(DefinitelyNotAService));
    }

    [Fact]
    public void ReadStartType_NonexistentService_ReturnsUnknown()
    {
        // Should not throw; should return Unknown so callers can treat it as "skip".
        Assert.Equal(ServiceStartType.Unknown, WindowsServiceController.ReadStartType(DefinitelyNotAService));
    }

    [Fact]
    public void ReadStatus_NonexistentService_ReturnsNull()
    {
        Assert.Null(WindowsServiceController.ReadStatus(DefinitelyNotAService));
    }

    // EventLog is a service that exists on every supported Windows install and
    // boots automatically. Reading its registry start type should succeed and
    // never return Unknown. We don't assert the specific value because Microsoft
    // has changed it over time (Auto vs AutoDelayed).
    [Fact]
    public void ReadStartType_EventLog_ReturnsKnown()
    {
        var start = WindowsServiceController.ReadStartType("EventLog");
        Assert.NotEqual(ServiceStartType.Unknown, start);
    }

    [Fact]
    public void Exists_EventLog_ReturnsTrue()
    {
        Assert.True(WindowsServiceController.Exists("EventLog"));
    }
}
