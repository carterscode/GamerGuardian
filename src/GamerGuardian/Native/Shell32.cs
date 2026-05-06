using System.Runtime.InteropServices;

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

    public static bool IsFullscreenAppActive()
    {
        if (SHQueryUserNotificationState(out var state) != 0) return false;
        return state == QueryUserNotificationState.QUNS_BUSY
            || state == QueryUserNotificationState.QUNS_RUNNING_D3D_FULL_SCREEN
            || state == QueryUserNotificationState.QUNS_PRESENTATION_MODE
            || state == QueryUserNotificationState.QUNS_APP;
    }
}
