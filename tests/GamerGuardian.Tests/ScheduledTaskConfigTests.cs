using System.Text.Json;
using GamerGuardian.Models;
using Xunit;

namespace GamerGuardian.Tests;

public class ScheduledTaskConfigTests
{
    private const string TaskPath = @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser";

    [Fact]
    public void FreshConfig_HasEmptyScheduledTasks()
    {
        Assert.Empty(new AppConfig().ScheduledTasks);
    }

    [Fact]
    public void DefaultPref_IsUnmanaged()
    {
        var pref = new ScheduledTaskPref();
        Assert.False(pref.Monitor);
        Assert.False(pref.AutoApply);
        Assert.Equal(ScheduledTaskTarget.Default, pref.Desired);
    }

    [Fact]
    public void ScheduledTasks_RoundTripThroughJson()
    {
        var cfg = new AppConfig();
        cfg.ScheduledTasks[TaskPath] = new ScheduledTaskPref
        {
            Monitor = true,
            AutoApply = true,
            Desired = ScheduledTaskTarget.Disabled,
        };

        var json = JsonSerializer.Serialize(cfg);
        var back = JsonSerializer.Deserialize<AppConfig>(json)!;

        Assert.True(back.ScheduledTasks.TryGetValue(TaskPath, out var pref));
        Assert.Equal(ScheduledTaskTarget.Disabled, pref!.Desired);
        Assert.True(pref.Monitor);
        Assert.True(pref.AutoApply);
    }

    [Fact]
    public void Deserialize_ConfigWithoutScheduledTasksSection_IsBackCompatible()
    {
        // Older config.json predates the feature -> ScheduledTasks should default to empty.
        const string legacy = """{ "LaunchAtStartup": true, "PollIntervalSeconds": 30 }""";
        var cfg = JsonSerializer.Deserialize<AppConfig>(legacy)!;
        Assert.NotNull(cfg.ScheduledTasks);
        Assert.Empty(cfg.ScheduledTasks);
    }
}
