namespace GamerGuardian.Models;

public sealed record ApplyResult(
    string SettingId,
    string Description,
    string Before,
    string Desired,
    string After,
    bool Verified,
    bool RequiresReboot,
    string Mechanism,
    string VerifyCommand);
