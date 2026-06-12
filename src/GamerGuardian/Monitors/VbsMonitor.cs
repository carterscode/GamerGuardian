using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Complete Virtualization-Based Security (VBS) disable — the full stack, not just
/// Memory Integrity. When DesiredOn = false this writes explicit REG_DWORD zeros to
/// the DeviceGuard root values, every <c>DeviceGuard\Scenarios\*</c> subkey (known
/// list plus whatever exists at runtime), <c>Lsa\LsaCfgFlags</c> (Credential Guard)
/// and the Group Policy mirror under <c>SOFTWARE\Policies</c>, and deletes the
/// per-scenario upgrade re-enable metadata (WasEnabledBy / EnabledBootId /
/// ChangedInBootCycle) wherever present.
///
/// <para>Explicit zeros, never deletes: Microsoft documents that absent values are
/// re-defaulted by feature updates (Credential Guard default enablement on 22H2+,
/// HVCI on clean installs), while values explicitly set to 0 before an upgrade
/// survive it. The Policies mirror additionally greys out the Windows Security
/// toggle, closing that re-enable vector.</para>
///
/// <para>Drift compares CONFIGURED registry state only. VBS cannot stop without a
/// reboot, so comparing runtime state would make post-apply verification impossible.
/// <c>RequiresReboot = true</c> drives the standard reboot prompt.</para>
///
/// <para>Interaction with <see cref="MemoryIntegrityMonitor"/>: VBS-off is a strict
/// superset of Memory-Integrity-off. While VBS is monitored with DesiredOn = false,
/// the Memory Integrity monitor defers (it yields no drift) so the two never fight
/// over the HVCI key; conversely a VBS restore skips the HVCI part whenever the
/// Memory Integrity preference is off (the pref tracks the live system state while
/// unmonitored, so this respects both an explicit choice and the machine state).</para>
///
/// <para>UEFI lock (<c>Locked = 1</c> or <c>LsaCfgFlags = 1</c>): registry writes
/// still land but firmware keeps VBS until the lock is cleared — that procedure
/// needs the EFI partition and a physical-presence prompt, so the app only detects
/// and reports it (drift description + Learn More), never automates it.</para>
/// </summary>
public sealed class VbsMonitor : IMonitoredSetting
{
    public string Id => "vbs";

    public const string RootKey = @"SYSTEM\CurrentControlSet\Control\DeviceGuard";
    public const string ScenariosKey = RootKey + @"\Scenarios";
    public const string LsaKey = @"SYSTEM\CurrentControlSet\Control\Lsa";
    public const string PolicyKey = @"SOFTWARE\Policies\Microsoft\Windows\DeviceGuard";

    public const string HvciScenario = "HypervisorEnforcedCodeIntegrity";

    /// <summary>Scenario subkeys we always pin to Enabled=0 on disable, even when the
    /// subkey doesn't exist yet (explicit suppression for the ones Windows creates
    /// later: Credential Guard on 22H2+; WindowsHello is the 24H2+ scenario that is
    /// community-reported — not formally documented — to keep VBS alive).</summary>
    public static readonly string[] KnownScenarios =
    {
        HvciScenario,
        "CredentialGuard",
        "SystemGuard",
        "KernelShadowStacks",
        "WindowsHello",
    };

    /// <summary>Per-scenario metadata deleted on disable. WasEnabledBy/EnabledBootId
    /// re-arm the upgrade re-enable path, so their presence counts as drift.
    /// ChangedInBootCycle is boot-cycle bookkeeping Windows may rewrite on its own:
    /// deleted when an apply runs, but never drift by itself (treating it as drift
    /// would mean a corrective apply + UAC prompt every boot).</summary>
    private static readonly string[] ScenarioMetaValues = { "WasEnabledBy", "EnabledBootId", "ChangedInBootCycle" };
    private static readonly string[] ReenableMetaValues = { "WasEnabledBy", "EnabledBootId" };

    /// <summary>State of one <c>DeviceGuard\Scenarios\*</c> subkey.</summary>
    public sealed record ScenarioState(int? Enabled, int? Locked, IReadOnlyList<string> MetaValuesPresent)
    {
        public static readonly ScenarioState Absent = new(null, null, Array.Empty<string>());
    }

    /// <summary>Everything CheckDrift/Apply need, read in one pass so the pure
    /// compliance/ops functions below are headless-testable.</summary>
    public sealed record VbsSnapshot(
        int? Evbs,
        int? RequirePlatformSecurityFeatures,
        int? Mandatory,
        int? RootHvci,
        int? RootLocked,
        IReadOnlyDictionary<string, ScenarioState> Scenarios,
        int? LsaCfgFlags,
        int? PolicyEvbs,
        int? PolicyLsaCfgFlags,
        int? PolicyHvci);

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.Vbs;
        VbsSnapshot snap;
        try { snap = ReadSnapshot(); }
        catch { yield break; }

        bool desired = pref.DesiredOn;
        if (IsCompliant(snap, desired, pref.Monitor)) yield break;

        // The user's Memory-Integrity-off choice wins over a VBS restore. The pref
        // tracks the live system state while that row is unmonitored
        // (SyncIfUnmonitored), so this respects an explicit preference and the
        // current machine state alike.
        bool skipHvci = desired && !config.Global.MemoryIntegrity.DesiredOn;

        var lockWarning = !desired && UefiLockDetected(snap)
            ? " — UEFI lock detected: registry disable applies, but firmware keeps VBS until the lock is cleared (see Learn More)"
            : "";

        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: (desired
                ? "Virtualization-Based Security — restore Windows defaults"
                : "Virtualization-Based Security — disable the full stack (all scenarios)")
                + " — requires reboot" + lockWarning,
            CurrentValue: Classify(snap),
            DesiredValue: desired ? "On" : "Off",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired, skipHvci)),
            RequiresReboot: true,
            IsMonitored: pref.Monitor,
            RawBefore: Summarize(snap),
            RawDesired: desired
                ? (skipHvci
                    ? "EnableVirtualizationBasedSecurity=1; disable markers removed (HVCI left to the Memory Integrity toggle)"
                    : "EnableVirtualizationBasedSecurity=1; HVCI Enabled=1, WasEnabledBy=2; disable markers removed")
                : "DeviceGuard {EVBS, RequirePlatformSecurityFeatures, Mandatory, HVCI}=0; Scenarios\\*\\Enabled=0; LsaCfgFlags=0; policy {EVBS, LsaCfgFlags, HVCI}=0; re-enable metadata deleted");
    }

    /// <summary>UI sync/row boolean. True = not explicitly disabled; false = fully
    /// disabled. Intentionally deviates from the usual null-on-absence monitor rule:
    /// Windows 11 runs VBS by default with NO registry values present, so absence
    /// means "Windows defaults (on)", not "feature not present on this SKU".
    /// (Limitation: on hardware where VBS genuinely can't run, this still reads
    /// "On"; the verify snippet's WMI check shows the runtime truth.)</summary>
    public static bool? ReadCurrent()
    {
        try { return !IsFullyDisabled(ReadSnapshot()); }
        catch { return null; }
    }

    /// <summary>Three-state row text — the binary <see cref="ReadCurrent"/> would
    /// show a half-disabled machine (e.g. EVBS=0 left by another tool) as plain
    /// "On", hiding exactly the state a user most needs to see.</summary>
    public static string ReadCurrentText()
    {
        try
        {
            return Classify(ReadSnapshot()) switch
            {
                "Off" => "Off",
                "On" => "On",
                _ => "Partially off",
            };
        }
        catch { return "not detected"; }
    }

    public static void Apply(bool on, bool skipHvci = false)
    {
        // Re-read at apply time: the ops are a diff against current state, so the
        // batched deletes only target values that existed moments ago (and the
        // delete chain is failure-tolerant for the TOCTOU window — see
        // ElevatedRegistry.BuildHklmBatch).
        var snap = ReadSnapshot();
        var (adds, deletes) = on ? BuildEnableOps(snap, skipHvci) : BuildDisableOps(snap);
        ElevatedRegistry.ApplyHklmBatch(adds, deletes);
    }

    // ---- Pure state functions (unit-tested headlessly) ---------------------

    /// <summary>The drift predicate. Monitored-Enabled enforces the strict
    /// restore contract (<see cref="HasNoDisableMarkers"/>); unmonitored-Enabled
    /// must round-trip with <see cref="ReadCurrent"/> so SyncIfUnmonitored keeps
    /// the row inert — a machine that is only partially disabled (e.g. EVBS=0
    /// from another tool) must never be "restored" as a side effect of an
    /// unrelated Apply the user never asked for.</summary>
    public static bool IsCompliant(VbsSnapshot s, bool desiredOn, bool monitored) =>
        desiredOn
            ? (monitored ? HasNoDisableMarkers(s) : !IsFullyDisabled(s))
            : IsFullyDisabled(s);

    /// <summary>The complete-disable contract: every relevant value explicitly 0 and
    /// the upgrade re-enable metadata (WasEnabledBy/EnabledBootId) gone.</summary>
    public static bool IsFullyDisabled(VbsSnapshot s)
    {
        if (s.Evbs != 0 || s.RequirePlatformSecurityFeatures != 0 || s.Mandatory != 0 || s.RootHvci != 0)
            return false;
        if (s.LsaCfgFlags != 0 || s.PolicyEvbs != 0 || s.PolicyLsaCfgFlags != 0 || s.PolicyHvci != 0)
            return false;
        foreach (var name in KnownScenarios)
        {
            if (!s.Scenarios.TryGetValue(name, out var sc) || sc.Enabled != 0)
                return false;
            if (sc.MetaValuesPresent.Any(m => ReenableMetaValues.Contains(m)))
                return false;
        }
        // Future scenario subkeys Windows may add: anything with Enabled != 0 keeps VBS alive.
        foreach (var (_, sc) in s.Scenarios)
            if (sc.Enabled is int e && e != 0)
                return false;
        return true;
    }

    /// <summary>The restore-default contract: no explicit-disable marker anywhere and
    /// the master switch not pinned to 0. The HVCI scenario value is deliberately not
    /// inspected — that's <see cref="MemoryIntegrityMonitor"/>'s domain.</summary>
    public static bool HasNoDisableMarkers(VbsSnapshot s)
    {
        if (s.Evbs == 0 || s.PolicyEvbs == 0 || s.PolicyHvci == 0) return false;
        if (s.LsaCfgFlags == 0 || s.PolicyLsaCfgFlags == 0) return false;
        foreach (var (name, sc) in s.Scenarios)
            if (name != HvciScenario && sc.Enabled == 0)
                return false;
        return true;
    }

    /// <summary>VBS-disable markers that render a standalone Memory Integrity
    /// re-enable inert: the policy mirror overrides the scenario key, and the
    /// master switch must be on for HVCI to start. Used by
    /// <see cref="MemoryIntegrityMonitor"/> so a "Verified" HVCI enable can't be
    /// silently defeated by this monitor's earlier disable.</summary>
    public static bool HvciBlocked(VbsSnapshot s) =>
        s.Evbs == 0 || s.PolicyEvbs == 0 || s.PolicyHvci == 0;

    /// <summary>The minimal ops that clear <see cref="HvciBlocked"/> markers without
    /// touching Credential Guard or the other scenarios.</summary>
    public static (List<(string subkey, string name, string kind, string data)> adds,
                   List<(string subkey, string name)> deletes) BuildHvciUnblockOps(VbsSnapshot s)
    {
        var adds = new List<(string, string, string, string)>();
        var deletes = new List<(string, string)>();
        if (s.Evbs == 0) adds.Add((RootKey, "EnableVirtualizationBasedSecurity", "REG_DWORD", "1"));
        if (s.PolicyEvbs == 0) deletes.Add((PolicyKey, "EnableVirtualizationBasedSecurity"));
        if (s.PolicyHvci == 0) deletes.Add((PolicyKey, "HypervisorEnforcedCodeIntegrity"));
        return (adds, deletes);
    }

    /// <summary>VBS or Credential Guard enabled with UEFI lock — registry writes alone
    /// won't stop it; firmware keeps the state until the documented physical-presence
    /// opt-out is performed.</summary>
    public static bool UefiLockDetected(VbsSnapshot s)
    {
        if (s.RootLocked is int l && l != 0) return true;
        if (s.LsaCfgFlags == 1) return true; // 1 = Credential Guard with UEFI lock; 2 = without
        foreach (var (_, sc) in s.Scenarios)
            if (sc.Locked is int sl && sl != 0)
                return true;
        return false;
    }

    public static (List<(string subkey, string name, string kind, string data)> adds,
                   List<(string subkey, string name)> deletes) BuildDisableOps(VbsSnapshot s)
    {
        var adds = new List<(string, string, string, string)>();
        var deletes = new List<(string, string)>();

        void Zero(string subkey, string name, int? current)
        {
            if (current != 0) adds.Add((subkey, name, "REG_DWORD", "0"));
        }

        Zero(RootKey, "EnableVirtualizationBasedSecurity", s.Evbs);
        Zero(RootKey, "RequirePlatformSecurityFeatures", s.RequirePlatformSecurityFeatures);
        Zero(RootKey, "Mandatory", s.Mandatory);
        Zero(RootKey, "HypervisorEnforcedCodeIntegrity", s.RootHvci);

        foreach (var name in AllScenarioNames(s))
        {
            var sc = s.Scenarios.TryGetValue(name, out var v) ? v : ScenarioState.Absent;
            Zero($@"{ScenariosKey}\{name}", "Enabled", sc.Enabled);
            foreach (var meta in sc.MetaValuesPresent)
                deletes.Add(($@"{ScenariosKey}\{name}", meta));
        }

        Zero(LsaKey, "LsaCfgFlags", s.LsaCfgFlags);
        Zero(PolicyKey, "EnableVirtualizationBasedSecurity", s.PolicyEvbs);
        Zero(PolicyKey, "LsaCfgFlags", s.PolicyLsaCfgFlags);
        Zero(PolicyKey, "HypervisorEnforcedCodeIntegrity", s.PolicyHvci);

        return (adds, deletes);
    }

    public static (List<(string subkey, string name, string kind, string data)> adds,
                   List<(string subkey, string name)> deletes) BuildEnableOps(VbsSnapshot s, bool skipHvci)
    {
        var adds = new List<(string, string, string, string)>();
        var deletes = new List<(string, string)>();

        // Only remove a marker where it currently holds the disable value — a real
        // domain GPO of 1/2 is never fought, and Windows-set enables are left alone.
        void RemoveIfZero(string subkey, string name, int? current)
        {
            if (current == 0) deletes.Add((subkey, name));
        }

        if (s.Evbs == 0) adds.Add((RootKey, "EnableVirtualizationBasedSecurity", "REG_DWORD", "1"));

        // Policy-mirror zeros first: they are what greys out Windows Security and
        // blocks every other re-enable path, so they must be the least likely
        // deletes to be skipped if anything goes wrong mid-batch.
        RemoveIfZero(PolicyKey, "EnableVirtualizationBasedSecurity", s.PolicyEvbs);
        RemoveIfZero(PolicyKey, "LsaCfgFlags", s.PolicyLsaCfgFlags);
        RemoveIfZero(PolicyKey, "HypervisorEnforcedCodeIntegrity", s.PolicyHvci);
        RemoveIfZero(LsaKey, "LsaCfgFlags", s.LsaCfgFlags);

        RemoveIfZero(RootKey, "RequirePlatformSecurityFeatures", s.RequirePlatformSecurityFeatures);
        RemoveIfZero(RootKey, "Mandatory", s.Mandatory);
        RemoveIfZero(RootKey, "HypervisorEnforcedCodeIntegrity", s.RootHvci);

        foreach (var (name, sc) in s.Scenarios)
        {
            if (name == HvciScenario) continue; // handled below
            RemoveIfZero($@"{ScenariosKey}\{name}", "Enabled", sc.Enabled);
        }

        if (!skipHvci)
        {
            var hvci = s.Scenarios.TryGetValue(HvciScenario, out var v) ? v : ScenarioState.Absent;
            if (hvci.Enabled != 1)
                adds.Add(($@"{ScenariosKey}\{HvciScenario}", "Enabled", "REG_DWORD", "1"));
            if (!hvci.MetaValuesPresent.Contains("WasEnabledBy"))
                adds.Add(($@"{ScenariosKey}\{HvciScenario}", "WasEnabledBy", "REG_DWORD", "2"));
        }

        return (adds, deletes);
    }

    /// <summary>Union of the pinned-known list and whatever exists on this machine,
    /// so future Windows scenario subkeys get zeroed too.</summary>
    public static IReadOnlyList<string> AllScenarioNames(VbsSnapshot s)
    {
        var names = new List<string>(KnownScenarios);
        foreach (var name in s.Scenarios.Keys)
            if (!names.Contains(name, StringComparer.OrdinalIgnoreCase))
                names.Add(name);
        return names;
    }

    public static string Classify(VbsSnapshot s) =>
        IsFullyDisabled(s) ? "Off" : HasNoDisableMarkers(s) ? "On" : "Partially off";

    public static string Summarize(VbsSnapshot s)
    {
        static string V(int? v) => v?.ToString() ?? "absent";
        var scenarios = string.Join(", ",
            AllScenarioNames(s).Select(n =>
                $"{n}={V(s.Scenarios.TryGetValue(n, out var sc) ? sc.Enabled : null)}"));
        return $"EVBS={V(s.Evbs)}, RPSF={V(s.RequirePlatformSecurityFeatures)}, Mandatory={V(s.Mandatory)}, " +
               $"Locked={V(s.RootLocked)}, Scenarios[{scenarios}], LsaCfgFlags={V(s.LsaCfgFlags)}, " +
               $"Policy[EVBS={V(s.PolicyEvbs)}, LsaCfgFlags={V(s.PolicyLsaCfgFlags)}, HVCI={V(s.PolicyHvci)}]";
    }

    // ---- Registry read ------------------------------------------------------

    public static VbsSnapshot ReadSnapshot()
    {
        using var root = Registry.LocalMachine.OpenSubKey(RootKey, writable: false);
        using var lsa = Registry.LocalMachine.OpenSubKey(LsaKey, writable: false);
        using var pol = Registry.LocalMachine.OpenSubKey(PolicyKey, writable: false);

        var scenarios = new Dictionary<string, ScenarioState>(StringComparer.OrdinalIgnoreCase);
        using (var scen = Registry.LocalMachine.OpenSubKey(ScenariosKey, writable: false))
        {
            if (scen is not null)
            {
                foreach (var name in scen.GetSubKeyNames())
                {
                    // reg.exe command segments forbid shell metacharacters; scenario
                    // names are Microsoft-defined identifiers, so skip (never write to)
                    // anything that wouldn't pass the ElevatedRegistry guard.
                    if (!IsSafeKeyName(name)) continue;
                    using var sub = scen.OpenSubKey(name, writable: false);
                    if (sub is null) continue;
                    var meta = ScenarioMetaValues.Where(m => sub.GetValue(m) is not null).ToArray();
                    scenarios[name] = new ScenarioState(
                        ToInt(sub.GetValue("Enabled")),
                        ToInt(sub.GetValue("Locked")),
                        meta);
                }
            }
        }

        return new VbsSnapshot(
            Evbs: ToInt(root?.GetValue("EnableVirtualizationBasedSecurity")),
            RequirePlatformSecurityFeatures: ToInt(root?.GetValue("RequirePlatformSecurityFeatures")),
            Mandatory: ToInt(root?.GetValue("Mandatory")),
            RootHvci: ToInt(root?.GetValue("HypervisorEnforcedCodeIntegrity")),
            RootLocked: ToInt(root?.GetValue("Locked")),
            Scenarios: scenarios,
            LsaCfgFlags: ToInt(lsa?.GetValue("LsaCfgFlags")),
            PolicyEvbs: ToInt(pol?.GetValue("EnableVirtualizationBasedSecurity")),
            PolicyLsaCfgFlags: ToInt(pol?.GetValue("LsaCfgFlags")),
            PolicyHvci: ToInt(pol?.GetValue("HypervisorEnforcedCodeIntegrity")));
    }

    /// <summary>Registry values written by third-party tools are sometimes typed
    /// REG_QWORD (boxed long) instead of REG_DWORD; a plain <c>as int?</c> would
    /// read those as absent and, worst case, suppress the UEFI-lock warning.</summary>
    public static int? ToInt(object? value) => value switch
    {
        int i => i,
        long l when l >= int.MinValue && l <= int.MaxValue => (int)l,
        _ => null,
    };

    public static bool IsSafeKeyName(string name)
    {
        foreach (var c in name)
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != ' ')
                return false;
        return name.Length > 0;
    }
}
