using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace NexaRun.Tray.Helpers;

public static class TrayIconHelper
{
    private const string IconResource = "avares://NexaRun/Assets/NexaRun.ico";

    public static WindowIcon LoadIcon()
    {
        using var stream = AssetLoader.Open(new Uri(IconResource));
        return new WindowIcon(new Bitmap(stream));
    }
}
