using System.IO;
using System.Linq;
using GamerGuardian.Models;
using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

public class SettingDocsCatalogTests
{
    [Theory]
    [InlineData("gamemode")]
    [InlineData("gamedvr")]
    [InlineData("hags")]
    [InlineData("memintegrity")]
    [InlineData("vbs")]
    [InlineData("sysresponse")]
    [InlineData("netthrottle")]
    [InlineData("usbsuspend")]
    [InlineData("gamestask")]
    [InlineData("mouseaccel")]
    [InlineData("fso")]
    [InlineData("vrr")]
    [InlineData("powerplan")]
    [InlineData("cpuplan")]
    [InlineData("ai.copilot")]
    [InlineData("ai.recall")]
    [InlineData("ai.clicktodo")]
    [InlineData("ai.edge")]
    [InlineData("ai.notepadpaint")]
    [InlineData("ai.settingssearch")]
    [InlineData("ai.actions")]
    [InlineData("ai.inputinsights")]
    [InlineData("ai.office")]
    [InlineData("privacy.advertisingid")]
    [InlineData("privacy.tailoredexp")]
    [InlineData("privacy.cdp")]
    [InlineData("privacy.activityhistory")]
    [InlineData("privacy.speech")]
    [InlineData("privacy.inking")]
    [InlineData("debloat.suggestedcontent")]
    [InlineData("debloat.spotlight")]
    [InlineData("debloat.finishsetup")]
    [InlineData("debloat.startrecommend")]
    [InlineData("debloat.explorerads")]
    [InlineData("debloat.feedback")]
    [InlineData("debloat.widgets")]
    [InlineData("debloat.edge")]
    [InlineData("powerthrottling")]
    [InlineData("faststartup")]
    [InlineData("visualfx")]
    [InlineData("network.nagle")]
    [InlineData("network.nicpower")]
    public void Get_KnownIds_ReturnsPopulatedEntry(string id)
    {
        var d = SettingDocsCatalog.Get(id);
        Assert.NotNull(d);
        Assert.False(string.IsNullOrWhiteSpace(d!.DisplayName));
        Assert.False(string.IsNullOrWhiteSpace(d.What));
        Assert.False(string.IsNullOrWhiteSpace(d.Why));
        Assert.False(string.IsNullOrWhiteSpace(d.HowItHelps));
        Assert.False(string.IsNullOrWhiteSpace(d.Recommended));
        Assert.False(string.IsNullOrWhiteSpace(d.Risks));
        Assert.False(string.IsNullOrWhiteSpace(d.ReversibleVia));
        Assert.NotEmpty(d.Scenarios);
    }

    [Fact]
    public void Get_ServicePrefix_ReturnsCorrectEntry()
    {
        var d = SettingDocsCatalog.Get("service:DiagTrack");
        Assert.NotNull(d);
        // SettingId echoes the lookup id; DisplayName is the friendly Windows label.
        Assert.Equal("service:DiagTrack", d!.SettingId);
        Assert.Contains("Telemetry", d.DisplayName, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Get_AiAppPrefix_ReturnsCorrectEntry()
    {
        var d = SettingDocsCatalog.Get("ai.app:Microsoft.Copilot");
        Assert.NotNull(d);
        Assert.Contains("Copilot", d!.DisplayName);
    }

    [Fact]
    public void CpuPlan_HasMechanismApplyAndVerifyCommands()
    {
        Assert.Contains("PowerDuplicateScheme", SettingDocs.MechanismFor("cpuplan"));
        Assert.Contains("powercfg", SettingDocs.ApplyCommandFor("cpuplan"));
        Assert.Contains("getactivescheme", SettingDocs.VerifyCommandFor("cpuplan"));
    }

    [Fact]
    public void Get_UnknownId_ReturnsNull()
    {
        Assert.Null(SettingDocsCatalog.Get("definitely.not.a.real.setting"));
    }

    [Fact]
    public void Get_NullId_ReturnsNullWithoutThrowing()
    {
        Assert.Null(SettingDocsCatalog.Get(null!));
    }

    [Fact]
    public void All_HasNoDuplicateSettingIds()
    {
        var ids = SettingDocsCatalog.All.Select(d => d.SettingId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void All_EntriesHaveCompleteScenarios()
    {
        foreach (var d in SettingDocsCatalog.All)
        {
            Assert.True(d.Scenarios.Count >= 2,
                $"{d.SettingId} has fewer than 2 scenario recommendations -- users want per-use-case guidance");
            foreach (var (k, v) in d.Scenarios)
            {
                Assert.False(string.IsNullOrWhiteSpace(k));
                Assert.False(string.IsNullOrWhiteSpace(v));
            }
        }
    }

    [Fact]
    public void FormatForExpander_IncludesAllSections()
    {
        var text = SettingDocsCatalog.FormatForExpander("hags");
        Assert.Contains("Recommended:", text);
        Assert.Contains("Why this is the recommendation", text);
        Assert.Contains("What it does", text);
        Assert.Contains("How it helps", text);
        Assert.Contains("Pros & cons of each choice", text);
        Assert.Contains("Per-scenario", text);
        Assert.Contains("Risks", text);
        Assert.Contains("Command line (PowerShell)", text);
        Assert.Contains("Reverse it (restore the Windows default)", text);
        Assert.Contains("Reversible via", text);
    }

    [Fact]
    public void FormatForExpander_EmbedsVerifyApplyAndReverseCommands()
    {
        // The command block must surface the real verify / apply / reverse
        // PowerShell, not just section headers.
        var text = SettingDocsCatalog.FormatForExpander("hags");
        Assert.Contains("HwSchMode).HwSchMode", text);          // verify
        Assert.Contains("HwSchMode -Value 2", text);            // apply (gaming = On)
        Assert.Contains("HwSchMode -Value 1", text);            // reverse (Off)

        // Memory Integrity's apply must show the gaming (Off=0) value, not the
        // safe On fallback ApplyCommandFor uses when given no raw value.
        var mi = SettingDocsCatalog.FormatForExpander("memintegrity");
        Assert.Contains("Enabled -Value 0", mi);                // apply (gaming = Off)
        Assert.Contains("Enabled -Value 1", mi);                // reverse (re-enable)
    }

    [Fact]
    public void ProsConsFor_EveryCatalogEntry_HasAtLeastTwoCompleteChoices()
    {
        foreach (var d in SettingDocsCatalog.All)
        {
            var pc = SettingDocsCatalog.ProsConsFor(d.SettingId);
            Assert.True(pc.Count >= 2,
                $"{d.SettingId} has fewer than 2 pro/con choices -- a user wants to see both sides");
            foreach (var t in pc)
            {
                Assert.False(string.IsNullOrWhiteSpace(t.Choice), $"{d.SettingId} pro/con missing a choice label");
                Assert.False(string.IsNullOrWhiteSpace(t.Pro), $"{d.SettingId} pro/con missing a Pro");
                Assert.False(string.IsNullOrWhiteSpace(t.Con), $"{d.SettingId} pro/con missing a Con");
            }
        }
    }

    [Fact]
    public void ProsConsFor_UnknownId_ReturnsEmpty()
    {
        Assert.Empty(SettingDocsCatalog.ProsConsFor("definitely.not.a.real.setting"));
        Assert.Empty(SettingDocsCatalog.ProsConsFor(null!));
    }

    [Fact]
    public void FormatForExpander_UnknownId_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, SettingDocsCatalog.FormatForExpander("nope"));
    }

    /// <summary>
    /// Asserts the committed docs/SETTINGS-REFERENCE.md matches the live
    /// catalog. If this fails, regenerate via:
    ///   GamerGuardian.exe --gen-docs docs/SETTINGS-REFERENCE.md
    /// </summary>
    [Fact]
    public void SettingsReferenceMd_MatchesCatalogOutput()
    {
        // Walk up from the test binary to the repo root to find the doc.
        // tests\GamerGuardian.Tests\bin\Debug\net8.0-windows10.0.22000.0\
        var dir = AppContext.BaseDirectory;
        string? repoRoot = null;
        for (int i = 0; i < 8 && dir != null; i++)
        {
            if (File.Exists(Path.Combine(dir, "GamerGuardian.sln"))) { repoRoot = dir; break; }
            dir = Path.GetDirectoryName(dir);
        }
        Assert.NotNull(repoRoot);
        var docPath = Path.Combine(repoRoot!, "docs", "SETTINGS-REFERENCE.md");
        Assert.True(File.Exists(docPath),
            $"docs/SETTINGS-REFERENCE.md missing -- run 'GamerGuardian.exe --gen-docs {docPath}'");

        var expected = SettingsReferenceGen.Render().Replace("\r\n", "\n").TrimEnd();
        var actual = File.ReadAllText(docPath).Replace("\r\n", "\n").TrimEnd();
        Assert.True(expected == actual,
            "docs/SETTINGS-REFERENCE.md is out of date. Regenerate via:\n" +
            $"  GamerGuardian.exe --gen-docs \"{docPath}\"\n" +
            "Then commit the regenerated file.");
    }
}
