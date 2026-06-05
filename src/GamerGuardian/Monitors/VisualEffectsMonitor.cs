using GamerGuardian.Models;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Visual effects "Adjust for best performance". The authoritative selector is
/// HKCU\...\Explorer\VisualEffects\VisualFXSetting (2 = best performance, the value
/// the System Performance dialog and Windows itself use). Applying also writes the
/// best-performance UserPreferencesMask so the animation effects actually turn off.
/// Direct HKCU writes -- no elevation.
///
/// <para>Gaming/Default semantics: <c>DesiredOn=true</c> = best performance.
/// Verify is on VisualFXSetting (the monitor's drift gate, which the standard
/// verify pass re-reads); the binary mask is parsed only to report the real
/// animation state accurately (see <see cref="AnimationsDisabled"/>). Full
/// per-effect changes apply on the next sign-out/Explorer restart.</para>
/// </summary>
public sealed class VisualEffectsMonitor : IMonitoredSetting
{
    public string Id => "visualfx";
    private const string VisualEffectsKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
    private const string VisualFxValue = "VisualFXSetting";
    private const string DesktopKey = @"Control Panel\Desktop";
    private const string MaskValue = "UserPreferencesMask";

    // Empirically-confirmed "best performance" UserPreferencesMask on Win10/11.
    private static readonly byte[] BestPerfMask = { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 };

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.VisualFx;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn; // true = gaming (best performance)
        bool animOff = AnimationsDisabled(ReadMask());
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "Visual effects -- adjust for best performance",
            CurrentValue: current.Value ? "Best performance (gaming)" : "Default",
            DesiredValue: desired ? "Best performance (gaming)" : "Default",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: $"VisualFXSetting={(current.Value ? 2 : 0)}, animations {(animOff ? "off" : "on")}",
            RawDesired: desired ? "VisualFXSetting=2 + best-performance mask" : "VisualFXSetting=0 (let Windows choose)");
    }

    /// <summary>True when "adjust for best performance" is selected (VisualFXSetting=2).</summary>
    public static bool? ReadCurrent()
    {
        using var k = Registry.CurrentUser.OpenSubKey(VisualEffectsKey, writable: false);
        if (k?.GetValue(VisualFxValue) is int v) return IsBestPerformance(v);
        return null;
    }

    /// <summary>Pure: best-performance is VisualFXSetting == 2.</summary>
    public static bool IsBestPerformance(int? visualFxSetting) => visualFxSetting == 2;

    /// <summary>Pure bit-level parse of UserPreferencesMask: animations are
    /// disabled when the menu/combo/smooth-scroll animation bits (byte 0, mask
    /// 0x0E) are all clear -- the state "best performance" produces.</summary>
    public static bool AnimationsDisabled(byte[]? mask)
        => mask is { Length: > 0 } && (mask[0] & 0x0E) == 0;

    private static byte[]? ReadMask()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(DesktopKey, writable: false);
            return k?.GetValue(MaskValue) as byte[];
        }
        catch { return null; }
    }

    public static void Apply(bool gaming)
    {
        using (var k = Registry.CurrentUser.CreateSubKey(VisualEffectsKey, writable: true)!)
            k.SetValue(VisualFxValue, gaming ? 2 : 0, RegistryValueKind.DWord);

        if (gaming)
            using (var k = Registry.CurrentUser.CreateSubKey(DesktopKey, writable: true)!)
                k.SetValue(MaskValue, BestPerfMask, RegistryValueKind.Binary);
        // On revert we set VisualFXSetting=0 (let Windows choose) and leave the
        // mask for Windows to reconcile -- we never stored the user's prior mask.
    }
}
