using GamerGuardian.Models;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class AppConfigClonerTests
{
    [Fact]
    public void Clone_ProducesIndependentObject()
    {
        var src = new AppConfig
        {
            LaunchAtStartup = true,
            PollIntervalSeconds = 45,
            Theme = AppThemeChoice.Dark,
        };
        src.Services["DiagTrack"] = new ServicePref { Desired = ServiceTargetState.Disabled, Monitor = true, AutoApply = true };
        src.Global.GameMode.DesiredOn = false;

        var clone = AppConfigCloner.Clone(src);

        Assert.NotSame(src, clone);
        Assert.NotSame(src.Services, clone.Services);
        Assert.NotSame(src.Services["DiagTrack"], clone.Services["DiagTrack"]);
        Assert.NotSame(src.Global, clone.Global);
        Assert.NotSame(src.Global.GameMode, clone.Global.GameMode);

        // Value equality at every level.
        Assert.Equal(src.LaunchAtStartup, clone.LaunchAtStartup);
        Assert.Equal(src.PollIntervalSeconds, clone.PollIntervalSeconds);
        Assert.Equal(src.Theme, clone.Theme);
        Assert.Equal(ServiceTargetState.Disabled, clone.Services["DiagTrack"].Desired);
        Assert.True(clone.Services["DiagTrack"].AutoApply);
        Assert.False(clone.Global.GameMode.DesiredOn);
    }

    [Fact]
    public void Clone_MutationsOnDraftDoNotLeakBackToSource()
    {
        // This is the core invariant the staged-apply UI relies on: the user
        // can flip every radio in the draft and the committed config is
        // untouched until ApplyChangesAsync copies it back.
        var src = new AppConfig();
        src.Services["BITS"] = new ServicePref { Desired = ServiceTargetState.Default };
        src.Global.GameDvr.DesiredOn = true;

        var draft = AppConfigCloner.Clone(src);
        draft.Services["BITS"].Desired = ServiceTargetState.Disabled;
        draft.Global.GameDvr.DesiredOn = false;
        draft.PollIntervalSeconds = 999;

        Assert.Equal(ServiceTargetState.Default, src.Services["BITS"].Desired);
        Assert.True(src.Global.GameDvr.DesiredOn);
        Assert.NotEqual(999, src.PollIntervalSeconds);
    }

    [Fact]
    public void CopyInto_OverwritesFieldsWithoutReplacingTargetReference()
    {
        // CopyInto exists so background components that already hold a reference
        // to the live AppConfig keep seeing the same object after a commit.
        // If we replaced _config wholesale, the MonitorService's captured ref
        // would still point at the pre-commit instance.
        var live = new AppConfig { PollIntervalSeconds = 30 };
        var liveRef = live;
        var draft = AppConfigCloner.Clone(live);
        draft.PollIntervalSeconds = 60;
        draft.Services["Spooler"] = new ServicePref { Desired = ServiceTargetState.Manual };
        draft.Global.Hags.DesiredOn = false;

        AppConfigCloner.CopyInto(draft, live);

        Assert.Same(liveRef, live);
        Assert.Equal(60, live.PollIntervalSeconds);
        Assert.Equal(ServiceTargetState.Manual, live.Services["Spooler"].Desired);
        Assert.False(live.Global.Hags.DesiredOn);
    }

    [Fact]
    public void Clone_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AppConfigCloner.Clone(null!));
    }

    [Fact]
    public void CopyInto_NullArgs_Throws()
    {
        var cfg = new AppConfig();
        Assert.Throws<ArgumentNullException>(() => AppConfigCloner.CopyInto(null!, cfg));
        Assert.Throws<ArgumentNullException>(() => AppConfigCloner.CopyInto(cfg, null!));
    }
}
