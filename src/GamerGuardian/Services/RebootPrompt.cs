using GamerGuardian.UI;
using Application = System.Windows.Application;

namespace GamerGuardian.Services;

/// <summary>
/// Shows the single, app-level "a restart is required" prompt. Every path that
/// applies a reboot-requiring change funnels through here — the manual Settings
/// Apply / preset flow and the background auto-apply loop — so the prompt looks
/// and behaves the same no matter what triggered it.
///
/// <para>The window is deliberately <b>unowned</b>. The manual Apply path shows
/// this prompt and then closes the Settings window; a WPF owned window is
/// destroyed together with its owner, so an owned reboot prompt vanished the
/// instant the user clicked "Save &amp; close" after a bulk apply (the Extreme
/// preset is the worst case — it applies several reboot-required settings at
/// once). That is exactly the "applied the changes but never prompted to
/// restart" bug. Unowned, combined with the app's <c>OnExplicitShutdown</c>
/// mode, keeps the prompt alive until the user acts on it.</para>
///
/// <para>Singleton: a later batch that also needs a reboot replaces the prior
/// prompt rather than stacking a second window (same pattern as
/// <see cref="Notifier"/>). The restart action is identical regardless of which
/// settings triggered it, so replacing the descriptions loses nothing.</para>
/// </summary>
public static class RebootPrompt
{
    private static RebootPendingWindow? _current;

    /// <summary>
    /// Surface the restart prompt for the given human-readable setting
    /// descriptions. No-op when the list is empty or the WPF application isn't
    /// running (e.g. headless self-test). Safe to call from any thread — the
    /// window is created on the UI dispatcher.
    /// </summary>
    public static void Show(IReadOnlyList<string> settingDescriptions)
    {
        if (settingDescriptions is null || settingDescriptions.Count == 0) return;
        var app = Application.Current;
        if (app is null) return;

        app.Dispatcher.BeginInvoke(() =>
        {
            try
            {
                _current?.Close();
                var win = new RebootPendingWindow(settingDescriptions);
                _current = win;
                // RebootPendingWindow.OnClosed already releases its visual tree.
                win.Closed += (_, _) => { if (ReferenceEquals(_current, win)) _current = null; };
                win.Show();
            }
            catch { /* best-effort: a failed prompt must never crash the applier */ }
        });
    }
}
