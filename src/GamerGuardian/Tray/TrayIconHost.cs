using System.Drawing;
using System.Windows.Forms;

namespace GamerGuardian.Tray;

public sealed class TrayIconHost : IDisposable
{
    private readonly NotifyIcon _icon;

    public event Action? OpenSettingsRequested;
    public event Action? CheckNowRequested;
    public event Action? ExitRequested;

    public TrayIconHost()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings", null, (_, _) => OpenSettingsRequested?.Invoke());
        menu.Items.Add("Check now", null, (_, _) => CheckNowRequested?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke());

        _icon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "GamerGuardian",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _icon.DoubleClick += (_, _) => OpenSettingsRequested?.Invoke();
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
}
