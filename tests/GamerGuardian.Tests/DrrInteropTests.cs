using System;
using System.Collections.Generic;
using GamerGuardian.Native;
using static GamerGuardian.Native.DisplayConfig;
using Xunit;

namespace GamerGuardian.Tests;

public class DrrInteropTests : IDisposable
{
    // These tests substitute the native DRR support probe; reset it (and the
    // cache) around each test so static state never leaks between tests.
    private readonly Func<LUID, uint, bool> _originalProbe = DrrInterop.SupportProbe;

    public DrrInteropTests() => DrrInterop.ClearSupportCache();

    public void Dispose()
    {
        DrrInterop.SupportProbe = _originalProbe;
        DrrInterop.ClearSupportCache();
    }

    private static LUID Luid(uint low, int high = 0) => new() { LowPart = low, HighPart = high };

    [Fact]
    public void IsDrrEnabled_TrueWhenBoostFlagSet()
    {
        Assert.True(DrrInterop.IsDrrEnabled(DisplayConfig.DISPLAYCONFIG_PATH_BOOST_REFRESH_RATE));
        Assert.True(DrrInterop.IsDrrEnabled(0x10));
    }

    [Fact]
    public void IsDrrEnabled_FalseWhenBoostFlagClear()
    {
        Assert.False(DrrInterop.IsDrrEnabled(0x00));
        Assert.False(DrrInterop.IsDrrEnabled(0x08)); // a different path flag (virtual-mode support)
    }

    [Fact]
    public void IsDrrEnabled_IgnoresUnrelatedBits()
    {
        // Boost bit set alongside other flags -> still enabled.
        Assert.True(DrrInterop.IsDrrEnabled(0x10 | 0x08 | 0x01));
        // Only unrelated bits set -> not enabled.
        Assert.False(DrrInterop.IsDrrEnabled(0x08 | 0x01));
    }

    // ---- Support-probe caching (the input-stall fix) ----------------------

    [Fact]
    public void IsSupported_ProbesEachTargetOnce_NotEveryCall()
    {
        var calls = new Dictionary<(uint, int, uint), int>();
        DrrInterop.SupportProbe = (a, t) =>
        {
            var k = (a.LowPart, a.HighPart, t);
            calls[k] = calls.GetValueOrDefault(k) + 1;
            return true;
        };

        // Simulate the calls DrrMonitor.CheckDrift makes across many 30s polls.
        for (int poll = 0; poll < 10; poll++)
        {
            Assert.True(DrrInterop.IsSupported(Luid(1), 100));
            Assert.True(DrrInterop.IsSupported(Luid(1), 200));
        }

        // The disruptive SetDisplayConfig(SDC_VALIDATE) probe must run once per
        // target, not on every poll. Before the cache this was 10 -- the bug.
        Assert.Equal(1, calls[(1u, 0, 100u)]);
        Assert.Equal(1, calls[(1u, 0, 200u)]);
    }

    [Fact]
    public void IsSupported_CachesTheResultValue()
    {
        DrrInterop.SupportProbe = (_, _) => false;
        Assert.False(DrrInterop.IsSupported(Luid(7), 1));
        // A later probe that would say true must not override the cached value.
        DrrInterop.SupportProbe = (_, _) => true;
        Assert.False(DrrInterop.IsSupported(Luid(7), 1));
    }

    [Fact]
    public void ClearSupportCache_ForcesReprobe()
    {
        int calls = 0;
        DrrInterop.SupportProbe = (_, _) => { calls++; return true; };

        DrrInterop.IsSupported(Luid(5), 1);
        DrrInterop.IsSupported(Luid(5), 1);
        Assert.Equal(1, calls);

        DrrInterop.ClearSupportCache(); // e.g. a monitor hot-plug
        DrrInterop.IsSupported(Luid(5), 1);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void DifferentTargetsAndAdapters_ProbedIndependently()
    {
        int calls = 0;
        DrrInterop.SupportProbe = (_, _) => { calls++; return true; };

        DrrInterop.IsSupported(Luid(1, 0), 1);
        DrrInterop.IsSupported(Luid(1, 0), 2); // same adapter, different target
        DrrInterop.IsSupported(Luid(2, 0), 1); // different adapter low part
        DrrInterop.IsSupported(Luid(1, 9), 1); // different adapter high part
        Assert.Equal(4, calls);
    }
}
