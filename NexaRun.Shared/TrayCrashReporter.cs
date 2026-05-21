using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace NexaRun.Shared;

public static class TrayCrashReporter
{
    public const string EventSourceName = "NexaRun-Tray";
    public const string EventLogName = "Application";

    public static string TrayCrashLogFile { get; } = Path.Combine(NexaRunPaths.LogDir, "tray-crash.log");

    public static void RegisterGlobalHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Report("AppDomain.UnhandledException", e.ExceptionObject as Exception, e.IsTerminating);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Report("TaskScheduler.UnobservedTaskException", e.Exception, isTerminating: false);
            e.SetObserved();
        };
    }

    public static void Report(string context, Exception? ex, bool isTerminating)
    {
        var message = Format(context, ex, isTerminating);
        WriteCrashFile(message);
        WriteEventLog(message, isTerminating);
    }

    [SupportedOSPlatform("windows")]
    public static void EnsureEventSourceRegistered()
    {
        try
        {
            if (!EventLog.SourceExists(EventSourceName))
                EventLog.CreateEventSource(EventSourceName, EventLogName);
        }
        catch
        {
            // Requires admin; installer registers the source. File log still works.
        }
    }

    private static string Format(string context, Exception? ex, bool isTerminating)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"NexaRun Tray — {context}");
        sb.AppendLine($"Time (UTC): {DateTime.UtcNow:O}");
        sb.AppendLine($"Machine: {Environment.MachineName}");
        sb.AppendLine($"User: {Environment.UserName}");
        sb.AppendLine($"Version: {Environment.Version}");
        sb.AppendLine($"Terminating: {isTerminating}");
        sb.AppendLine($"Executable: {Environment.ProcessPath ?? AppContext.BaseDirectory}");

        if (ex != null)
            AppendException(sb, ex);

        return sb.ToString();
    }

    private static void AppendException(StringBuilder sb, Exception ex, int depth = 0)
    {
        var indent = new string(' ', depth * 2);
        sb.AppendLine($"{indent}{ex.GetType().FullName}: {ex.Message}");
        if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            sb.AppendLine($"{indent}{ex.StackTrace}");

        if (ex.InnerException != null)
        {
            sb.AppendLine($"{indent}--- Inner ---");
            AppendException(sb, ex.InnerException, depth + 1);
        }
    }

    private static void WriteCrashFile(string message)
    {
        try
        {
            NexaRunPaths.EnsureDirectories();
            var entry = $"{new string('-', 72)}{Environment.NewLine}{message}{Environment.NewLine}";
            File.AppendAllText(TrayCrashLogFile, entry, Encoding.UTF8);
        }
        catch
        {
            // Last resort — cannot throw from crash handler
        }
    }

    private static void WriteEventLog(string message, bool isTerminating)
    {
        if (!OperatingSystem.IsWindows()) return;
        WriteEventLogWindows(message, isTerminating);
    }

    [SupportedOSPlatform("windows")]
    private static void WriteEventLogWindows(string message, bool isTerminating)
    {
        try
        {
            if (!EventLog.SourceExists(EventSourceName))
            {
                WriteEventLogFallback(message, isTerminating);
                return;
            }

            var type = isTerminating ? EventLogEntryType.Error : EventLogEntryType.Warning;
            const int max = 30000;
            if (message.Length > max)
                message = message[..max] + Environment.NewLine + "(truncated)";

            EventLog.WriteEntry(EventSourceName, message, type, isTerminating ? 1001 : 1000);
        }
        catch
        {
            WriteEventLogFallback(message, isTerminating);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void WriteEventLogFallback(string message, bool isTerminating)
    {
        try
        {
            using var log = new EventLog(EventLogName);
            log.Source = EventLogName;
            var type = isTerminating ? EventLogEntryType.Error : EventLogEntryType.Warning;
            var text = $"[{EventSourceName}] {message}";
            if (text.Length > 30000)
                text = text[..30000];
            log.WriteEntry(text, type);
        }
        catch
        {
            // Ignore — file log remains
        }
    }
}
