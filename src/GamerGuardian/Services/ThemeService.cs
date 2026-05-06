using GamerGuardian.Models;
using Microsoft.Win32;
using Wpf.Ui.Appearance;

namespace GamerGuardian.Services;

public static class ThemeService
{
    public static void Apply(AppThemeChoice choice)
    {
        var theme = choice switch
        {
            AppThemeChoice.Light => ApplicationTheme.Light,
            AppThemeChoice.Dark => ApplicationTheme.Dark,
            _ => GetSystemTheme(),
        };
        ApplicationThemeManager.Apply(theme);
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
