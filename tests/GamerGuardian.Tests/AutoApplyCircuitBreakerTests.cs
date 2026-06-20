using System;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class AutoApplyCircuitBreakerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NotTripped_Initially()
    {
        var b = new AutoApplyCircuitBreaker(tripThreshold: 3);
        Assert.False(b.IsTripped("hags", T0));
        Assert.Equal(0, b.ResetCount("hags"));
    }

    [Fact]
    public void Trips_OnlyOnceThresholdResetsReached()
    {
        var b = new AutoApplyCircuitBreaker(tripThreshold: 3, cooldown: TimeSpan.FromMinutes(15));

        Assert.False(b.RecordExternalReset("hags", T0));                 // 1
        Assert.False(b.IsTripped("hags", T0));
        Assert.False(b.RecordExternalReset("hags", T0.AddSeconds(30)));  // 2
        Assert.False(b.IsTripped("hags", T0.AddSeconds(30)));

        Assert.True(b.RecordExternalReset("hags", T0.AddSeconds(60)));   // 3 -> trips
        Assert.True(b.IsTripped("hags", T0.AddSeconds(60)));
        Assert.Equal(3, b.ResetCount("hags"));
    }

    [Fact]
    public void Trip_ReturnsTrueExactlyOnce_NotEveryTickWhileTripped()
    {
        var b = new AutoApplyCircuitBreaker(tripThreshold: 2);
        Assert.False(b.RecordExternalReset("vrr", T0));
        Assert.True(b.RecordExternalReset("vrr", T0.AddSeconds(30)));   // trips
        // Subsequent resets while still in cooldown must NOT re-report a trip.
        Assert.False(b.RecordExternalReset("vrr", T0.AddSeconds(60)));
        Assert.False(b.RecordExternalReset("vrr", T0.AddSeconds(90)));
    }

    [Fact]
    public void Tripped_ExcludedUntilCooldownExpires()
    {
        var cooldown = TimeSpan.FromMinutes(15);
        var b = new AutoApplyCircuitBreaker(tripThreshold: 1, cooldown: cooldown);

        Assert.True(b.RecordExternalReset("hdr:1", T0));        // threshold 1 -> trips immediately
        Assert.True(b.IsTripped("hdr:1", T0));
        Assert.True(b.IsTripped("hdr:1", T0 + cooldown - TimeSpan.FromSeconds(1)));
        Assert.False(b.IsTripped("hdr:1", T0 + cooldown));      // cooldown elapsed -> eligible again
    }

    [Fact]
    public void AfterCooldown_OneMoreRevertRetrips()
    {
        // The cooldown runs from the trip time. Use threshold 1 so the trip time
        // is unambiguous (the first revert), then measure the window from there.
        var cooldown = TimeSpan.FromMinutes(15);
        var b = new AutoApplyCircuitBreaker(tripThreshold: 1, cooldown: cooldown);

        Assert.True(b.RecordExternalReset("svc", T0));   // trips at T0
        var afterCooldown = T0 + cooldown;
        Assert.False(b.IsTripped("svc", afterCooldown)); // window elapsed -> eligible

        // A single fresh revert after the cooldown re-trips -- so the worst case is
        // one interruption per cooldown, not one every poll.
        Assert.True(b.RecordExternalReset("svc", afterCooldown));
        Assert.True(b.IsTripped("svc", afterCooldown));
    }

    [Fact]
    public void RecordHealthy_ClearsStreakAndCooldown()
    {
        var b = new AutoApplyCircuitBreaker(tripThreshold: 2);
        b.RecordExternalReset("hags", T0);
        Assert.True(b.RecordExternalReset("hags", T0.AddSeconds(30)));  // trips
        Assert.True(b.IsTripped("hags", T0.AddSeconds(30)));

        b.RecordHealthy("hags");                                       // Windows stopped reverting
        Assert.False(b.IsTripped("hags", T0.AddSeconds(60)));
        Assert.Equal(0, b.ResetCount("hags"));

        // Streak restarts from zero -- it takes the full threshold again to trip.
        Assert.False(b.RecordExternalReset("hags", T0.AddSeconds(90)));
        Assert.False(b.IsTripped("hags", T0.AddSeconds(90)));
    }

    [Fact]
    public void TracksEachSettingIndependently()
    {
        var b = new AutoApplyCircuitBreaker(tripThreshold: 2);
        b.RecordExternalReset("hags", T0);
        Assert.True(b.RecordExternalReset("hags", T0.AddSeconds(30)));  // hags trips

        Assert.True(b.IsTripped("hags", T0.AddSeconds(30)));
        Assert.False(b.IsTripped("vrr", T0.AddSeconds(30)));           // vrr unaffected
        Assert.False(b.RecordExternalReset("vrr", T0.AddSeconds(30))); // vrr only 1 reset
    }

    [Fact]
    public void TrackedSettingIds_ReflectStreaksAndCooldowns()
    {
        var b = new AutoApplyCircuitBreaker(tripThreshold: 5);
        b.RecordExternalReset("hags", T0);
        b.RecordExternalReset("vrr", T0);
        Assert.Contains("hags", b.TrackedSettingIds);
        Assert.Contains("vrr", b.TrackedSettingIds);

        b.RecordHealthy("hags");
        Assert.DoesNotContain("hags", b.TrackedSettingIds);
        Assert.Contains("vrr", b.TrackedSettingIds);
    }

    [Fact]
    public void EmptySettingId_IsIgnored()
    {
        var b = new AutoApplyCircuitBreaker(tripThreshold: 1);
        Assert.False(b.RecordExternalReset("", T0));
        Assert.False(b.IsTripped("", T0));
    }
}
