using GamerGuardian.Models;
using GamerGuardian.Native;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Nagle's algorithm (TCP no-delay / ack-frequency), asserted per network
/// interface across all active physical adapters. Gaming = Nagle off
/// (TcpAckFrequency=1, TCPNoDelay=1) under each adapter's interface key. Writes
/// batch through one elevation prompt; reversal deletes the values across all
/// adapters in one prompt (U13). Empty active-adapter set = no drift.
///
/// <para>Contested tweak shipped at full parity by user choice -- benefit is
/// per-hardware and can make some connections worse. Inverted Gaming/Default
/// semantics: <c>DesiredOn=true</c> = gaming (Nagle off on every adapter).</para>
/// </summary>
public sealed class NagleMonitor : IMonitoredSetting
{
    public string Id => "network.nagle";
    private const string IfBase = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
    private static string SubkeyFor(string guid) => $@"{IfBase}\{guid}";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.Nagle;
        var adapters = NetworkAdapters.GetActivePhysical();
        if (adapters.Count == 0) yield break; // no active physical adapters -> not drift

        bool desired = pref.DesiredOn; // true = gaming (Nagle off)
        var states = adapters.Select(a => (a, gaming: IsAdapterGaming(a.Guid))).ToList();
        bool compliant = desired ? states.All(s => s.gaming) : states.All(s => !s.gaming);
        if (compliant) yield break;

        string before = string.Join("; ", states.Select(s => $"{s.a.Name}={(s.gaming ? "off" : "default")}"));
        string currentLabel = states.All(s => s.gaming) ? "Disabled (gaming)"
            : states.Any(s => s.gaming) ? "Mixed" : "Default";
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "network",
            DisplayLabel: "Network",
            Description: desired
                ? "Nagle's algorithm -- disable on all active adapters"
                : "Nagle's algorithm -- restore Windows default on all active adapters",
            CurrentValue: currentLabel,
            DesiredValue: desired ? "Disabled (gaming)" : "Default",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: before,
            RawDesired: desired ? "TcpAckFrequency=1, TCPNoDelay=1 (per active adapter)" : "(deleted, per active adapter)");
    }

    private static bool IsAdapterGaming(string guid)
    {
        try
        {
            using var k = Registry.LocalMachine.OpenSubKey(SubkeyFor(guid), writable: false);
            var ack = k?.GetValue("TcpAckFrequency") as int?;
            var noDelay = k?.GetValue("TCPNoDelay") as int?;
            return ack == 1 && noDelay == 1;
        }
        catch { return false; }
    }

    /// <summary>True when every active physical adapter has Nagle disabled (gaming).
    /// Null when there are no active physical adapters.</summary>
    public static bool? ReadCurrent()
    {
        var adapters = NetworkAdapters.GetActivePhysical();
        if (adapters.Count == 0) return null;
        return adapters.All(a => IsAdapterGaming(a.Guid));
    }

    public static void Apply(bool gaming)
    {
        var adapters = NetworkAdapters.GetActivePhysical();
        if (adapters.Count == 0) return;

        if (gaming)
        {
            var writes = adapters.SelectMany(a => new[]
            {
                (SubkeyFor(a.Guid), "TcpAckFrequency", "REG_DWORD", "1"),
                (SubkeyFor(a.Guid), "TCPNoDelay", "REG_DWORD", "1"),
            });
            ElevatedRegistry.SetHklmMulti(writes);
        }
        else
        {
            var deletes = adapters.SelectMany(a => new[]
            {
                (SubkeyFor(a.Guid), "TcpAckFrequency"),
                (SubkeyFor(a.Guid), "TCPNoDelay"),
            });
            ElevatedRegistry.DeleteHklmMulti(deletes);
        }
    }
}
