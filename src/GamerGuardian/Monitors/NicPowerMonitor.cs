using GamerGuardian.Models;
using GamerGuardian.Native;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// NIC power management -- "Allow the computer to turn off this device to save
/// power" -- asserted across all active physical adapters. Driven by the
/// <c>PnPCapabilities</c> value under the adapter's network-class instance
/// (matched to the interface GUID via <c>NetCfgInstanceId</c>). Gaming = power
/// management disabled (PnPCapabilities bits 0x18 set). Reversal is algorithmic
/// (clear the 0x18 bits, preserving other flags) so no machine-specific
/// before-value is persisted. Requires an adapter reset / reboot to take effect.
///
/// <para>Contested per-hardware tweak shipped at full parity. Inverted
/// Gaming/Default semantics: <c>DesiredOn=true</c> = gaming (power mgmt off).
/// Writes batch through one elevation prompt. Empty adapter set = no drift.</para>
/// </summary>
public sealed class NicPowerMonitor : IMonitoredSetting
{
    public string Id => "network.nicpower";
    private const string ClassKey = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";
    private const int PowerMgmtOffBits = 0x18; // "do not allow the computer to turn off this device"

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.NicPower;
        var mapped = MapAdapters();
        if (mapped.Count == 0) yield break; // no resolvable active physical adapter -> not drift

        bool desired = pref.DesiredOn; // true = gaming (power mgmt off)
        var states = mapped.Select(m => (m.name, m.subkey, gaming: IsGaming(m.subkey))).ToList();
        bool compliant = desired ? states.All(s => s.gaming) : states.All(s => !s.gaming);
        if (compliant) yield break;

        string before = string.Join("; ", states.Select(s => $"{s.name}={(s.gaming ? "mgmt-off" : "default")}"));
        string currentLabel = states.All(s => s.gaming) ? "Disabled (gaming)"
            : states.Any(s => s.gaming) ? "Mixed" : "Default";
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "network",
            DisplayLabel: "Network",
            Description: desired
                ? "NIC power management -- disable on all active adapters"
                : "NIC power management -- restore Windows default on all active adapters",
            CurrentValue: currentLabel,
            DesiredValue: desired ? "Disabled (gaming)" : "Default",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            RequiresReboot: true,
            IsMonitored: pref.Monitor,
            RawBefore: before,
            RawDesired: desired ? "PnPCapabilities |= 0x18 (per active adapter)" : "PnPCapabilities &= ~0x18 (per active adapter)");
    }

    /// <summary>True when every resolvable active physical adapter has power
    /// management disabled. Null when none can be resolved.</summary>
    public static bool? ReadCurrent()
    {
        var mapped = MapAdapters();
        if (mapped.Count == 0) return null;
        return mapped.All(m => IsGaming(m.subkey));
    }

    private static bool IsGaming(string subkey)
    {
        var v = ReadPnp(subkey);
        return v is { } val && (val & PowerMgmtOffBits) == PowerMgmtOffBits;
    }

    private static int? ReadPnp(string subkey)
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(subkey, writable: false);
            return k?.GetValue("PnPCapabilities") as int?;
        }
        catch { return null; }
    }

    /// <summary>Resolves each active physical adapter (by interface GUID) to its
    /// network-class instance subkey via NetCfgInstanceId. Adapters with no
    /// matching class instance are skipped.</summary>
    private static List<(string name, string subkey)> MapAdapters()
    {
        var result = new List<(string, string)>();
        var adapters = NetworkAdapters.GetActivePhysical();
        if (adapters.Count == 0) return result;

        try
        {
            using var classKey = Registry.LocalMachine.OpenSubKey(ClassKey, writable: false);
            if (classKey is null) return result;
            var instanceByGuid = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sub in classKey.GetSubKeyNames())
            {
                if (sub.Length != 4 || !sub.All(char.IsDigit)) continue;
                using var inst = classKey.OpenSubKey(sub, writable: false);
                if (inst?.GetValue("NetCfgInstanceId") is string guid && !string.IsNullOrEmpty(guid))
                    instanceByGuid[guid] = $@"{ClassKey}\{sub}";
            }
            foreach (var a in adapters)
                if (instanceByGuid.TryGetValue(a.Guid, out var subkey))
                    result.Add((a.Name, subkey));
        }
        catch { /* return whatever resolved */ }

        return result;
    }

    public static void Apply(bool gaming)
    {
        var mapped = MapAdapters();
        if (mapped.Count == 0) return;

        var writes = new List<(string, string, string, string)>();
        foreach (var (_, subkey) in mapped)
        {
            int current = ReadPnp(subkey) ?? 0;
            int target = gaming ? (current | PowerMgmtOffBits) : (current & ~PowerMgmtOffBits);
            writes.Add((subkey, "PnPCapabilities", "REG_DWORD", target.ToString()));
        }
        ElevatedRegistry.SetHklmMulti(writes);
    }
}
