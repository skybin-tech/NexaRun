using Avalonia;
using NexaRun.Tray;

AppBuilder.Configure<App>()
    .UsePlatformDetect()

    .StartWithClassicDesktopLifetime(args);
