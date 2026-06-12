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
/// HVCI upgrade re-enable metadata (WasEnabledBy / EnabledBootId / ChangedInBootCycle).
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
/// over the HVCI key; conversely VBS re-enable skips the HVCI restore when Memory
/// Integrity is monitored with DesiredOn = false.</para>
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
    /// later: Credential Guard on 22H2+, WindowsHello on 24H2+).</summary>
    public static readonly string[] KnownScenarios =
    {
        HvciScenario,
        "CredentialGuard",
        "SystemGuard",
        "KernelShadowStacks",
        "WindowsHello",
    };

    /// <summary>Per-scenario re-enable metadata that must be deleted on disable —
    /// Windows uses these to restore HVCI after upgrades/boot-failure probation.</summary>
    private static readonly string[] ScenarioMetaValues = { "WasEnabledBy", "EnabledBootId", "ChangedInBootCycle" };

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
        bool compliant = desired ? HasNoDisableMarkers(snap) : IsFullyDisabled(snap);
        if (compliant) yield break;

        // The user's standalone Memory-Integrity-off choice wins over a VBS restore.
        bool skipHvci = desired && config.Global.MemoryIntegrity is { Monitor: true, DesiredOn: false };

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
                ? "EnableVirtualizationBasedSecurity=1; HVCI Enabled=1, WasEnabledBy=2; disable markers removed"
                : "DeviceGuard {EVBS, RequirePlatformSecurityFeatures, Mandatory, HVCI}=0; Scenarios\\*\\Enabled=0; LsaCfgFlags=0; policy {EVBS, LsaCfgFlags, HVCI}=0; HVCI meta deleted");
    }

    /// <summary>UI sync/row text. True = not explicitly disabled (on a modern Win11
    /// install that means VBS is on or free to turn on); false = fully disabled.
    /// Registry alone can't see firmware state — the verify snippet (WMI) shows the
    /// runtime truth.</summary>
    public static bool? ReadCurrent()
    {
        try { return !IsFullyDisabled(ReadSnapshot()); }
        catch { return null; }
    }

    public static void Apply(bool on, bool skipHvci = false)
    {
        // Re-read at apply time: the ops are a diff against current state, so the
        // batched deletes only target values that exist (reg.exe chains with && and
        // a failed delete would abort the rest).
        var snap = ReadSnapshot();
        var (adds, deletes) = on ? BuildEnableOps(snap, skipHvci) : BuildDisableOps(snap);
        ElevatedRegistry.ApplyHklmBatch(adds, deletes);
    }

    // ---- Pure state functions (unit-tested headlessly) ---------------------

    /// <summary>The complete-disable contract: every relevant value explicitly 0 and
    /// the per-scenario re-enable metadata gone.</summary>
    public static bool IsFullyDisabled(VbsSnapshot s)
    {
        if (s.Evbs != 0 || s.RequirePlatformSecurityFeatures != 0 || s.Mandatory != 0 || s.RootHvci != 0)
            return false;
        if (s.LsaCfgFlags != 0 || s.PolicyEvbs != 0 || s.PolicyLsaCfgFlags != 0 || s.PolicyHvci != 0)
            return false;
        foreach (var name in KnownScenarios)
            if (!s.Scenarios.TryGetValue(name, out var sc) || sc.Enabled != 0 || sc.MetaValuesPresent.Count > 0)
                return false;
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

        RemoveIfZero(LsaKey, "LsaCfgFlags", s.LsaCfgFlags);
        RemoveIfZero(PolicyKey, "EnableVirtualizationBasedSecurity", s.PolicyEvbs);
        RemoveIfZero(PolicyKey, "LsaCfgFlags", s.PolicyLsaCfgFlags);
        RemoveIfZero(PolicyKey, "HypervisorEnforcedCodeIntegrity", s.PolicyHvci);

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
                        sub.GetValue("Enabled") as int?,
                        sub.GetValue("Locked") as int?,
                        meta);
                }
            }
        }

        return new VbsSnapshot(
            Evbs: root?.GetValue("EnableVirtualizationBasedSecurity") as int?,
            RequirePlatformSecurityFeatures: root?.GetValue("RequirePlatformSecurityFeatures") as int?,
            Mandatory: root?.GetValue("Mandatory") as int?,
            RootHvci: root?.GetValue("HypervisorEnforcedCodeIntegrity") as int?,
            RootLocked: root?.GetValue("Locked") as int?,
            Scenarios: scenarios,
            LsaCfgFlags: lsa?.GetValue("LsaCfgFlags") as int?,
            PolicyEvbs: pol?.GetValue("EnableVirtualizationBasedSecurity") as int?,
            PolicyLsaCfgFlags: pol?.GetValue("LsaCfgFlags") as int?,
            PolicyHvci: pol?.GetValue("HypervisorEnforcedCodeIntegrity") as int?);
    }

    public static bool IsSafeKeyName(string name)
    {
        foreach (var c in name)
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != ' ')
                return false;
        return name.Length > 0;
    }
}
