using System.Diagnostics;

namespace NexaRun.Tray.Helpers;

public static class WindowsShellHelper
{
    /// <summary>
    /// The notification area requires an interactive shell (Explorer). Server Core and
    /// non-interactive install sessions do not provide one.
    /// </summary>
    public static bool IsNotificationAreaAvailable()
    {
        if (!OperatingSystem.IsWindows())
            return true;

        try
        {
            return Process.GetProcessesByName("explorer").Length > 0;
        }
        catch
        {
            return false;
        }
    }
}
