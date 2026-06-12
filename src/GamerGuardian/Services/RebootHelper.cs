using System.Diagnostics;

namespace GamerGuardian.Services;

/// <summary>
/// Restarts Windows immediately. /f force-closes open applications without
/// prompting to save (with /t 0 the force flag is NOT implied, so it must be
/// explicit — otherwise any app with unsaved state can block the restart and
/// it silently never happens).
/// </summary>
public static class RebootHelper
{
    public static void ForceRebootNow()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "shutdown.exe",
                Arguments = "/r /f /t 0",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Could not start the reboot: {ex.Message}\n\nYou can reboot manually instead.",
                "GamerGuardian",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }
}
