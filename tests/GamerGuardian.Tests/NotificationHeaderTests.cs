using GamerGuardian.Models;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class NotificationHeaderTests
{
    private static DriftItem Item(string displayKey, string label = "x") => new(
        SettingId: $"{displayKey}:test",
        DisplayKey: displayKey,
        DisplayLabel: label,
        Description: $"{label} -- test",
        CurrentValue: "On",
        DesiredValue: "Off",
        AutoApply: false,
        Apply: () => Task.CompletedTask);

    [Fact]
    public void EmptyReport_GenericHeader()
    {
        var h = NotificationHeader.For(new DriftReport(Array.Empty<DriftItem>()));
        Assert.Equal("Monitored settings have drifted", h);
    }

    [Fact]
    public void NullReport_GenericHeader()
    {
        Assert.Equal("Monitored settings have drifted", NotificationHeader.For(null!));
    }

    [Fact]
    public void SingleAiPolicy_SaysWindowsAi()
    {
        // The bug we're fixing: this used to say "Display settings have drifted"
        // even for an AI policy drift. Verify the AI category is named correctly.
        var h = NotificationHeader.For(new DriftReport(new[] { Item("ai") }));
        Assert.Equal("Windows AI setting has drifted", h);
        Assert.DoesNotContain("Display", h);
    }

    [Fact]
    public void MultipleAiPolicies_PluralForm()
    {
        var h = NotificationHeader.For(new DriftReport(new[] { Item("ai"), Item("ai") }));
        Assert.Equal("Windows AI settings have drifted", h);
    }

    [Fact]
    public void SingleService_SingularForm()
    {
        var h = NotificationHeader.For(new DriftReport(new[] { Item("service") }));
        Assert.Equal("Windows service has drifted", h);
    }

    [Fact]
    public void MultipleServices_PluralForm()
    {
        var h = NotificationHeader.For(new DriftReport(new[] { Item("service"), Item("service") }));
        Assert.Equal("Windows services have drifted", h);
    }

    [Fact]
    public void SingleDisplay_SingularForm()
    {
        var h = NotificationHeader.For(new DriftReport(new[] { Item("display") }));
        Assert.Equal("Display setting has drifted", h);
    }

    [Fact]
    public void GlobalGaming_KnownLabel()
    {
        var h = NotificationHeader.For(new DriftReport(new[] { Item("global") }));
        Assert.Equal("Global gaming setting has drifted", h);
    }

    [Fact]
    public void MixedCategories_GenericCountHeader()
    {
        var h = NotificationHeader.For(new DriftReport(new[]
        {
            Item("ai"), Item("service"), Item("display")
        }));
        Assert.Equal("3 monitored settings have drifted", h);
    }

    [Fact]
    public void UnknownCategory_FallsBackGracefully()
    {
        var h = NotificationHeader.For(new DriftReport(new[] { Item("madeup") }));
        Assert.Contains("drifted", h);
        // Doesn't crash or say "Display"
        Assert.DoesNotContain("Display", h);
    }
}
