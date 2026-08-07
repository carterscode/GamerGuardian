using GamerGuardian.Services;
using Xunit;

namespace GamerGuardian.Tests;

/// <summary>
/// Covers the Markdown-to-plaintext cleanup that makes the in-app update prompt
/// (a plain TextBlock) render the CHANGELOG-sourced release notes cleanly.
/// </summary>
public class ReleaseNotesFormatterTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n  \n")]
    public void Empty_Input_ReturnsEmpty(string? input)
    {
        Assert.Equal(string.Empty, ReleaseNotesFormatter.ToPlainText(input));
    }

    [Fact]
    public void Headings_LoseTheirHashes()
    {
        var outp = ReleaseNotesFormatter.ToPlainText("## What's new in 0.1.63");
        Assert.Equal("What's new in 0.1.63", outp);
    }

    [Fact]
    public void Bold_And_Italic_Markers_Removed()
    {
        Assert.Equal("Balanced by default", ReleaseNotesFormatter.ToPlainText("**Balanced** by *default*"));
        Assert.Equal("really", ReleaseNotesFormatter.ToPlainText("__really__"));
    }

    [Fact]
    public void Bullets_BecomeGlyphs()
    {
        Assert.Equal("• first\n• second",
            ReleaseNotesFormatter.ToPlainText("- first\n* second"));
    }

    [Fact]
    public void Links_CollapseToText()
    {
        Assert.Equal("GitHub Release",
            ReleaseNotesFormatter.ToPlainText("[GitHub Release](https://github.com/x/y/releases)"));
    }

    [Fact]
    public void Blockquotes_And_InlineCode_Stripped()
    {
        Assert.Equal("Important note", ReleaseNotesFormatter.ToPlainText("> Important note"));
        Assert.Equal("edit CHANGELOG.md now", ReleaseNotesFormatter.ToPlainText("edit `CHANGELOG.md` now"));
    }

    [Fact]
    public void Underscores_InIdentifiers_Preserved()
    {
        // Single-marker italics use '*' only, so underscores in paths survive.
        var outp = ReleaseNotesFormatter.ToPlainText("see %TEMP%\\gamerguardian_error.log");
        Assert.Contains("gamerguardian_error.log", outp);
    }

    [Fact]
    public void HorizontalRules_Dropped_And_BlankRunsCollapsed()
    {
        var outp = ReleaseNotesFormatter.ToPlainText("A\n\n\n---\n\nB");
        Assert.Equal("A\n\nB", outp);
    }

    [Fact]
    public void RealisticSection_ReadsCleanly()
    {
        const string md = """
            ## What's new in 0.1.63

            ### Changed
            - **The recommended Windows power plan is now Balanced**, not
              [High Performance](https://example.com).

            ### Added
            - A **"What this plan changes"** section on the CPU / Power tab.
            """;

        var outp = ReleaseNotesFormatter.ToPlainText(md);

        Assert.DoesNotContain("#", outp);
        Assert.DoesNotContain("**", outp);
        Assert.DoesNotContain("](", outp);
        Assert.Contains("What's new in 0.1.63", outp);
        Assert.Contains("• The recommended Windows power plan is now Balanced", outp);
        Assert.Contains("High Performance", outp);
        Assert.Contains("• A \"What this plan changes\" section", outp);
    }
}
