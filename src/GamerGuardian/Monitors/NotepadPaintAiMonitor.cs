using GamerGuardian.Models;
using GamerGuardian.Services;
using Microsoft.Win32;

namespace GamerGuardian.Monitors;

/// <summary>
/// Per-user AI feature flags for Notepad (Rewrite) and Paint (Cocreator,
/// Image Creator, Generative Erase). HKCU writes the per-user flags; HKLM
/// writes the machine-wide policy for Paint's Image Creator (zoicware uses
/// both -- the HKLM policy stops the feature from offering the cloud-AI
/// suggestion banner even before per-user toggle). Also blanks Paint's
/// experiment-targeting opt-in.
/// </summary>
public sealed class NotepadPaintAiMonitor : IMonitoredSetting
{
    public string Id => "ai.notepadpaint";

    private const string NotepadKey = @"Software\Microsoft\Notepad";
    private const string NotepadVal = "RewriteEnabled";
    private const string PaintKey   = @"Software\Microsoft\Windows\CurrentVersion\Paint";
    private const string PaintTargetingKey = @"Software\Microsoft\Windows\CurrentVersion\Applets\Paint\View";
    private const string PaintTargetingVal = "IsSignedUpForTargetingService";
    private const string HklmPaintPolicyKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint";
    private const string HklmPaintPolicyVal = "DisableImageCreator";

    public IEnumerable<DriftItem> CheckDrift(AppConfig config)
    {
        var pref = config.Global.NotepadPaintAi;
        var current = ReadCurrent();
        if (current is null) yield break;
        if (current.Value == pref.DesiredOn) yield break;

        bool desired = pref.DesiredOn;
        yield return new DriftItem(
            SettingId: Id,
            DisplayKey: "ai",
            DisplayLabel: "Windows AI",
            Description: desired
                ? "Notepad Rewrite + Paint AI features -- re-enable"
                : "Notepad Rewrite + Paint AI features (Cocreator, Image Creator, Generative Erase) -- disable",
            CurrentValue: current.Value ? "On" : "Off",
            DesiredValue: desired ? "On" : "Off",
            AutoApply: pref.AutoApply,
            Apply: () => Task.Run(() => Apply(desired)),
            IsMonitored: pref.Monitor,
            RawBefore:  current.Value ? "(default)" : "Notepad.RewriteEnabled=0, Paint.{DisableCocreator,DisableImageCreator,DisableGenerativeErase}=1, HKLM Paint policy + targeting opt-out",
            RawDesired: desired ? "(deleted)" : "Notepad.RewriteEnabled=0, Paint.{DisableCocreator,DisableImageCreator,DisableGenerativeErase}=1, HKLM Paint policy + targeting opt-out");
    }

    public static bool? ReadCurrent()
    {
        try
        {
            using var notepad = Registry.CurrentUser.OpenSubKey(NotepadKey, writable: false);
            using var paint   = Registry.CurrentUser.OpenSubKey(PaintKey, writable: false);
            using var paintTargeting = Registry.CurrentUser.OpenSubKey(PaintTargetingKey, writable: false);
            using var hklmPaint = Registry.LocalMachine.OpenSubKey(HklmPaintPolicyKey, writable: false);
            var rewrite     = notepad?.GetValue(NotepadVal) as int?;
            var cocreator   = paint?.GetValue("DisableCocreator") as int?;
            var imageCre    = paint?.GetValue("DisableImageCreator") as int?;
            var generErase  = paint?.GetValue("DisableGenerativeErase") as int?;
            var targeting   = paintTargeting?.GetValue(PaintTargetingVal) as int?;
            var hklmDisable = hklmPaint?.GetValue(HklmPaintPolicyVal) as int?;
            bool off = rewrite == 0 && cocreator == 1 && imageCre == 1 && generErase == 1
                       && targeting == 0 && hklmDisable == 1;
            return !off;
        }
        catch { return null; }
    }

    public static void Apply(bool on)
    {
        try
        {
            using var notepad = Registry.CurrentUser.CreateSubKey(NotepadKey, writable: true)!;
            using var paint   = Registry.CurrentUser.CreateSubKey(PaintKey, writable: true)!;
            using var paintTargeting = Registry.CurrentUser.CreateSubKey(PaintTargetingKey, writable: true)!;
            if (on)
            {
                notepad.DeleteValue(NotepadVal, throwOnMissingValue: false);
                paint.DeleteValue("DisableCocreator", throwOnMissingValue: false);
                paint.DeleteValue("DisableImageCreator", throwOnMissingValue: false);
                paint.DeleteValue("DisableGenerativeErase", throwOnMissingValue: false);
                paintTargeting.DeleteValue(PaintTargetingVal, throwOnMissingValue: false);
                ElevatedRegistry.DeleteHklmValue(HklmPaintPolicyKey, HklmPaintPolicyVal);
            }
            else
            {
                notepad.SetValue(NotepadVal, 0, RegistryValueKind.DWord);
                paint.SetValue("DisableCocreator", 1, RegistryValueKind.DWord);
                paint.SetValue("DisableImageCreator", 1, RegistryValueKind.DWord);
                paint.SetValue("DisableGenerativeErase", 1, RegistryValueKind.DWord);
                paintTargeting.SetValue(PaintTargetingVal, 0, RegistryValueKind.DWord);
                ElevatedRegistry.SetHklmDword(HklmPaintPolicyKey, HklmPaintPolicyVal, 1);
            }
        }
        catch { /* HKCU writes shouldn't fail; HKLM may UAC-fail and we swallow to keep the batch alive */ }
    }
}
