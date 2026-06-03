using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Disables Microsoft 365 Copilot inside the desktop Word / Excel / OneNote
/// apps + opts out of Microsoft's AI model training on document contents.
///
/// <para>Per-app settings live under <c>HKCU\Software\Microsoft\Office\16.0\{App}\Options</c>;
/// the model-training opt-out lives under the HKLM admin-template path so it
/// covers every Office user on the machine. We honor what zoicware does and
/// flip all four together.</para>
///
/// <para>Detection: only flags drift on machines where Office is installed
/// (the HKCU\Software\Microsoft\Office\16.0\* keys exist). On machines
/// without Office, ReadCurrent returns null and CheckDrift yields nothing.</para>
/// </summary>
public sealed class OfficeCopilotMonitor : IMonitoredSetting
{
    public string Id => "ai.office";

    private const string OfficeRoot = @"Software\Microsoft\Office\16.0";
    private const string TrainingKey = @"SOFTWARE\Policies\Microsoft\office\16.0\common\ai\training\general";
    private const string TrainingVal = "disabletraining";

    private static readonly (string Subkey, string Value)[] AppCopilotValues = new[]
    {
        (OfficeRoot + @"\Word\Options",   "EnableCopilot"),
        (OfficeRoot + @"\Excel\Options",  "EnableCopilot"),
        (OfficeRoot + @"\OneNote\Options\Copilot", "CopilotEnabled"),
    };

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.OfficeCopilot;
        var current = ReadCurrent();
        if (current is null) yield break;  // Office not installed -- no drift
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "ai",
            DisplayLabel: "Windows AI",
            Description: desired
                ? "Microsoft 365 Copilot in Word/Excel/OneNote -- re-enable"
                : "Microsoft 365 Copilot in Word/Excel/OneNote + training opt-out",
            CurrentValue: current.Value ? "On" : "Off",
            DesiredValue: desired ? "On" : "Off",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore:  current.Value ? "(default)" : "Word.EnableCopilot=0, Excel.EnableCopilot=0, OneNote.CopilotEnabled=0, training disabled",
            RawDesired: desired ? "(deleted)" : "Word.EnableCopilot=0, Excel.EnableCopilot=0, OneNote.CopilotEnabled=0, training disabled");
    }

    /// <summary>
    /// True (on) when Office is installed AND at least one Copilot toggle is
    /// missing or non-zero. False (off) when every toggle reads 0 AND the
    /// training opt-out is in place. Null when Office isn't installed (so
    /// CheckDrift can no-op without flagging missing-software drift).
    /// </summary>
    public static bool? ReadCurrent()
    {
        try
        {
            using var officeProbe = Registry.CurrentUser.OpenSubKey(OfficeRoot, writable: false);
            if (officeProbe is null) return null;

            foreach (var (subkey, value) in AppCopilotValues)
            {
                using var k = Registry.CurrentUser.OpenSubKey(subkey, writable: false);
                var v = k?.GetValue(value) as int?;
                if (v != 0) return true;  // any Copilot toggle that's not 0 = effectively "on"
            }
            using var t = Registry.LocalMachine.OpenSubKey(TrainingKey, writable: false);
            var training = t?.GetValue(TrainingVal) as int?;
            if (training != 1) return true;
            return false;
        }
        catch { return null; }
    }

    public static void Apply(bool on)
    {
        try
        {
            foreach (var (subkey, value) in AppCopilotValues)
            {
                if (on)
                {
                    using var k = Registry.CurrentUser.OpenSubKey(subkey, writable: true);
                    k?.DeleteValue(value, throwOnMissingValue: false);
                }
                else
                {
                    using var k = Registry.CurrentUser.CreateSubKey(subkey, writable: true)!;
                    k.SetValue(value, 0, RegistryValueKind.DWord);
                }
            }
            if (on)
                ElevatedRegistry.DeleteHklmValue(TrainingKey, TrainingVal);
            else
                ElevatedRegistry.SetHklmDword(TrainingKey, TrainingVal, 1);
        }
        catch { }
    }
}
