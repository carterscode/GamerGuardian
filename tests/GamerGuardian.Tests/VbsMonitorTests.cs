using GamerGuardian.Models;
using GamerGuardian.Monitors;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

/// <summary>
/// Pure-logic tests over <see cref="VbsMonitor.VbsSnapshot"/> — no live registry
/// access, so they run headlessly on CI. The snapshot/ops split exists exactly so
/// the complete-disable contract is testable, including per-field mutation
/// coverage (every individual comparison in the compliance predicates is pinned)
/// and op-replay simulations proving both directions converge.
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

    private static Dictionary<string, VbsMonitor.ScenarioState> WithScenario(
        VbsMonitor.VbsSnapshot s, string name, VbsMonitor.ScenarioState state)
    {
        var d = new Dictionary<string, VbsMonitor.ScenarioState>(s.Scenarios, StringComparer.OrdinalIgnoreCase)
        {
            [name] = state,
        };
        return d;
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

    // Every scalar field check in IsFullyDisabled is individually load-bearing:
    // flipping any single one to 1 (re-enabled) or to null (absent) must break
    // compliance. A refactor that drops one comparison fails here.
    [Theory]
    [InlineData("Evbs", 1)]
    [InlineData("Evbs", null)]
    [InlineData("Rpsf", 1)]
    [InlineData("Rpsf", null)]
    [InlineData("Mandatory", 1)]
    [InlineData("Mandatory", null)]
    [InlineData("RootHvci", 1)]
    [InlineData("RootHvci", null)]
    [InlineData("LsaCfgFlags", 2)]
    [InlineData("LsaCfgFlags", null)]
    [InlineData("PolicyEvbs", 1)]
    [InlineData("PolicyEvbs", null)]
    [InlineData("PolicyLsaCfgFlags", 2)]
    [InlineData("PolicyLsaCfgFlags", null)]
    [InlineData("PolicyHvci", 1)]
    [InlineData("PolicyHvci", null)]
    public void IsFullyDisabled_EverySingleFieldIsLoadBearing(string field, int? value)
    {
        var s = FullyDisabledSnapshot();
        s = field switch
        {
            "Evbs" => s with { Evbs = value },
            "Rpsf" => s with { RequirePlatformSecurityFeatures = value },
            "Mandatory" => s with { Mandatory = value },
            "RootHvci" => s with { RootHvci = value },
            "LsaCfgFlags" => s with { LsaCfgFlags = value },
            "PolicyEvbs" => s with { PolicyEvbs = value },
            "PolicyLsaCfgFlags" => s with { PolicyLsaCfgFlags = value },
            "PolicyHvci" => s with { PolicyHvci = value },
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };
        Assert.False(VbsMonitor.IsFullyDisabled(s));
    }

    [Theory]
    [InlineData(1)]      // re-enabled
    [InlineData(null)]   // value vanished (or wrong-typed) — still not explicit 0
    public void IsFullyDisabled_EveryKnownScenarioEnabledIsLoadBearing(int? enabled)
    {
        foreach (var name in VbsMonitor.KnownScenarios)
        {
            var s = FullyDisabledSnapshot();
            s = s with { Scenarios = WithScenario(s, name, Scenario(enabled)) };
            Assert.False(VbsMonitor.IsFullyDisabled(s));
        }
    }

    [Fact]
    public void IsFullyDisabled_FalseWhenUnknownFutureScenarioEnabled()
    {
        var s = FullyDisabledSnapshot();
        s = s with { Scenarios = WithScenario(s, "HypervisorEnforcedPagingTranslation", Scenario(1)) };
        Assert.False(VbsMonitor.IsFullyDisabled(s));
    }

    [Fact]
    public void IsFullyDisabled_ToleratesUnknownScenarioWithoutEnabledValue()
    {
        // A subkey carrying only metadata/Locked must not block compliance.
        var s = FullyDisabledSnapshot();
        s = s with { Scenarios = WithScenario(s, "SomeFutureScenario", Scenario(null)) };
        Assert.True(VbsMonitor.IsFullyDisabled(s));
    }

    [Theory]
    [InlineData("WasEnabledBy")]
    [InlineData("EnabledBootId")]
    public void IsFullyDisabled_FalseWhenUpgradeReenableMetadataPresent(string meta)
    {
        // WasEnabledBy/EnabledBootId let Windows restore HVCI after an upgrade —
        // the disable contract requires them gone.
        var s = FullyDisabledSnapshot();
        s = s with { Scenarios = WithScenario(s, VbsMonitor.HvciScenario, Scenario(0, null, meta)) };
        Assert.False(VbsMonitor.IsFullyDisabled(s));
    }

    [Fact]
    public void IsFullyDisabled_IgnoresChangedInBootCycle_ButDisableOpsStillDeleteIt()
    {
        // ChangedInBootCycle is boot-cycle bookkeeping Windows may rewrite on its
        // own; treating it as drift would mean a corrective apply every boot. It
        // is cleaned up whenever an apply does run.
        var s = FullyDisabledSnapshot();
        s = s with { Scenarios = WithScenario(s, VbsMonitor.HvciScenario, Scenario(0, null, "ChangedInBootCycle")) };

        Assert.True(VbsMonitor.IsFullyDisabled(s));
        var (_, deletes) = VbsMonitor.BuildDisableOps(s);
        Assert.Contains(($@"{VbsMonitor.ScenariosKey}\{VbsMonitor.HvciScenario}", "ChangedInBootCycle"), deletes);
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

    // Each marker check is individually load-bearing: starting from a fresh
    // machine, setting any single disable marker must break the predicate.
    [Theory]
    [InlineData("Evbs")]
    [InlineData("PolicyEvbs")]
    [InlineData("PolicyHvci")]
    [InlineData("LsaCfgFlags")]
    [InlineData("PolicyLsaCfgFlags")]
    [InlineData("NonHvciScenario")]
    public void HasNoDisableMarkers_EverySingleMarkerIsLoadBearing(string marker)
    {
        var s = marker switch
        {
            "Evbs" => Snapshot(evbs: 0),
            "PolicyEvbs" => Snapshot(policyEvbs: 0),
            "PolicyHvci" => Snapshot(policyHvci: 0),
            "LsaCfgFlags" => Snapshot(lsaCfgFlags: 0),
            "PolicyLsaCfgFlags" => Snapshot(policyLsaCfgFlags: 0),
            _ => Snapshot(scenarios: new Dictionary<string, VbsMonitor.ScenarioState>(StringComparer.OrdinalIgnoreCase)
            {
                ["CredentialGuard"] = Scenario(0),
            }),
        };
        Assert.False(VbsMonitor.HasNoDisableMarkers(s));
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

    [Fact]
    public void HasNoDisableMarkers_ScenarioWithoutEnabledValueIsNotAMarker()
    {
        var scenarios = new Dictionary<string, VbsMonitor.ScenarioState>(StringComparer.OrdinalIgnoreCase)
        {
            ["CredentialGuard"] = Scenario(null, locked: 0),
        };
        Assert.True(VbsMonitor.HasNoDisableMarkers(Snapshot(scenarios: scenarios)));
    }

    // ---- The drift predicate (monitored vs unmonitored) -----------------------

    [Fact]
    public void IsCompliant_UnmonitoredEnabled_RoundTripsWithReadCurrent()
    {
        // The SyncIfUnmonitored invariant: for ANY machine state, syncing
        // DesiredOn from the ReadCurrent boolean must leave the unmonitored row
        // compliant — otherwise a "Partially off" machine (EVBS=0 from another
        // tool) gets silently "restored" by an unrelated Apply.
        var states = new[]
        {
            Snapshot(),                       // fresh default
            FullyDisabledSnapshot(),          // our complete disable
            Snapshot(evbs: 0),                // partial: master switch only
            Snapshot(lsaCfgFlags: 0),         // partial: Credential Guard GPO tattoo
            Snapshot(policyHvci: 0),          // partial: policy mirror remnant
            Snapshot(evbs: 1, scenarios: new Dictionary<string, VbsMonitor.ScenarioState>(StringComparer.OrdinalIgnoreCase)
            {
                ["CredentialGuard"] = Scenario(0),
            }),
        };
        foreach (var s in states)
        {
            bool syncedDesiredOn = !VbsMonitor.IsFullyDisabled(s); // == ReadCurrent()
            Assert.True(VbsMonitor.IsCompliant(s, syncedDesiredOn, monitored: false));
        }
    }

    [Fact]
    public void IsCompliant_MonitoredEnabled_EnforcesStrictRestoreContract()
    {
        Assert.False(VbsMonitor.IsCompliant(Snapshot(evbs: 0), desiredOn: true, monitored: true));
        Assert.True(VbsMonitor.IsCompliant(Snapshot(), desiredOn: true, monitored: true));
    }

    [Fact]
    public void IsCompliant_DesiredOff_RequiresFullDisableRegardlessOfMonitor()
    {
        Assert.False(VbsMonitor.IsCompliant(Snapshot(evbs: 0), desiredOn: false, monitored: false));
        Assert.False(VbsMonitor.IsCompliant(Snapshot(evbs: 0), desiredOn: false, monitored: true));
        Assert.True(VbsMonitor.IsCompliant(FullyDisabledSnapshot(), desiredOn: false, monitored: false));
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
    public void UefiLockDetected_FalseForExplicitZeroLocks_AndLsaCfgFlags2()
    {
        // Locked=0 written-then-unlocked is a real state; LsaCfgFlags=2 means
        // Credential Guard WITHOUT a UEFI lock.
        var s = Snapshot(evbs: 1, lsaCfgFlags: 2, rootLocked: 0,
            scenarios: new Dictionary<string, VbsMonitor.ScenarioState>(StringComparer.OrdinalIgnoreCase)
            {
                [VbsMonitor.HvciScenario] = Scenario(1, locked: 0),
            });
        Assert.False(VbsMonitor.UefiLockDetected(s));
    }

    // ---- Disable ops ----------------------------------------------------------

    [Fact]
    public void BuildDisableOps_FreshDefaultMachine_WritesEveryExplicitZero()
    {
        var (adds, deletes) = VbsMonitor.BuildDisableOps(Snapshot());

        Assert.Empty(deletes);
        // Root x4 + every known scenario + LsaCfgFlags + policy x3.
        Assert.Equal(8 + VbsMonitor.KnownScenarios.Length, adds.Count);
        Assert.All(adds, a => Assert.Equal("0", a.data));
        Assert.All(adds, a => Assert.Equal("REG_DWORD", a.kind));
        Assert.Contains(adds, a => a.subkey == VbsMonitor.RootKey && a.name == "EnableVirtualizationBasedSecurity");
        Assert.Contains(adds, a => a.subkey == VbsMonitor.RootKey && a.name == "RequirePlatformSecurityFeatures");
        Assert.Contains(adds, a => a.subkey == VbsMonitor.RootKey && a.name == "Mandatory");
        Assert.Contains(adds, a => a.subkey == VbsMonitor.RootKey && a.name == "HypervisorEnforcedCodeIntegrity");
        foreach (var scenario in VbsMonitor.KnownScenarios)
            Assert.Contains(adds, a => a.subkey == $@"{VbsMonitor.ScenariosKey}\{scenario}" && a.name == "Enabled");
        Assert.Contains(adds, a => a.subkey == VbsMonitor.LsaKey && a.name == "LsaCfgFlags");
        Assert.Contains(adds, a => a.subkey == VbsMonitor.PolicyKey && a.name == "EnableVirtualizationBasedSecurity");
        Assert.Contains(adds, a => a.subkey == VbsMonitor.PolicyKey && a.name == "LsaCfgFlags");
        Assert.Contains(adds, a => a.subkey == VbsMonitor.PolicyKey && a.name == "HypervisorEnforcedCodeIntegrity");
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
    public void BuildEnableOps_FullyPinned_RestoresAndRemovesEverything()
    {
        var (adds, deletes) = VbsMonitor.BuildEnableOps(FullyDisabledSnapshot(), skipHvci: false);

        // Adds: master switch + HVCI Enabled + WasEnabledBy ("enabled by user",
        // keeps the Windows Security toggle un-greyed).
        Assert.Equal(3, adds.Count);
        Assert.All(adds, a => Assert.Equal("REG_DWORD", a.kind));
        Assert.Contains(adds, a => a.subkey == VbsMonitor.RootKey && a.name == "EnableVirtualizationBasedSecurity" && a.data == "1");
        Assert.Contains(adds, a => a.subkey == $@"{VbsMonitor.ScenariosKey}\{VbsMonitor.HvciScenario}" && a.name == "Enabled" && a.data == "1");
        Assert.Contains(adds, a => a.name == "WasEnabledBy" && a.data == "2");

        // Deletes: policy x3 + Lsa + root x3 + every non-HVCI known scenario.
        Assert.Equal(7 + VbsMonitor.KnownScenarios.Length - 1, deletes.Count);
        Assert.Contains((VbsMonitor.PolicyKey, "EnableVirtualizationBasedSecurity"), deletes);
        Assert.Contains((VbsMonitor.PolicyKey, "LsaCfgFlags"), deletes);
        Assert.Contains((VbsMonitor.PolicyKey, "HypervisorEnforcedCodeIntegrity"), deletes);
        Assert.Contains((VbsMonitor.LsaKey, "LsaCfgFlags"), deletes);
        Assert.Contains((VbsMonitor.RootKey, "RequirePlatformSecurityFeatures"), deletes);
        Assert.Contains((VbsMonitor.RootKey, "Mandatory"), deletes);
        Assert.Contains((VbsMonitor.RootKey, "HypervisorEnforcedCodeIntegrity"), deletes);
        foreach (var scenario in VbsMonitor.KnownScenarios.Where(s => s != VbsMonitor.HvciScenario))
            Assert.Contains(($@"{VbsMonitor.ScenariosKey}\{scenario}", "Enabled"), deletes);
    }

    [Fact]
    public void BuildEnableOps_PolicyUnGreyDeletesComeFirst()
    {
        // The policy-mirror zeros grey out Windows Security and block every other
        // re-enable path — they must be the least likely deletes to be skipped if
        // anything goes wrong mid-batch.
        var (_, deletes) = VbsMonitor.BuildEnableOps(FullyDisabledSnapshot(), skipHvci: false);
        Assert.Equal(VbsMonitor.PolicyKey, deletes[0].subkey);
        Assert.Equal(VbsMonitor.PolicyKey, deletes[1].subkey);
        Assert.Equal(VbsMonitor.PolicyKey, deletes[2].subkey);
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

    [Fact]
    public void BuildEnableOps_ScenarioWithoutEnabledValue_EmitsNoDelete()
    {
        var scenarios = new Dictionary<string, VbsMonitor.ScenarioState>(StringComparer.OrdinalIgnoreCase)
        {
            ["CredentialGuard"] = Scenario(null),
        };
        var (_, deletes) = VbsMonitor.BuildEnableOps(Snapshot(evbs: 1, scenarios: scenarios), skipHvci: false);
        Assert.Empty(deletes);
    }

    // ---- Op-replay simulations: both directions converge -----------------------

    /// <summary>Replays adds/deletes onto a snapshot the way reg.exe would mutate
    /// the registry, so convergence is provable without touching the real one.</summary>
    private static VbsMonitor.VbsSnapshot ApplyOps(
        VbsMonitor.VbsSnapshot s,
        List<(string subkey, string name, string kind, string data)> adds,
        List<(string subkey, string name)> deletes)
    {
        var result = s;
        var scenarios = new Dictionary<string, VbsMonitor.ScenarioState>(s.Scenarios, StringComparer.OrdinalIgnoreCase);

        VbsMonitor.ScenarioState Get(string name) =>
            scenarios.TryGetValue(name, out var sc) ? sc : VbsMonitor.ScenarioState.Absent;

        foreach (var (subkey, name, _, data) in adds)
        {
            int v = int.Parse(data);
            if (subkey == VbsMonitor.RootKey)
            {
                result = name switch
                {
                    "EnableVirtualizationBasedSecurity" => result with { Evbs = v },
                    "RequirePlatformSecurityFeatures" => result with { RequirePlatformSecurityFeatures = v },
                    "Mandatory" => result with { Mandatory = v },
                    "HypervisorEnforcedCodeIntegrity" => result with { RootHvci = v },
                    _ => result,
                };
            }
            else if (subkey == VbsMonitor.LsaKey && name == "LsaCfgFlags") result = result with { LsaCfgFlags = v };
            else if (subkey == VbsMonitor.PolicyKey)
            {
                result = name switch
                {
                    "EnableVirtualizationBasedSecurity" => result with { PolicyEvbs = v },
                    "LsaCfgFlags" => result with { PolicyLsaCfgFlags = v },
                    "HypervisorEnforcedCodeIntegrity" => result with { PolicyHvci = v },
                    _ => result,
                };
            }
            else if (subkey.StartsWith(VbsMonitor.ScenariosKey + @"\", StringComparison.OrdinalIgnoreCase))
            {
                var scenario = subkey[(VbsMonitor.ScenariosKey.Length + 1)..];
                var sc = Get(scenario);
                scenarios[scenario] = name == "Enabled"
                    ? sc with { Enabled = v }
                    : sc with { MetaValuesPresent = sc.MetaValuesPresent.Append(name).Distinct().ToArray() };
            }
        }

        foreach (var (subkey, name) in deletes)
        {
            if (subkey == VbsMonitor.LsaKey && name == "LsaCfgFlags") result = result with { LsaCfgFlags = null };
            else if (subkey == VbsMonitor.RootKey)
            {
                result = name switch
                {
                    "RequirePlatformSecurityFeatures" => result with { RequirePlatformSecurityFeatures = null },
                    "Mandatory" => result with { Mandatory = null },
                    "HypervisorEnforcedCodeIntegrity" => result with { RootHvci = null },
                    _ => result,
                };
            }
            else if (subkey == VbsMonitor.PolicyKey)
            {
                result = name switch
                {
                    "EnableVirtualizationBasedSecurity" => result with { PolicyEvbs = null },
                    "LsaCfgFlags" => result with { PolicyLsaCfgFlags = null },
                    "HypervisorEnforcedCodeIntegrity" => result with { PolicyHvci = null },
                    _ => result,
                };
            }
            else if (subkey.StartsWith(VbsMonitor.ScenariosKey + @"\", StringComparison.OrdinalIgnoreCase))
            {
                var scenario = subkey[(VbsMonitor.ScenariosKey.Length + 1)..];
                var sc = Get(scenario);
                scenarios[scenario] = name == "Enabled"
                    ? sc with { Enabled = null }
                    : sc with { MetaValuesPresent = sc.MetaValuesPresent.Where(m => m != name).ToArray() };
            }
        }

        return result with { Scenarios = scenarios };
    }

    [Fact]
    public void DisableOps_AppliedToAnyStartingState_ProduceFullyDisabled()
    {
        var startingStates = new[]
        {
            Snapshot(),                                  // fresh default machine
            Snapshot(evbs: 1, rpsf: 3, lsaCfgFlags: 2),  // everything on
            Snapshot(evbs: 0),                           // partial
            Snapshot(evbs: 1, scenarios: new Dictionary<string, VbsMonitor.ScenarioState>(StringComparer.OrdinalIgnoreCase)
            {
                [VbsMonitor.HvciScenario] = Scenario(1, null, "WasEnabledBy", "EnabledBootId"),
                ["WindowsHello"] = Scenario(1),
                ["FutureScenario"] = Scenario(1),
            }),
        };
        foreach (var start in startingStates)
        {
            var (adds, deletes) = VbsMonitor.BuildDisableOps(start);
            var after = ApplyOps(start, adds, deletes);
            Assert.True(VbsMonitor.IsFullyDisabled(after));
            // And applying again is a no-op (idempotent diff).
            var (adds2, deletes2) = VbsMonitor.BuildDisableOps(after);
            Assert.Empty(adds2);
            Assert.Empty(deletes2);
        }
    }

    [Fact]
    public void EnableOps_AppliedToFullyDisabled_ProduceNoDisableMarkers()
    {
        var start = FullyDisabledSnapshot();
        var (adds, deletes) = VbsMonitor.BuildEnableOps(start, skipHvci: false);
        var after = ApplyOps(start, adds, deletes);

        Assert.True(VbsMonitor.HasNoDisableMarkers(after));
        // Idempotent: a second restore finds nothing to do.
        var (adds2, deletes2) = VbsMonitor.BuildEnableOps(after, skipHvci: false);
        Assert.Empty(adds2);
        Assert.Empty(deletes2);
    }

    // ---- HVCI unblock (MemoryIntegrityMonitor interplay) ------------------------

    [Theory]
    [InlineData("evbs")]
    [InlineData("policyEvbs")]
    [InlineData("policyHvci")]
    public void HvciBlocked_EachMarkerBlocks(string marker)
    {
        var s = marker switch
        {
            "evbs" => Snapshot(evbs: 0),
            "policyEvbs" => Snapshot(policyEvbs: 0),
            _ => Snapshot(policyHvci: 0),
        };
        Assert.True(VbsMonitor.HvciBlocked(s));
    }

    [Fact]
    public void HvciBlocked_FalseOnFreshOrEnabledMachine()
    {
        Assert.False(VbsMonitor.HvciBlocked(Snapshot()));
        Assert.False(VbsMonitor.HvciBlocked(Snapshot(evbs: 1)));
    }

    [Fact]
    public void BuildHvciUnblockOps_ClearsExactlyTheBlockers_NothingElse()
    {
        var s = Snapshot(evbs: 0, policyEvbs: 0, policyHvci: 0, lsaCfgFlags: 0, policyLsaCfgFlags: 0);
        var (adds, deletes) = VbsMonitor.BuildHvciUnblockOps(s);

        Assert.Single(adds);
        Assert.Equal((VbsMonitor.RootKey, "EnableVirtualizationBasedSecurity", "REG_DWORD", "1"), adds[0]);
        Assert.Equal(2, deletes.Count);
        Assert.Contains((VbsMonitor.PolicyKey, "EnableVirtualizationBasedSecurity"), deletes);
        Assert.Contains((VbsMonitor.PolicyKey, "HypervisorEnforcedCodeIntegrity"), deletes);
        // Credential Guard is NOT Memory Integrity's business.
        Assert.DoesNotContain(deletes, d => d.name == "LsaCfgFlags");

        var after = ApplyOps(s, adds, deletes);
        Assert.False(VbsMonitor.HvciBlocked(after));
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
        if (ok)
        {
            // The cross-contract, executable: every name IsSafeKeyName accepts
            // must also pass the ElevatedRegistry injection guard, or ReadSnapshot
            // would feed a name into the batch that throws mid-apply.
            var ex = Record.Exception(() =>
                ElevatedRegistry.BuildHklmMultiDelete(new[] { ($@"SOFTWARE\K\{name}", "Enabled") }));
            Assert.Null(ex);
        }
    }

    [Theory]
    [InlineData(7, 7)]
    [InlineData(0, 0)]
    public void ToInt_PassesThroughInts(int input, int expected)
    {
        Assert.Equal(expected, VbsMonitor.ToInt(input));
    }

    [Fact]
    public void ToInt_AcceptsQwordTypedValues_RejectsOverflowAndStrings()
    {
        // Third-party tools sometimes write REG_QWORD (boxed long); a plain
        // "as int?" would read those as absent and suppress the UEFI-lock warning.
        Assert.Equal(1, VbsMonitor.ToInt(1L));
        Assert.Null(VbsMonitor.ToInt(long.MaxValue));
        Assert.Null(VbsMonitor.ToInt("1"));
        Assert.Null(VbsMonitor.ToInt(null));
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

    [Theory]
    [InlineData(true, false, true)]   // VBS monitored + held off → defer
    [InlineData(true, true, false)]   // VBS monitored but Enabled → no defer
    [InlineData(false, false, false)] // unmonitored → never defers
    [InlineData(false, true, false)]
    public void MemoryIntegrity_DefersToVbs_ExactlyWhenMonitoredOff(bool vbsMonitor, bool vbsDesiredOn, bool expectDefer)
    {
        var cfg = new AppConfig();
        cfg.Global.Vbs.Monitor = vbsMonitor;
        cfg.Global.Vbs.DesiredOn = vbsDesiredOn;
        Assert.Equal(expectDefer, MemoryIntegrityMonitor.DefersToVbs(cfg));
    }

    [Fact]
    public void MemoryIntegrityMonitor_DefersWhileVbsMonitoredOff()
    {
        // Smoke check on the real CheckDrift path: the defer guard runs before any
        // registry access, so this is deterministic regardless of machine state.
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

        var clone = AppConfigCloner.Clone(cfg);

        Assert.True(clone.Global.Vbs.Monitor);
        Assert.False(clone.Global.Vbs.DesiredOn);
        Assert.True(clone.Global.Vbs.AutoApply);
    }
}
