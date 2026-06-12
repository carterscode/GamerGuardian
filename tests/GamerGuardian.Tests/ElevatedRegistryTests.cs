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

    [Fact]
    public void BuildHklmBatch_ChainsAddsBeforeDeletes()
    {
        var cmd = ElevatedRegistry.BuildHklmBatch(
            new[] { (@"SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", "REG_DWORD", "0") },
            new[] { (@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "WasEnabledBy") });

        Assert.Equal(
            "reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\" /v \"EnableVirtualizationBasedSecurity\" /t REG_DWORD /d 0 /f" +
            " && (" +
            "reg delete \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /v \"WasEnabledBy\" /f" +
            ")",
            cmd);
    }

    [Fact]
    public void BuildHklmBatch_DeletesAreFailureTolerant_SingleAmpersand()
    {
        // A value deleted out from under us between snapshot and UAC approval
        // must not abort the remaining cleanup — deletes chain with '&', not '&&'.
        var cmd = ElevatedRegistry.BuildHklmBatch(
            Array.Empty<(string, string, string, string)>(),
            new[] { (@"SOFTWARE\Key", "A"), (@"SOFTWARE\Key", "B") });

        Assert.Equal(
            "reg delete \"HKLM\\SOFTWARE\\Key\" /v \"A\" /f" +
            " & " +
            "reg delete \"HKLM\\SOFTWARE\\Key\" /v \"B\" /f",
            cmd);
    }

    [Fact]
    public void BuildHklmBatch_AddsOnly_OmitsTrailingChain()
    {
        var cmd = ElevatedRegistry.BuildHklmBatch(
            new[] { (@"SOFTWARE\Key", "Name", "REG_DWORD", "0") },
            Array.Empty<(string, string)>());

        Assert.Equal("reg add \"HKLM\\SOFTWARE\\Key\" /v \"Name\" /t REG_DWORD /d 0 /f", cmd);
    }

    [Fact]
    public void BuildHklmBatch_DeletesOnly_OmitsLeadingChain()
    {
        var cmd = ElevatedRegistry.BuildHklmBatch(
            Array.Empty<(string, string, string, string)>(),
            new[] { (@"SOFTWARE\Key", "Name") });

        Assert.Equal("reg delete \"HKLM\\SOFTWARE\\Key\" /v \"Name\" /f", cmd);
    }

    [Fact]
    public void BuildHklmBatch_EmptyInputs_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ElevatedRegistry.BuildHklmBatch(
            Array.Empty<(string, string, string, string)>(),
            Array.Empty<(string, string)>()));
    }

    [Fact]
    public void BuildHklmBatch_RejectsShellMetacharacters()
    {
        Assert.Throws<ArgumentException>(() => ElevatedRegistry.BuildHklmBatch(
            Array.Empty<(string, string, string, string)>(),
            new[] { (@"SOFTWARE\Key", "Name && calc") }));
    }

    [Theory]
    [InlineData("REG_DWORD && calc")]
    [InlineData("REG_TYPO")]
    [InlineData("")]
    public void BuildHklmMultiAdd_RejectsKindOutsideWhitelist(string kind)
    {
        // The /t type token is interpolated unquoted — it is the one segment the
        // metacharacter blocklist used to skip, so it gets a strict REG_* whitelist.
        Assert.Throws<ArgumentException>(() =>
            ElevatedRegistry.BuildHklmMultiAdd(new[] { (@"SOFTWARE\Key", "Name", kind, "0") }));
    }

    [Fact]
    public void BuildHklmMultiAdd_AcceptsAllWhitelistedKinds()
    {
        foreach (var kind in new[] { "REG_DWORD", "REG_QWORD", "REG_SZ", "REG_EXPAND_SZ", "REG_MULTI_SZ", "REG_BINARY" })
        {
            var cmd = ElevatedRegistry.BuildHklmMultiAdd(new[] { (@"SOFTWARE\Key", "Name", kind, "0") });
            Assert.Contains($"/t {kind}", cmd);
        }
    }
}
