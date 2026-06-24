using System;
using Avalonia;
using NexaRun.Shared;
using NexaRun.Tray;

namespace NexaRun.Tray;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        TrayCrashReporter.RegisterGlobalHandlers();
        if (OperatingSystem.IsWindows())
            TrayCrashReporter.EnsureEventSourceRegistered();

        using var singleInstance = new Mutex(true, @"Global\NexaRun.Tray.SingleInstance", out var createdNew);
        if (!createdNew)
            return;

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            TrayCrashReporter.Report("Main", ex, isTerminating: true);
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
