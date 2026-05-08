using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace GamerGuardian.Tray;

public sealed class TrayIconHost : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _pauseItem;
    private bool _paused;

    public event Action? OpenSettingsRequested;
    public event Action? CheckNowRequested;
    public event Action? ExitRequested;
    public event Action? PauseToggleRequested;

    public TrayIconHost()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings", null, (_, _) => OpenSettingsRequested?.Invoke());
        menu.Items.Add("Check now", null, (_, _) => CheckNowRequested?.Invoke());
        _pauseItem = new ToolStripMenuItem("Pause monitoring", null, (_, _) => PauseToggleRequested?.Invoke());
        menu.Items.Add(_pauseItem);
        menu.Items.Add(new ToolStripSeparator());
        // 'Quit' is the only path that fully terminates the process. Closing or
        // minimizing the Settings window goes to the tray (App.xaml has
        // ShutdownMode=OnExplicitShutdown).
        menu.Items.Add("Quit", null, (_, _) => ExitRequested?.Invoke());

        _icon = new NotifyIcon
        {
            Icon = LoadAppIcon() ?? SystemIcons.Application,
            Text = "GamerGuardian",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _icon.DoubleClick += (_, _) => OpenSettingsRequested?.Invoke();
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
        _pauseItem.Text = paused ? "Resume monitoring" : "Pause monitoring";
        _icon.Text = paused ? "GamerGuardian (paused)" : "GamerGuardian";
    }

    public void ShowBalloon(string title, string text)
    {
        _icon.BalloonTipTitle = title;
        _icon.BalloonTipText = text;
        _icon.ShowBalloonTip(3000);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }

    private static Icon? LoadAppIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/AppIcon.ico");
            using var stream = System.Windows.Application.GetResourceStream(uri)?.Stream;
            if (stream is null) return null;
            return new Icon(stream, SystemInformation.SmallIconSize);
        }
        catch
        {
            try
            {
                return Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? "");
            }
            catch { return null; }
        }
    }
}
