namespace GamerGuardian.Models;

/// <summary>
/// One applied (or attempted) change. Produced by
/// <see cref="Services.ChangeApplier"/>, consumed by
/// <see cref="UI.ApplyResultsWindow"/> and <see cref="Services.ChangeLogger"/>.
///
/// Fields are split into "what we wanted" (Before/Desired/Mechanism/ApplyCommand/VerifyCommand)
/// and "what happened" (After/Verified/ElapsedMs/ErrorMessage). The log writer
/// renders both halves so a user reading <c>changes.log</c> can fully reproduce
/// or roll back the change from PowerShell.
/// </summary>
public sealed record ApplyResult(
    string SettingId,
    string Description,
    string Before,
    string Desired,
    string After,
    bool Verified,
    bool RequiresReboot,
    string Mechanism,
    string VerifyCommand,
    string RawBefore = "",
    string RawDesired = "",
    string RawAfter = "",
    string ApplyCommand = "",
    long ElapsedMs = 0,
    string Source = "manual",
    string SessionId = "",
    string? ErrorMessage = null,
    bool ExternalResetDetected = false,
    int StickinessCount = 0);
