using System.IO;
using System.Reflection;
using System.Text;
using GamerGuardian.Models;

namespace GamerGuardian.Services;

/// <summary>
/// Persists every applied change (manual or auto) plus drift / pause / memory
/// events to <c>%APPDATA%\GamerGuardian\changes.log</c> in append-only plain text.
/// Rotated when the file grows past ~1 MB.
///
/// <para><b>What a verbose entry contains (per the v0.1.38 schema):</b></para>
/// <list type="bullet">
///   <item><c>session</c> — short id shared by every record in one Apply batch (groupable for grep).</item>
///   <item><c>source</c> — manual / auto / auto-revert / preset.</item>
///   <item><c>settingName</c>, <c>settingId</c> — human + machine identifiers.</item>
///   <item><c>location</c> — the registry path, API name, or service handle the app touched.</item>
///   <item><c>before</c>, <c>desired</c>, <c>after</c> — both display and raw values.</item>
///   <item><c>applyCmd</c> — copy-pasteable PowerShell that reproduces the change.</item>
///   <item><c>verifyCmd</c> — copy-pasteable PowerShell that reads the current value.</item>
///   <item><c>status</c>, <c>elapsedMs</c>, <c>reboot</c> — outcome of the apply.</item>
///   <item><c>error</c> — exception message if Apply threw (e.g. user denied UAC).</item>
///   <item><c>stickiness</c> — how many times Windows has reverted this same value in the past hour.</item>
/// </list>
///
/// <para>External resets (Windows changed a value we'd previously applied) are
/// logged as a distinct <c>EXTRESET</c> line so a user grepping <c>changes.log</c>
/// can see exactly when and why each silent-restore happened.</para>
/// </summary>
public static class ChangeLogger
{
    public static string LogPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GamerGuardian", "changes.log");

    private const long MaxBytes = 1_000_000;
    private const string Divider = "--------------------------------------------------------------------------------";

    public static void LogApplyResults(IEnumerable<ApplyResult> results, string source)
    {
        try
        {
            EnsureLogDir();
            RotateIfNeeded();

            var sb = new StringBuilder();
            var list = results as IList<ApplyResult> ?? results.ToList();
            if (list.Count == 0) return;

            var session = list[0].SessionId;
            sb.AppendLine(Divider);
            sb.AppendLine($"[{Now()}] [APPLY-START] session={session}  source={source}  count={list.Count}");
            sb.AppendLine(Divider);

            foreach (var r in list) sb.Append(Format(r));

            int verified = list.Count(r => r.Verified);
            long totalMs = 0;
            for (int i = 0; i < list.Count; i++) totalMs += list[i].ElapsedMs;
            sb.AppendLine(Divider);
            sb.AppendLine($"[{Now()}] [APPLY-END  ] session={session}  verified={verified}/{list.Count}  totalMs={totalMs}");
            sb.AppendLine();

            File.AppendAllText(LogPath, sb.ToString(), Encoding.UTF8);
        }
        catch { /* logging is best-effort */ }
    }

    /// <summary>
    /// Records a preference toggle (Monitor / AutoApply / Want) made in the
    /// Settings UI's draft. Single-line so the log stays scannable. These are
    /// staged-only — the actual change is not applied until the user clicks
    /// Apply or Save &amp; close, which produces the multi-line APPLY entries above.
    /// </summary>
    public static void LogPreferenceChange(string settingName, string field, string before, string after)
    {
        try
        {
            EnsureLogDir();
            RotateIfNeeded();
            var line = $"[{Now()}] [PREF-STAGE] {settingName}  |  {field}: {before} -> {after}";
            File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
        }
        catch { }
    }

    /// <summary>
    /// Records the start or end of a polling pause (fullscreen / benchmark / user).
    /// </summary>
    public static void LogPauseEvent(string action, string reason)
    {
        try
        {
            EnsureLogDir();
            RotateIfNeeded();
            var line = $"[{Now()}] [PAUSE     ] {action,-7} {reason}";
            File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
        }
        catch { }
    }

    /// <summary>
    /// Records a working-set / private-memory snapshot. Called on each periodic trim
    /// so users can grep "[MEM]" in changes.log and see whether memory holds steady
    /// over hours, instead of guessing.
    /// </summary>
    public static void LogMemorySnapshot(string trigger)
    {
        try
        {
            EnsureLogDir();
            RotateIfNeeded();
            using var p = System.Diagnostics.Process.GetCurrentProcess();
            long ws = p.WorkingSet64 / 1024 / 1024;
            long priv = p.PrivateMemorySize64 / 1024 / 1024;
            int handles = p.HandleCount;
            int threads = p.Threads.Count;
            var line = $"[{Now()}] [MEM       ] WS={ws}MB  Priv={priv}MB  Handles={handles}  Threads={threads}  ({trigger})";
            File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
        }
        catch { }
    }

    /// <summary>
    /// Header written once per app start. Includes the things you'd otherwise
    /// have to guess about from the timestamps alone: app version, OS build,
    /// elevation, process id, .NET runtime. Useful when comparing logs from
    /// multiple machines or after an upgrade.
    /// </summary>
    public static void LogSessionStart()
    {
        try
        {
            EnsureLogDir();
            RotateIfNeeded();
            var asm = typeof(ChangeLogger).Assembly;
            var ver = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";
            var os = Environment.OSVersion.VersionString;
            using var p = System.Diagnostics.Process.GetCurrentProcess();
            var elevated = IsElevated();
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(Divider);
            sb.AppendLine($"[{Now()}] [SESSION   ] GamerGuardian v{ver}");
            sb.AppendLine($"  OS         : {os}  ({Environment.OSVersion.Platform})");
            sb.AppendLine($"  CLR        : .NET {Environment.Version}");
            sb.AppendLine($"  Machine    : {Environment.MachineName}");
            sb.AppendLine($"  User       : {Environment.UserName}  (elevated: {elevated})");
            sb.AppendLine($"  PID        : {p.Id}");
            sb.AppendLine($"  ConfigPath : {Path.Combine(Path.GetDirectoryName(LogPath) ?? "", "config.json")}");
            sb.AppendLine(Divider);
            File.AppendAllText(LogPath, sb.ToString(), Encoding.UTF8);
        }
        catch { }
    }

    /// <summary>
    /// Records that Windows (or another tool) changed a value the app had
    /// previously applied and verified. Distinct line type so log readers can
    /// answer "what keeps reverting?" with a single grep. Includes how long the
    /// previous applied value held before drifting.
    /// </summary>
    public static void LogExternalReset(
        string settingId,
        string description,
        string lastAppliedValue,
        string currentValue,
        DateTimeOffset lastAppliedAt,
        int stickinessCount,
        bool autoApplyOn)
    {
        try
        {
            EnsureLogDir();
            RotateIfNeeded();
            var held = DateTimeOffset.UtcNow - lastAppliedAt;
            var willCorrect = autoApplyOn ? "will silently restore" : "no auto-apply (notify only)";
            var sb = new StringBuilder();
            sb.AppendLine(Divider);
            sb.AppendLine($"[{Now()}] [EXTRESET  ] {description}");
            sb.AppendLine($"  settingId    : {settingId}");
            sb.AppendLine($"  lastApplied  : {lastAppliedValue}  (held for {FormatDuration(held)})");
            sb.AppendLine($"  currentValue : {currentValue}");
            sb.AppendLine($"  stickiness   : Windows has reverted this setting {stickinessCount} time(s) since app start");
            sb.AppendLine($"  next         : {willCorrect}");
            File.AppendAllText(LogPath, sb.ToString(), Encoding.UTF8);
        }
        catch { }
    }

    private static string Format(ApplyResult r)
    {
        var status = r.ErrorMessage is not null ? "ERROR" : (r.Verified ? "OK" : "FAILED");
        var sb = new StringBuilder();
        sb.AppendLine($"[{Now()}] [{r.Source,-10}] {status,-6} {r.Description}");
        sb.AppendLine($"  session      : {r.SessionId}");
        sb.AppendLine($"  settingId    : {r.SettingId}");
        sb.AppendLine($"  location     : {r.Mechanism}");
        sb.AppendLine($"  before       : {FormatValue(r.Before, r.RawBefore)}");
        sb.AppendLine($"  desired      : {FormatValue(r.Desired, r.RawDesired)}");
        sb.AppendLine($"  after        : {FormatValue(r.After, r.RawAfter)}  {(r.Verified ? "<- verified" : "<- NOT VERIFIED")}");
        if (!string.IsNullOrEmpty(r.ApplyCommand))
            sb.AppendLine($"  applyCmd     : {r.ApplyCommand}");
        if (!string.IsNullOrEmpty(r.VerifyCommand))
            sb.AppendLine($"  verifyCmd    : {r.VerifyCommand}");
        sb.AppendLine($"  elapsedMs    : {r.ElapsedMs}");
        if (r.RequiresReboot)
            sb.AppendLine($"  reboot       : required to take effect");
        if (r.ExternalResetDetected)
            sb.AppendLine($"  trigger      : Windows externally reset this value; this entry is the corrective re-apply");
        if (r.StickinessCount > 0)
            sb.AppendLine($"  stickiness   : Windows has reverted this {r.StickinessCount} time(s) this session");
        if (r.ErrorMessage is not null)
            sb.AppendLine($"  error        : {r.ErrorMessage}");
        sb.AppendLine();
        return sb.ToString();
    }

    private static string FormatValue(string display, string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw == display) return display;
        return $"{display}  ({raw})";
    }

    private static string FormatDuration(TimeSpan d)
    {
        if (d.TotalSeconds < 60) return $"{(int)d.TotalSeconds}s";
        if (d.TotalMinutes < 60) return $"{(int)d.TotalMinutes}m{d.Seconds}s";
        if (d.TotalHours < 24)   return $"{(int)d.TotalHours}h{d.Minutes}m";
        return $"{(int)d.TotalDays}d{d.Hours}h";
    }

    private static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    private static bool IsElevated()
    {
        try
        {
            using var id = System.Security.Principal.WindowsIdentity.GetCurrent();
            return new System.Security.Principal.WindowsPrincipal(id)
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    private static void EnsureLogDir()
    {
        var dir = Path.GetDirectoryName(LogPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }

    private static void RotateIfNeeded()
    {
        try
        {
            var fi = new FileInfo(LogPath);
            if (!fi.Exists || fi.Length < MaxBytes) return;
            var prev = LogPath + ".1";
            if (File.Exists(prev)) File.Delete(prev);
            File.Move(LogPath, prev);
        }
        catch { }
    }
}
