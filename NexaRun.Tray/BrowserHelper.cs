using System.Diagnostics;

namespace NexaRun.Tray;

internal static class BrowserHelper
{
    public static bool TryOpen(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var target = url.Trim();
        if (!target.Contains("://", StringComparison.Ordinal))
            target = "http://" + target;

        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
        return true;
    }
}
