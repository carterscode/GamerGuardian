using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class ElevatedRegistryTests
{
    [Fact]
    public void BuildHklmMultiAdd_ChainsWritesWithAmpersand()
    {
        var cmd = ElevatedRegistry.BuildHklmMultiAdd(new[]
        {
            (@"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", "REG_DWORD", "0"),
            (@"SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities", "REG_DWORD", "0"),
        });

        Assert.Equal(
            "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v \"EnableActivityFeed\" /t REG_DWORD /d 0 /f" +
            " && " +
            "reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v \"PublishUserActivities\" /t REG_DWORD /d 0 /f",
            cmd);
    }

    [Fact]
    public void BuildHklmMultiDelete_ChainsDeletesWithAmpersand()
    {
        var cmd = ElevatedRegistry.BuildHklmMultiDelete(new[]
        {
            (@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{GUID-A}", "TcpAckFrequency"),
            (@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{GUID-B}", "TCPNoDelay"),
        });

        Assert.Equal(
            "reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\\{GUID-A}\" /v \"TcpAckFrequency\" /f" +
            " && " +
            "reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\\{GUID-B}\" /v \"TCPNoDelay\" /f",
            cmd);
    }

    [Fact]
    public void BuildHklmMultiDelete_SingleValue_MatchesSingleDeleteShape()
    {
        var cmd = ElevatedRegistry.BuildHklmMultiDelete(new[]
        {
            (@"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR"),
        });

        Assert.Equal(
            "reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR\" /v \"AllowGameDVR\" /f",
            cmd);
        Assert.DoesNotContain("&&", cmd);
    }

    [Fact]
    public void BuildHklmMultiAdd_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal(string.Empty,
            ElevatedRegistry.BuildHklmMultiAdd(Array.Empty<(string, string, string, string)>()));
    }

    [Theory]
    [InlineData("SOFTWARE\\Evil && calc", "Name")]
    [InlineData("SOFTWARE\\Key", "Name && calc")]
    [InlineData("SOFTWARE\\Key", "Name|whoami")]
    [InlineData("SOFTWARE\\Key", "Name\"escape")]
    public void BuildHklmMultiDelete_RejectsShellMetacharacters(string subkey, string name)
    {
        Assert.Throws<ArgumentException>(() =>
            ElevatedRegistry.BuildHklmMultiDelete(new[] { (subkey, name) }));
    }

    [Fact]
    public void BuildHklmMultiAdd_RejectsShellMetacharacterInData()
    {
        Assert.Throws<ArgumentException>(() =>
            ElevatedRegistry.BuildHklmMultiAdd(new[]
            {
                (@"SOFTWARE\Key", "Name", "REG_SZ", "value && calc"),
            }));
    }
}
