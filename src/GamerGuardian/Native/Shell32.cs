using System.Runtime.InteropServices;
using System.Text;

namespace GamerGuardian.Native;

internal static class Shell32
{
    public enum QueryUserNotificationState
    {
        QUNS_NOT_PRESENT = 1,
        QUNS_BUSY = 2,
        QUNS_RUNNING_D3D_FULL_SCREEN = 3,
        QUNS_PRESENTATION_MODE = 4,
        QUNS_ACCEPTS_NOTIFICATIONS = 5,
        QUNS_QUIET_TIME = 6,
        QUNS_APP = 7,
    }

    [DllImport("shell32.dll")]
    public static extern int SHQueryUserNotificationState(out QueryUserNotificationState pquns);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    public static bool IsFullscreenAppActive()
    {
        if (SHQueryUserNotificationState(out var state) == 0)
        {
            if (state == QueryUserNotificationState.QUNS_BUSY
                || state == QueryUserNotificationState.QUNS_RUNNING_D3D_FULL_SCREEN
                || state == QueryUserNotificationState.QUNS_PRESENTATION_MODE
                || state == QueryUserNotificationState.QUNS_APP)
                return true;
        }
        return IsForegroundBorderlessFullscreen();
    }

    public static string? GetForegroundProcessName()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;
            GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == 0) return null;
            using var p = System.Diagnostics.Process.GetProcessById((int)pid);
            return p.ProcessName;
        }
        catch { return null; }
    }

    private static bool IsForegroundBorderlessFullscreen()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;

            var sb = new StringBuilder(64);
            GetClassName(hwnd, sb, sb.Capacity);
            var cn = sb.ToString();
            if (cn is "WorkerW" or "Progman" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd"
                or "MultitaskingViewFrame" or "Windows.UI.Core.CoreWindow")
                return false;

            GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == (uint)System.Diagnostics.Process.GetCurrentProcess().Id) return false;

            if (!GetWindowRect(hwnd, out var winRect)) return false;

            var hMon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (hMon == IntPtr.Zero) return false;

            var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(hMon, ref mi)) return false;

            return winRect.Left <= mi.rcMonitor.Left
                && winRect.Top <= mi.rcMonitor.Top
                && winRect.Right >= mi.rcMonitor.Right
                && winRect.Bottom >= mi.rcMonitor.Bottom;
        }
        catch
        {
            return false;
        }
    }
}
