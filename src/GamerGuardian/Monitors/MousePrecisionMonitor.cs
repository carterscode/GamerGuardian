using GamerGuardian.Models;
using GamerGuardian.Native;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

public sealed class MousePrecisionMonitor : IMonitoredSetting
{
    public string Id => "mouseaccel";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.MousePrecision;

        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "global",
            DisplayLabel: "Global",
            Description: "Mouse \"Enhance pointer precision\"",
            CurrentValue: current.Value ? "On" : "Off",
            DesiredValue: desired ? "On" : "Off",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore: current.Value ? "[6, 10, 1]" : "[0, 0, 0]",
            RawDesired: desired ? "[6, 10, 1]" : "[0, 0, 0]");
    }

    public static bool? ReadCurrent()
    {
        var p = new int[3];
        if (!User32.SystemParametersInfo(User32.SPI_GETMOUSE, 0, p, 0)) return null;
        return p[2] != 0;
    }

    public static bool Apply(bool on)
    {
        int[] p = on ? new[] { 6, 10, 1 } : new[] { 0, 0, 0 };
        bool ok = User32.SystemParametersInfo(
            User32.SPI_SETMOUSE, 0, p,
            User32.SPIF_UPDATEINIFILE | User32.SPIF_SENDCHANGE);
        try
        {
            using var k = Registry.CurrentUser.CreateSubKey(@"Control Panel\Mouse", writable: true)!;
            k.SetValue("MouseSpeed", on ? "1" : "0", RegistryValueKind.String);
            k.SetValue("MouseThreshold1", on ? "6" : "0", RegistryValueKind.String);
            k.SetValue("MouseThreshold2", on ? "10" : "0", RegistryValueKind.String);
        }
        catch { }
        return ok;
    }
}
