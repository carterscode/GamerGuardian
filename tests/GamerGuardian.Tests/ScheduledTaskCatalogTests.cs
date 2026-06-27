using GamerGuardian.Models;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class ScheduledTaskCatalogTests
{
    [Fact]
    public void All_ContainsTasks()
    {
        Assert.NotEmpty(ScheduledTaskCatalog.All);
    }

    [Fact]
    public void All_HasNoDuplicateTaskPaths()
    {
        var paths = ScheduledTaskCatalog.All.Select(d => d.TaskPath).ToList();
        Assert.Equal(paths.Count, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void All_EveryEntryHasNonEmptyFields()
    {
        foreach (var def in ScheduledTaskCatalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(def.TaskPath), "empty TaskPath for an entry");
            Assert.False(string.IsNullOrWhiteSpace(def.DisplayName), $"empty DisplayName for {def.TaskPath}");
            Assert.False(string.IsNullOrWhiteSpace(def.Description), $"empty Description for {def.TaskPath}");
        }
    }

    [Fact]
    public void All_EveryTaskPathIsFullyQualifiedUnderApplicationExperience()
    {
        foreach (var def in ScheduledTaskCatalog.All)
        {
            Assert.StartsWith(@"\Microsoft\Windows\Application Experience\", def.TaskPath);
        }
    }

    [Fact]
    public void All_EveryEntryRecommendsDisabled()
    {
        // The design decision is to disable the whole Application Experience folder
        // via the Recommended preset, so every entry must carry RecommendedTarget=Disabled.
        foreach (var def in ScheduledTaskCatalog.All)
        {
            Assert.Equal(ScheduledTaskTarget.Disabled, def.RecommendedTarget);
        }
    }

    [Theory]
    [InlineData(@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser")]
    [InlineData(@"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser Exp")]
    [InlineData(@"\Microsoft\Windows\Application Experience\ProgramDataUpdater")]
    [InlineData(@"\Microsoft\Windows\Application Experience\StartupAppTask")]
    [InlineData(@"\Microsoft\Windows\Application Experience\PcaPatchDbTask")]
    public void All_IncludesExpectedTasks(string taskPath)
    {
        Assert.Contains(ScheduledTaskCatalog.All, d =>
            d.TaskPath.Equals(taskPath, StringComparison.OrdinalIgnoreCase));
    }
}
