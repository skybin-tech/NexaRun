using NexaRun.Shared.Models;

namespace NexaRun.Tray.ViewModels;

public class ProcessRow(NexaProcess p)
{
    public int Id { get; } = p.Id;
    public string Name { get; } = p.Name;
    public string Status { get; } = p.Status.ToString();
    public string Pid { get; } = p.Pid?.ToString() ?? "-";
    public int Restarts { get; } = p.Restarts;
    public string Memory { get; } = FormatMemory(p.MemoryUsage);
    public string Uptime { get; } = p.Status == ProcessStatus.Online ? FormatUptime(p.StartedAt) : "-";
    public bool IsOnline { get; } = p.Status == ProcessStatus.Online;
    public NexaProcess Source { get; } = p;

    private static string FormatMemory(long bytes)
    {
        if (bytes <= 0) return "-";
        if (bytes < 1024 * 1024) return $"{bytes / 1024} KB";
        return $"{bytes / (1024 * 1024)} MB";
    }

    private static string FormatUptime(DateTime startedAt)
    {
        var span = DateTime.UtcNow - startedAt;
        if (span.TotalDays >= 1) return $"{(int)span.TotalDays}d";
        if (span.TotalHours >= 1) return $"{(int)span.TotalHours}h";
        if (span.TotalMinutes >= 1) return $"{(int)span.TotalMinutes}m";
        return $"{(int)span.TotalSeconds}s";
    }
}
