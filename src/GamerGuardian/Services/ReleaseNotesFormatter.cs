using System.Text;
using System.Text.RegularExpressions;

namespace GamerGuardian.Services;

/// <summary>
/// Turns the Markdown release notes (the GitHub release body, which the workflow
/// sources from <c>CHANGELOG.md</c>) into clean plain text for the in-app update
/// prompt. That prompt's notes area is a plain WPF <c>TextBlock</c> with no
/// Markdown renderer, so without this it showed literal <c>##</c>, <c>**</c>,
/// <c>- </c> and <c>[text](url)</c> markup — which is what "the release notes
/// don't look very good" was about.
///
/// <para>Deliberately small: it strips the markup characters and normalises
/// bullets and blank lines. It is not a full Markdown engine — anything it
/// doesn't recognise is passed through untouched, so the worst case is a line
/// that reads exactly like the source.</para>
/// </summary>
public static class ReleaseNotesFormatter
{
    public static string ToPlainText(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;

        var lines = markdown.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var sb = new StringBuilder();
        int blankRun = 0;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();

            // Drop horizontal rules (---, ***, ___ on their own line).
            if (Regex.IsMatch(line, @"^\s*([-*_])\1{2,}\s*$")) continue;

            line = StripInline(line);

            // Headings: keep the text, drop the leading #'s.
            var heading = Regex.Match(line, @"^\s*#{1,6}\s+(.*)$");
            if (heading.Success) line = heading.Groups[1].Value.Trim();

            // Blockquotes: drop the leading '>' markers, keep the text.
            line = Regex.Replace(line, @"^\s*>+\s?", "");

            // List bullets (-, *, +) become a real bullet glyph.
            var bullet = Regex.Match(line, @"^(\s*)[-*+]\s+(.*)$");
            if (bullet.Success)
                line = bullet.Groups[1].Value + "• " + bullet.Groups[2].Value;

            if (line.Trim().Length == 0)
            {
                // Collapse runs of blank lines to a single separator.
                if (++blankRun > 1) continue;
                sb.Append('\n');
            }
            else
            {
                blankRun = 0;
                sb.Append(line).Append('\n');
            }
        }

        return sb.ToString().Trim();
    }

    private static string StripInline(string line)
    {
        // [text](url) -> text
        line = Regex.Replace(line, @"\[([^\]]+)\]\([^)]*\)", "$1");
        // **bold** / __bold__ -> bold   (before single-marker italics)
        line = Regex.Replace(line, @"\*\*(.+?)\*\*", "$1");
        line = Regex.Replace(line, @"__(.+?)__", "$1");
        // `code` -> code
        line = Regex.Replace(line, @"`([^`]+)`", "$1");
        // *italic* -> italic  (single asterisks only; underscores are left alone
        // so file paths and identifiers like gamerguardian_error.log survive)
        line = Regex.Replace(line, @"\*(?=\S)(.+?)(?<=\S)\*", "$1");
        return line;
    }
}
