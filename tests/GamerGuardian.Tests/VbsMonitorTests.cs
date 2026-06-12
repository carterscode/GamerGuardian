using GamerGuardian.Models;
using GamerGuardian.Monitors;
using Xunit;

namespace GamerGuardian.Tests;

/// <summary>
/// Pure-logic tests over <see cref="VbsMonitor.VbsSnapshot"/> — no live registry
/// access, so they run headlessly on CI. The snapshot/ops split exists exactly so
/// the complete-disable contract is testable.
/// </summary>
public class VbsMonitorTests
{
    private static VbsMonitor.VbsSnapshot Snapshot(
        int? evbs = null, int? rpsf = null, int? mandatory = null, int? rootHvci = null,
        int? rootLocked = null,
        Dictionary<string, VbsMonitor.ScenarioState>? scenarios = null,
        int? lsaCfgFlags = null,
        int? policyEvbs = null, int? policyLsaCfgFlags = null, int? policyHvci = null) =>
        new(evbs, rpsf, mandatory, rootHvci, rootLocked,
            scenarios ?? new Dictionary<string, VbsMonitor.ScenarioState>(StringComparer.OrdinalIgnoreCase),
            lsaCfgFlags, policyEvbs, policyLsaCfgFlags, policyHvci);

    private static VbsMonitor.ScenarioState Scenario(int? enabled, int? locked = null, params string[] meta) =>
        new(enabled, locked, meta);

    /// <summary>The state our own disable produces: every value explicitly 0,
    /// every known scenario present with Enabled=0, no re-enable metadata.</summary>
    private static VbsMonitor.VbsSnapshot FullyDisabledSnapshot()
    {
        var scenarios = VbsMonitor.KnownScenarios.ToDictionary(
            n => n, _ => Scenario(0), StringComparer.OrdinalIgnoreCase);
        return Snapshot(
            evbs: 0, rpsf: 0, mandatory: 0, rootHvci: 0,
            scenarios: scenarios, lsaCfgFlags: 0,
            policyEvbs: 0, policyLsaCfgFlags: 0, policyHvci: 0);
    }

    // ---- Compliance: fully disabled -----------------------------------------

    [Fact]
    public void IsFullyDisabled_TrueForCompleteDisableState()
    {
        Assert.True(VbsMonitor.IsFullyDisabled(FullyDisabledSnapshot()));
    }

    [Fact]
    public void IsFullyDisabled_FalseOnFreshDefaultMachine_AbsentKeysAreNotDisabled()
    {
        // Windows 11 enables VBS by default with NO registry values present —
        // absence must never read as "disabled".
        Assert.False(VbsMonitor.IsFullyDisabled(Snapshot()));
    }

    [Fact]
    public void IsFullyDisabled_FalseWhenMasterSwitchZeroButScenarioStillEnabled()
    {
        // The 24H2 trap: EnableVirtualizationBasedSecurity=0 but an enabled
        // scenario (e.g. WindowsHello) keeps VBS alive.
        var s = FullyDisabledSnapshot();
        var scenarios = new Dictionary<string, VbsMonitor.ScenarioState>(s.Scenarios, StringComparer.OrdinalIgnoreCase)
        {
            ["WindowsHello"] = Scenario(1),
        };
        Assert.False(VbsMonitor.IsFullyDisabled(s with { Scenarios = scenarios }));
    }

    [Fact]
    public void IsFullyDisabled_FalseWhenUnknownFutureScenarioEnabled()
    {
        var s = FullyDisabledSnapshot();
        var scenarios = new Dictionary<string, VbsMonitor.ScenarioState>(s.Scenarios, StringComparer.OrdinalIgnoreCase)
        {
            ["HypervisorEnforcedPagingTranslation"] = Scenario(1),
        };
        Assert.False(VbsMonitor.IsFullyDisabled(s with { Scenarios = scenarios }));
    }

    [Fact]
    public void IsFullyDisabled_FalseWhenUpgradeReenableMetadataPresent()
    {
        // WasEnabledBy/EnabledBootId let Windows restore HVCI after an upgrade —
        // the disable contract requires them gone.
        var s = FullyDisabledSnapshot();
        var scenarios = new Dictionary<string, VbsMonitor.ScenarioState>(s.Scenarios, StringComparer.OrdinalIgnoreCase)
        {
            [VbsMonitor.HvciScenario] = Scenario(0, null, "WasEnabledBy"),
        };
        Assert.False(VbsMonitor.IsFullyDisabled(s with { Scenarios = scenarios }));
    }

    [Theory]
    [InlineData("lsa")]      // Credential Guard via Lsa
    [InlineData("policy")]   // GPO mirror re-enabled by gpupdate
    public void IsFullyDisabled_FalseWhenCredentialGuardPathReenabled(string vector)
    {
        var s = FullyDisabledSnapshot();
        s = vector == "lsa" ? s with { LsaCfgFlags = 2 } : s with { PolicyLsaCfgFlags = 2 };
        Assert.False(VbsMonitor.IsFullyDisabled(s));
    }

    // ---- Compliance: restore-default ----------------------------------------

    [Fact]
    public void HasNoDisableMarkers_TrueOnFreshDefaultMachine()
    {
        // A machine running VBS by Windows default (no keys at all) must NOT
        // drift for a user whose preference is Enabled.
        Assert.True(VbsMonitor.HasNoDisableMarkers(Snapshot()));
    }

    [Fact]
    public void HasNoDisableMarkers_FalseAfterOurDisable()
    {
        Assert.False(VbsMonitor.HasNoDisableMarkers(FullyDisabledSnapshot()));
    }

    [Fact]
    public void HasNoDisableMarkers_IgnoresHvciScenario_MemoryIntegrityOwnsIt()
    {
        // Standalone Memory-Integrity-off (the existing memintegrity toggle) must
        // not count as a VBS disable marker, or the two monitors would fight.
        var scenarios = new Dictionary<string, VbsMonitor.ScenarioState>(StringComparer.OrdinalIgnoreCase)
        {
            [VbsMonitor.HvciScenario] = Scenario(0),
        };
        Assert.True(VbsMonitor.HasNoDisableMarkers(Snapshot(evbs: 1, scenarios: scenarios)));
    }

    // ---- UEFI lock detection -------------------------------------------------

    [Theory]
    [InlineData("root")]
    [InlineData("scenario")]
    [InlineData("lsaCfgFlags1")]
    public void UefiLockDetected_AllLockShapes(string shape)
    {
        var s = shape switch
        {
            "root" => Snapshot(evbs: 1, rootLocked: 1),
            "scenario" => Snapshot(evbs: 1, scenarios: new Dictionary<string, VbsMonitor.ScenarioState>(StringComparer.OrdinalIgnoreCase)
            {
                [VbsMonitor.HvciScenario] = Scenario(1, locked: 1),
            }),
            _ => Snapshot(evbs: 1, lsaCfgFlags: 1), // 1 = Credential Guard WITH UEFI lock
        };
        Assert.True(VbsMonitor.UefiLockDetected(s));
    }

    [Fact]
    public void UefiLockDetected_FalseWithoutLock_LsaCfgFlags2IsNotALock()
    {
        Assert.False(VbsMonitor.UefiLockDetected(Snapshot(evbs: 1, lsaCfgFlags: 2)));
    }

    // ---- Disable ops ----------------------------------------------------------

    [Fact]
    public void BuildDisableOps_FreshDefaultMachine_WritesEveryExplicitZero()
    {
        var (adds, deletes) = VbsMonitor.BuildDisableOps(Snapshot());

        Assert.Empty(deletes);
        // Root x4 + 5 known scenarios + LsaCfgFlags + policy x3.
        Assert.Equal(13, adds.Count);
        Assert.All(adds, a => Assert.Equal("0", a.data));
        Assert.Contains(adds, a => a.subkey == VbsMonitor.RootKey && a.name == "EnableVirtualizationBasedSecurity");
        foreach (var scenario in VbsMonitor.KnownScenarios)
            Assert.Contains(adds, a => a.subkey == $@"{VbsMonitor.ScenariosKey}\{scenario}" && a.name == "Enabled");
        Assert.Contains(adds, a => a.subkey == VbsMonitor.LsaKey && a.name == "LsaCfgFlags");
        Assert.Contains(adds, a => a.subkey == VbsMonitor.PolicyKey && a.name == "LsaCfgFlags");
    }

    [Fact]
    public void BuildDisableOps_IsADiff_SkipsValuesAlreadyZero()
    {
        var (adds, deletes) = VbsMonitor.BuildDisableOps(FullyDisabledSnapshot());
        Assert.Empty(adds);
        Assert.Empty(deletes);
    }

    [Fact]
    public void BuildDisableOps_DeletesReenableMetadataOnlyWherePresent()
    {
        var scenarios = new Dictionary<string, VbsMonitor.ScenarioState>(StringComparer.OrdinalIgnoreCase)
        {
            [VbsMonitor.HvciScenario] = Scenario(1, null, "WasEnabledBy", "EnabledBootId"),
            ["KernelShadowStacks"] = Scenario(1, null, "WasEnabledBy"),
            ["CredentialGuard"] = Scenario(1),
        };
        var (_, deletes) = VbsMonitor.BuildDisableOps(Snapshot(evbs: 1, scenarios: scenarios));

        Assert.Equal(3, deletes.Count);
        Assert.Contains(($@"{VbsMonitor.ScenariosKey}\{VbsMonitor.HvciScenario}", "WasEnabledBy"), deletes);
        Assert.Contains(($@"{VbsMonitor.ScenariosKey}\{VbsMonitor.HvciScenario}", "EnabledBootId"), deletes);
        Assert.Contains(($@"{VbsMonitor.ScenariosKey}\KernelShadowStacks", "WasEnabledBy"), deletes);
    }

    [Fact]
    public void BuildDisableOps_ZeroesDiscoveredUnknownScenarios()
    {
        var scenarios = new Dictionary<string, VbsMonitor.ScenarioState>(StringComparer.OrdinalIgnoreCase)
        {
            ["HypervisorEnforcedPagingTranslation"] = Scenario(1),
        };
        var (adds, _) = VbsMonitor.BuildDisableOps(Snapshot(scenarios: scenarios));
        Assert.Contains(adds, a =>
            a.subkey == $@"{VbsMonitor.ScenariosKey}\HypervisorEnforcedPagingTranslation" && a.name == "Enabled" && a.data == "0");
    }

    // ---- Enable (restore default) ops -----------------------------------------

    [Fact]
    public void BuildEnableOps_RestoresMasterSwitchAndHvciAndRemovesOurZeros()
    {
        var (adds, deletes) = VbsMonitor.BuildEnableOps(FullyDisabledSnapshot(), skipHvci: false);

        Assert.Contains(adds, a => a.subkey == VbsMonitor.RootKey && a.name == "EnableVirtualizationBasedSecurity" && a.data == "1");
        Assert.Contains(adds, a => a.subkey == $@"{VbsMonitor.ScenariosKey}\{VbsMonitor.HvciScenario}" && a.name == "Enabled" && a.data == "1");
        // WasEnabledBy=2 = "enabled by user": keeps the Windows Security toggle un-greyed.
        Assert.Contains(adds, a => a.name == "WasEnabledBy" && a.data == "2");

        Assert.Contains((VbsMonitor.LsaKey, "LsaCfgFlags"), deletes);
        Assert.Contains((VbsMonitor.PolicyKey, "EnableVirtualizationBasedSecurity"), deletes);
        Assert.Contains((VbsMonitor.PolicyKey, "LsaCfgFlags"), deletes);
        Assert.Contains((VbsMonitor.PolicyKey, "HypervisorEnforcedCodeIntegrity"), deletes);
        Assert.Contains(($@"{VbsMonitor.ScenariosKey}\CredentialGuard", "Enabled"), deletes);
    }

    [Fact]
    public void BuildEnableOps_SkipHvci_LeavesMemoryIntegrityChoiceAlone()
    {
        var (adds, _) = VbsMonitor.BuildEnableOps(FullyDisabledSnapshot(), skipHvci: true);
        Assert.DoesNotContain(adds, a => a.subkey == $@"{VbsMonitor.ScenariosKey}\{VbsMonitor.HvciScenario}");
    }

    [Fact]
    public void BuildEnableOps_NeverFightsARealGpo()
    {
        // Policy values of 1/2 come from a real domain GPO — restore-default must
        // not delete them (gpupdate would rewrite them anyway).
        var s = Snapshot(evbs: 1, policyEvbs: 1, policyLsaCfgFlags: 2, lsaCfgFlags: 2);
        var (_, deletes) = VbsMonitor.BuildEnableOps(s, skipHvci: false);
        Assert.Empty(deletes);
    }

    // ---- Classification / safety ----------------------------------------------

    [Fact]
    public void Classify_ThreeStates()
    {
        Assert.Equal("Off", VbsMonitor.Classify(FullyDisabledSnapshot()));
        Assert.Equal("On", VbsMonitor.Classify(Snapshot()));
        Assert.Equal("Partially off", VbsMonitor.Classify(Snapshot(evbs: 0)));
    }

    [Theory]
    [InlineData("HypervisorEnforcedCodeIntegrity", true)]
    [InlineData("KernelShadowStacks", true)]
    [InlineData("Has Space And-Dash_Underscore9", true)]
    [InlineData("Evil&&calc", false)]
    [InlineData("Pipe|name", false)]
    [InlineData("", false)]
    public void IsSafeKeyName_MatchesElevatedRegistryGuardExpectations(string name, bool ok)
    {
        Assert.Equal(ok, VbsMonitor.IsSafeKeyName(name));
    }

    [Fact]
    public void Summarize_ReportsAbsentValuesExplicitly()
    {
        var text = VbsMonitor.Summarize(Snapshot(evbs: 0, lsaCfgFlags: 2));
        Assert.Contains("EVBS=0", text);
        Assert.Contains("LsaCfgFlags=2", text);
        Assert.Contains("RPSF=absent", text);
    }

    // ---- Monitor interplay ------------------------------------------------------

    [Fact]
    public void MemoryIntegrityMonitor_DefersWhileVbsMonitoredOff()
    {
        // Regardless of live registry state, memintegrity must yield nothing while
        // the VBS monitor owns the HVCI key — otherwise the two fight every poll.
        var cfg = new AppConfig();
        cfg.Global.Vbs.Monitor = true;
        cfg.Global.Vbs.DesiredOn = false;
        cfg.Global.MemoryIntegrity.Monitor = true;
        cfg.Global.MemoryIntegrity.DesiredOn = true;

        Assert.Empty(new MemoryIntegrityMonitor().CheckDrift(cfg));
    }

    [Fact]
    public void VbsPref_RoundTripsThroughConfigJson()
    {
        var cfg = new AppConfig();
        cfg.Global.Vbs.Monitor = true;
        cfg.Global.Vbs.DesiredOn = false;
        cfg.Global.Vbs.AutoApply = true;

        var clone = Services.AppConfigCloner.Clone(cfg);

        Assert.True(clone.Global.Vbs.Monitor);
        Assert.False(clone.Global.Vbs.DesiredOn);
        Assert.True(clone.Global.Vbs.AutoApply);
    }
}
