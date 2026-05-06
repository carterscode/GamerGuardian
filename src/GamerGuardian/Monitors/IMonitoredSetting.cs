using GamerGuardian.Models;

namespace GamerGuardian.Monitors;

public interface IMonitoredSetting
{
    string Id { get; }
    IEnumerable<DriftItem> CheckDrift(AppConfig config);
}
