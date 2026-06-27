using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class ScheduledTaskControllerTests
{
    private const string TaskA = @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser";
    private const string TaskB = @"\Microsoft\Windows\Application Experience\ProgramDataUpdater";

    [Fact]
    public void BuildChangeBatch_SingleTask_Disable_QuotesPathAndUsesDisableFlag()
    {
        var cmd = ScheduledTaskController.BuildChangeBatch(new[] { TaskA }, enable: false);
        Assert.Equal($"schtasks /Change /TN \"{TaskA}\" /Disable", cmd);
        Assert.DoesNotContain(" & ", cmd);
    }

    [Fact]
    public void BuildChangeBatch_MultipleTasks_ChainsIntoSingleCommand()
    {
        var cmd = ScheduledTaskController.BuildChangeBatch(new[] { TaskA, TaskB }, enable: false);
        Assert.Equal(
            $"schtasks /Change /TN \"{TaskA}\" /Disable & schtasks /Change /TN \"{TaskB}\" /Disable",
            cmd);
    }

    [Fact]
    public void BuildChangeBatch_Enable_UsesEnableFlag()
    {
        var cmd = ScheduledTaskController.BuildChangeBatch(new[] { TaskA }, enable: true);
        Assert.Equal($"schtasks /Change /TN \"{TaskA}\" /Enable", cmd);
    }

    [Fact]
    public void BuildChangeBatch_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, ScheduledTaskController.BuildChangeBatch(Array.Empty<string>(), enable: false));
    }

    [Theory]
    [InlineData("evil & calc")]
    [InlineData("task | whoami")]
    [InlineData("task\" & shutdown")]
    [InlineData("task > out.txt")]
    public void BuildChangeBatch_RejectsShellMetacharacters(string maliciousPath)
    {
        Assert.Throws<ArgumentException>(() =>
            ScheduledTaskController.BuildChangeBatch(new[] { maliciousPath }, enable: false));
    }

    [Fact]
    public void ParseState_DisabledStatus_MapsToDisabled()
    {
        const string output = """
            Folder: \Microsoft\Windows\Application Experience
            HostName:      DESKTOP
            TaskName:      \Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser
            Next Run Time: N/A
            Status:        Disabled
            """;
        Assert.Equal(ScheduledTaskState.Disabled, ScheduledTaskController.ParseState(output));
    }

    [Theory]
    [InlineData("Ready")]
    [InlineData("Running")]
    public void ParseState_EnabledStatuses_MapToEnabled(string status)
    {
        var output = $"TaskName: \\foo\nStatus:        {status}\n";
        Assert.Equal(ScheduledTaskState.Enabled, ScheduledTaskController.ParseState(output));
    }

    [Fact]
    public void ParseState_NoStatusLine_MapsToNotPresent()
    {
        Assert.Equal(ScheduledTaskState.NotPresent, ScheduledTaskController.ParseState("ERROR: task not found"));
        Assert.Equal(ScheduledTaskState.NotPresent, ScheduledTaskController.ParseState(""));
    }
}
