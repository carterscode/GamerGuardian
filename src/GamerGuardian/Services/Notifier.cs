using GamerGuardian.Models;
using GamerGuardian.UI;
using Application = System.Windows.Application;

namespace GamerGuardian.Services;

public sealed class Notifier
{
    private NotificationWindow? _current;

    public Task ShowAsync(DriftReport report)
    {
        if (!report.HasDrift) return Task.CompletedTask;

        var tcs = new TaskCompletionSource();
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            try
            {
                _current?.Close();
                var win = new NotificationWindow(report);
                _current = win;
                win.Closed += (_, _) =>
                {
                    if (ReferenceEquals(_current, win)) _current = null;
                    tcs.TrySetResult();
                };
                win.Show();
            }
            catch
            {
                tcs.TrySetResult();
            }
        });
        return tcs.Task;
    }
}
