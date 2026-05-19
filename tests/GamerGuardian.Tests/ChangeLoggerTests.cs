using System.IO;
using System.Reflection;
using GamerGuardian.Models;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

/// <summary>
/// ChangeLogger writes to a static path under %APPDATA%. Rather than monkey-
/// patch that path, these tests invoke the formatter against a fresh log file
/// and grep the resulting text. They run sequentially via collection fixture
/// because they touch the same file.
/// </summary>
[Collection(nameof(ChangeLogSerialCollection))]
public class ChangeLoggerTests : IDisposable
{
    private readonly string _backupPath;

    public ChangeLoggerTests()
    {
        // Back up the real log file if one exists so we don't trample a user's
        // log when these tests run on a dev machine.
        _backupPath = ChangeLogger.LogPath + ".testbackup";
        if (File.Exists(ChangeLogger.LogPath))
        {
            File.Move(ChangeLogger.LogPath, _backupPath, overwrite: true);
        }
        var dir = Path.GetDirectoryName(ChangeLogger.LogPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }

    public void Dispose()
    {
        try { if (File.Exists(ChangeLogger.LogPath)) File.Delete(ChangeLogger.LogPath); } catch { }
        try
        {
            if (File.Exists(_backupPath)) File.Move(_backupPath, ChangeLogger.LogPath, overwrite: true);
        }
        catch { }
    }

    [Fact]
    public void LogApplyResults_IncludesAllVerboseFields()
    {
        var r = new ApplyResult(
            SettingId: "service:DiagTrack",
            Description: "DiagTrack -- stop and disable",
            Before: "Automatic",
            Desired: "Disabled",
            After: "Disabled",
            Verified: true,
            RequiresReboot: false,
            Mechanism: "sc.exe stop / config (writes HKLM\\SYSTEM\\...\\DiagTrack\\Start)",
            VerifyCommand: "sc qc \"DiagTrack\"",
            RawBefore: "2",
            RawDesired: "4",
            RawAfter: "4",
            ApplyCommand: "sc.exe stop \"DiagTrack\"; sc.exe config \"DiagTrack\" start= disabled",
            ElapsedMs: 412,
            Source: "manual",
            SessionId: "abc12345");

        ChangeLogger.LogApplyResults(new[] { r }, "manual");

        var log = File.ReadAllText(ChangeLogger.LogPath);

        // Batch markers
        Assert.Contains("[APPLY-START]", log);
        Assert.Contains("[APPLY-END  ]", log);
        Assert.Contains("session=abc12345", log);
        Assert.Contains("count=1", log);
        Assert.Contains("verified=1/1", log);

        // Per-change verbose fields the user asked for
        Assert.Contains("settingId    : service:DiagTrack", log);
        Assert.Contains("location     : sc.exe", log);
        Assert.Contains("before       : Automatic  (2)", log);
        Assert.Contains("desired      : Disabled  (4)", log);
        Assert.Contains("after        : Disabled  (4)", log);
        Assert.Contains("applyCmd     : sc.exe stop \"DiagTrack\"", log);
        Assert.Contains("verifyCmd    : sc qc \"DiagTrack\"", log);
        Assert.Contains("elapsedMs    : 412", log);
        Assert.Contains("<- verified", log);
    }

    [Fact]
    public void LogApplyResults_ErrorEntry_SurfacesErrorAndMarksFailed()
    {
        var r = new ApplyResult(
            SettingId: "hags",
            Description: "HAGS enable",
            Before: "Disabled",
            Desired: "Enabled",
            After: "Disabled",
            Verified: false,
            RequiresReboot: true,
            Mechanism: "HKLM\\...\\HwSchMode",
            VerifyCommand: "(Get-ItemProperty ...).HwSchMode",
            ErrorMessage: "UnauthorizedAccessException: denied",
            Source: "manual",
            SessionId: "xyz98765");

        ChangeLogger.LogApplyResults(new[] { r }, "manual");

        var log = File.ReadAllText(ChangeLogger.LogPath);
        Assert.Contains("ERROR ", log);
        Assert.Contains("error        : UnauthorizedAccessException: denied", log);
        Assert.Contains("reboot       : required to take effect", log);
        Assert.Contains("verified=0/1", log);
    }

    [Fact]
    public void LogExternalReset_EmitsStandaloneRecord()
    {
        ChangeLogger.LogExternalReset(
            settingId: "service:dosvc",
            description: "Delivery Optimization -- apply Group Policy override",
            lastAppliedValue: "Disabled by policy (100)",
            currentValue: "policy DODownloadMode=1 (1)",
            lastAppliedAt: DateTimeOffset.UtcNow.AddMinutes(-20),
            stickinessCount: 3,
            autoApplyOn: true);

        var log = File.ReadAllText(ChangeLogger.LogPath);
        Assert.Contains("[EXTRESET  ]", log);
        Assert.Contains("settingId    : service:dosvc", log);
        Assert.Contains("lastApplied  : Disabled by policy (100)", log);
        Assert.Contains("currentValue : policy DODownloadMode=1 (1)", log);
        Assert.Contains("stickiness   : Windows has reverted this setting 3", log);
        Assert.Contains("next         : will silently restore", log);
    }

    [Fact]
    public void LogExternalReset_NoAutoApply_LabelsItNotifyOnly()
    {
        ChangeLogger.LogExternalReset(
            settingId: "hags",
            description: "HAGS",
            lastAppliedValue: "Enabled (2)",
            currentValue: "Disabled (1)",
            lastAppliedAt: DateTimeOffset.UtcNow.AddHours(-2),
            stickinessCount: 1,
            autoApplyOn: false);

        Assert.Contains("notify only", File.ReadAllText(ChangeLogger.LogPath));
    }

    [Fact]
    public void LogSessionStart_IncludesVersionAndElevation()
    {
        ChangeLogger.LogSessionStart();
        var log = File.ReadAllText(ChangeLogger.LogPath);
        Assert.Contains("[SESSION   ]", log);
        Assert.Contains("OS         :", log);
        Assert.Contains("CLR        :", log);
        Assert.Contains("elevated:", log);
    }

    [Fact]
    public void LogPreferenceChange_StageLabelMakesItGreppable()
    {
        ChangeLogger.LogPreferenceChange("Service: DiagTrack", "Want", "Default", "Disabled");
        Assert.Contains("[PREF-STAGE]", File.ReadAllText(ChangeLogger.LogPath));
        Assert.Contains("Want: Default -> Disabled", File.ReadAllText(ChangeLogger.LogPath));
    }
}

[CollectionDefinition(nameof(ChangeLogSerialCollection), DisableParallelization = true)]
public sealed class ChangeLogSerialCollection { }
