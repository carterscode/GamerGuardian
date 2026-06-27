using System.Text;
using GamerGuardian.Models;

namespace GamerGuardian.Services;

/// <summary>
/// Renders <see cref="SettingDocsCatalog"/> entries as markdown for
/// <c>docs/SETTINGS-REFERENCE.md</c>. Used by the App entrypoint when
/// invoked with <c>--gen-docs</c> and by a unit test that asserts the
/// committed file matches the catalog (so the docs and the code can't drift).
/// </summary>
public static class SettingsReferenceGen
{
    public static string Render()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# GamerGuardian settings reference");
        sb.AppendLine();
        sb.AppendLine("This document is **generated from [`SettingDocsCatalog.cs`](../src/GamerGuardian/Services/SettingDocsCatalog.cs)**. Edit the catalog, then run `GamerGuardian.exe --gen-docs` to regenerate. A unit test asserts that the committed file matches the catalog so they can't drift.");
        sb.AppendLine();
        sb.AppendLine("Every setting here is managed via the Settings window. Toggle **Monitor** to have GamerGuardian watch the value; toggle **Auto-apply silently** to have it auto-correct on drift. Both default off, so nothing changes until you opt in.");
        sb.AppendLine();
        sb.AppendLine("## Contents");
        sb.AppendLine();

        bool IsGlobal(string id) =>
            !id.StartsWith("service:") && !id.StartsWith("ai.app:") && !id.StartsWith("ai.")
            && !id.StartsWith("privacy.") && !id.StartsWith("network.") && !id.StartsWith("debloat.")
            && !id.StartsWith("task:");

        var globals = Order(SettingDocsCatalog.All.Where(d => IsGlobal(d.SettingId)));
        var privacy = Order(SettingDocsCatalog.All.Where(d => d.SettingId.StartsWith("privacy.")));
        var debloat = Order(SettingDocsCatalog.All.Where(d => d.SettingId.StartsWith("debloat.")));
        var network = Order(SettingDocsCatalog.All.Where(d => d.SettingId.StartsWith("network.")));
        var ai = Order(SettingDocsCatalog.All.Where(d => d.SettingId.StartsWith("ai.") && !d.SettingId.StartsWith("ai.app:")));
        var aiApps = Order(SettingDocsCatalog.All.Where(d => d.SettingId.StartsWith("ai.app:")));
        var services = Order(SettingDocsCatalog.All.Where(d => d.SettingId.StartsWith("service:")));
        var tasks = Order(SettingDocsCatalog.All.Where(d => d.SettingId.StartsWith("task:")));

        Toc(sb, "Global gaming + display", globals);
        Toc(sb, "Privacy", privacy);
        Toc(sb, "Debloat (ads, nags & background bloat)", debloat);
        Toc(sb, "Network", network);
        Toc(sb, "Windows AI policies", ai);
        Toc(sb, "Windows AI UWP packages", aiApps);
        Toc(sb, "Windows services", services);
        Toc(sb, "Windows scheduled tasks", tasks);

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        Section(sb, "Global gaming + display", globals);
        Section(sb, "Privacy", privacy);
        Section(sb, "Debloat (ads, nags & background bloat)", debloat);
        Section(sb, "Network", network);
        Section(sb, "Windows AI policies", ai);
        Section(sb, "Windows AI UWP packages", aiApps);
        Section(sb, "Windows services", services);
        Section(sb, "Windows scheduled tasks", tasks);

        return sb.ToString();
    }

    private static void Section(StringBuilder sb, string title, IReadOnlyCollection<SettingDetails> entries)
    {
        if (entries.Count == 0) return;
        sb.AppendLine($"## {title}");
        foreach (var d in entries) Render(sb, d);
    }

    private static IReadOnlyList<SettingDetails> Order(IEnumerable<SettingDetails> src)
        => src.OrderBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();

    private static void Toc(StringBuilder sb, string section, IReadOnlyCollection<SettingDetails> entries)
    {
        if (entries.Count == 0) return;
        sb.AppendLine($"**{section}**");
        sb.AppendLine();
        foreach (var d in entries)
            sb.AppendLine($"- [{d.DisplayName}](#{Slug(d.DisplayName)}) (`{d.SettingId}`)");
        sb.AppendLine();
    }

    private static void Render(StringBuilder sb, SettingDetails d)
    {
        sb.AppendLine();
        sb.AppendLine($"### {d.DisplayName}");
        sb.AppendLine();
        sb.AppendLine($"`{d.SettingId}` &nbsp; **Recommended:** {d.Recommended}");
        sb.AppendLine();
        sb.AppendLine($"**Why this is the recommendation.** {d.Why}");
        sb.AppendLine();
        sb.AppendLine($"**What it does.** {d.What}");
        sb.AppendLine();
        sb.AppendLine($"**How it helps.** {d.HowItHelps}");
        sb.AppendLine();

        var prosCons = SettingDocsCatalog.ProsConsFor(d.SettingId);
        if (prosCons.Count > 0)
        {
            sb.AppendLine("**Pros & cons of each choice:**");
            sb.AppendLine();
            sb.AppendLine("| Choice | Pro | Con |");
            sb.AppendLine("|---|---|---|");
            foreach (var t in prosCons)
                sb.AppendLine($"| {Cell(t.Choice)} | {Cell(t.Pro)} | {Cell(t.Con)} |");
            sb.AppendLine();
        }

        sb.AppendLine("**Per-scenario recommendation:**");
        sb.AppendLine();
        sb.AppendLine("| Scenario | Setting |");
        sb.AppendLine("|---|---|");
        foreach (var (scenario, rec) in d.Scenarios)
            sb.AppendLine($"| {Cell(scenario)} | {Cell(rec)} |");
        sb.AppendLine();
        sb.AppendLine($"**Risks.** {d.Risks}");
        sb.AppendLine();

        Commands(sb, d);

        sb.AppendLine($"**Reversible via.** {d.ReversibleVia}");
        sb.AppendLine();
    }

    /// <summary>Verify / apply / reverse PowerShell, in a fenced block when present.</summary>
    private static void Commands(StringBuilder sb, SettingDetails d)
    {
        var verify = SettingDocs.VerifyCommandFor(d.SettingId);
        var apply = SettingDocs.ApplyCommandFor(d.SettingId, GamingRaw(d.SettingId));
        var reverse = SettingDocs.ReverseCommandFor(d.SettingId);
        if (string.IsNullOrWhiteSpace(reverse)) reverse = d.ReversibleVia;

        sb.AppendLine("**Command line (PowerShell):**");
        sb.AppendLine();
        sb.AppendLine("```powershell");
        if (!string.IsNullOrWhiteSpace(verify))
        {
            sb.AppendLine("# Check the current value");
            sb.AppendLine(verify);
            sb.AppendLine();
        }
        if (!string.IsNullOrWhiteSpace(apply))
        {
            sb.AppendLine("# Apply the gaming-optimized value");
            sb.AppendLine(apply);
            sb.AppendLine();
        }
        sb.AppendLine("# Reverse it (restore the Windows default)");
        sb.AppendLine(reverse);
        sb.AppendLine("```");
        sb.AppendLine();
    }

    // Memory Integrity's apply fallback is the safe (On) value, not the gaming
    // (Off) one -- ask for Off explicitly so the doc shows the gaming command.
    private static string GamingRaw(string settingId) =>
        settingId == "memintegrity" ? "0" : string.Empty;

    /// <summary>Escape a value for a Markdown table cell (pipes + newlines).</summary>
    private static string Cell(string s) =>
        s.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

    private static string Slug(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c == ' ' || c == '-' || c == '_' || c == '/') sb.Append('-');
            // drop everything else (punctuation, slashes, parens)
        }
        return sb.ToString();
    }
}
