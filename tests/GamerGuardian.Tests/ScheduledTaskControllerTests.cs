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

    private const string Ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";

    [Fact]
    public void ParseEnabledState_SettingsEnabledFalse_MapsToDisabled()
    {
        var xml = $"""
            <?xml version="1.0" encoding="UTF-16"?>
            <Task version="1.4" xmlns="{Ns}">
              <Settings>
                <Enabled>false</Enabled>
              </Settings>
            </Task>
            """;
        Assert.Equal(ScheduledTaskState.Disabled, ScheduledTaskController.ParseEnabledState(xml));
    }

    [Fact]
    public void ParseEnabledState_SettingsEnabledTrue_MapsToEnabled()
    {
        var xml = $"""<Task xmlns="{Ns}"><Settings><Enabled>true</Enabled></Settings></Task>""";
        Assert.Equal(ScheduledTaskState.Enabled, ScheduledTaskController.ParseEnabledState(xml));
    }

    [Fact]
    public void ParseEnabledState_NoSettingsEnabled_DefaultsToEnabled()
    {
        var xml = $"""<Task xmlns="{Ns}"><Settings/></Task>""";
        Assert.Equal(ScheduledTaskState.Enabled, ScheduledTaskController.ParseEnabledState(xml));
    }

    [Fact]
    public void ParseEnabledState_TriggerEnabledFalse_DoesNotMaskSettingsEnabled()
    {
        // A disabled trigger must not be read as a disabled task; only Settings/Enabled counts.
        var xml = $"""
            <Task xmlns="{Ns}">
              <Triggers><CalendarTrigger><Enabled>false</Enabled></CalendarTrigger></Triggers>
              <Settings><Enabled>true</Enabled></Settings>
            </Task>
            """;
        Assert.Equal(ScheduledTaskState.Enabled, ScheduledTaskController.ParseEnabledState(xml));
    }

    [Fact]
    public void ParseEnabledState_EmptyOrGarbage_MapsToNotPresent()
    {
        Assert.Equal(ScheduledTaskState.NotPresent, ScheduledTaskController.ParseEnabledState(""));
        Assert.Equal(ScheduledTaskState.NotPresent, ScheduledTaskController.ParseEnabledState("ERROR: not found"));
    }
}
