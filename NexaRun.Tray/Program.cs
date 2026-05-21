using Avalonia;
using NexaRun.Shared;
using NexaRun.Tray;

TrayCrashReporter.RegisterGlobalHandlers();
if (OperatingSystem.IsWindows())
    TrayCrashReporter.EnsureEventSourceRegistered();

try
{
    AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace()
        .StartWithClassicDesktopLifetime(args);
}
catch (Exception ex)
{
    TrayCrashReporter.Report("Main", ex, isTerminating: true);
    throw;
}
