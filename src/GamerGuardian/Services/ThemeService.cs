using System.Windows;
// WinForms is referenced for the tray icon, so these bare names are ambiguous.
using Color = System.Windows.Media.Color;
using Application = System.Windows.Application;
using GamerGuardian.Models;
using Microsoft.Win32;
using Wpf.Ui.Appearance;

namespace GamerGuardian.Services;

public static class ThemeService
{
    /// <summary>
    /// GitHub Primer's accent blue (<c>accent.emphasis</c>). Handed to WPF-UI's
    /// accent manager, which derives the light/dark variants the control templates
    /// read, so accented surfaces match the palette dictionary rather than fighting
    /// it.
    /// </summary>
    private static readonly Color GitHubAccent = Color.FromRgb(0x1F, 0x6F, 0xEB);

    internal static readonly Uri GitHubDarkPaletteUri =
        new("pack://application:,,,/UI/Themes/GitHubDark.xaml", UriKind.Absolute);

    public static void Apply(AppThemeChoice choice)
    {
        var theme = choice switch
        {
            AppThemeChoice.Light => ApplicationTheme.Light,
            AppThemeChoice.Dark => ApplicationTheme.Dark,
            _ => GetSystemTheme(),
        };
        ApplicationThemeManager.Apply(theme);
        ApplyPalette(theme);
    }

    /// <summary>Keys this service has written at the application level, so the light
    /// theme can put them back exactly as they were.</summary>
    private static readonly List<object> AppliedKeys = new();

    /// <summary>
    /// Layers the GitHub dark palette over WPF-UI's dark theme, or strips it for
    /// light.
    ///
    /// <para>The entries are copied into <c>Application.Resources</c> itself rather
    /// than merged as a dictionary. Merging is not enough: WPF looks in a
    /// dictionary's own entries before any of its merged ones, and WPF-UI writes
    /// part of the window chrome — the title bar, the navigation pane and the footer
    /// — straight into the application resources. A merged palette recoloured the
    /// content area and left those three grey. Writing at the same level wins, and
    /// removing the keys again restores whatever the theme dictionary underneath
    /// says, which is what the light theme needs.</para>
    ///
    /// <para>This has to run after every <see cref="ApplicationThemeManager.Apply"/>,
    /// not once at startup, because Apply swaps the theme dictionary on each change.</para>
    ///
    /// <para>Light is left stock on purpose: the request was for GitHub's dark
    /// scheme, and a half-translated light palette would look worse than the theme
    /// WPF-UI already ships.</para>
    /// </summary>
    private static void ApplyPalette(ApplicationTheme theme)
    {
        var app = Application.Current;
        if (app is null) return;   // unit tests and design time have no Application

        foreach (var key in AppliedKeys) app.Resources.Remove(key);
        AppliedKeys.Clear();

        if (theme != ApplicationTheme.Dark) return;

        // Accent first: this one legitimately writes at the application level, and
        // the palette below deliberately overrides some of what it derives (WPF-UI
        // puts black text on accent, which is unreadable on Primer's darker blue).
        ApplicationAccentColorManager.Apply(GitHubAccent, theme);

        var palette = new ResourceDictionary { Source = GitHubDarkPaletteUri };
        foreach (System.Collections.DictionaryEntry entry in palette)
        {
            app.Resources[entry.Key] = entry.Value;
            AppliedKeys.Add(entry.Key);
        }
    }

    public static ApplicationTheme GetSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                writable: false);
            if (key?.GetValue("AppsUseLightTheme") is int v)
                return v == 0 ? ApplicationTheme.Dark : ApplicationTheme.Light;
        }
        catch { }
        return ApplicationTheme.Dark;
    }
}
