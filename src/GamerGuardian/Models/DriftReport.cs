namespace GamerGuardian.Models;

public sealed record DriftItem(
    string SettingId,
    string DisplayKey,
    string DisplayLabel,
    string Description,
    string CurrentValue,
    string DesiredValue,
    bool AutoApply,
    Func<Task> Apply);

public sealed record DriftReport(IReadOnlyList<DriftItem> Items)
{
    public bool HasDrift => Items.Count > 0;
}
