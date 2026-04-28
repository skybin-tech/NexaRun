using Avalonia.Media;
using NexaRun.Shared.Models;

namespace NexaRun.Tray.ViewModels;

public class DashboardProcessItem(NexaProcess p)
{
    public string Name { get; } = p.Name;
    public string StatusLabel { get; } = p.Status.ToString();
    public string StatusColor { get; } = p.Status switch
    {
        ProcessStatus.Online   => "#22c55e",
        ProcessStatus.Errored  => "#ef4444",
        ProcessStatus.Starting => "#eab308",
        ProcessStatus.Stopping => "#f97316",
        _                      => "#6b7280"
    };
    public string Memory { get; } = p.MemoryUsage > 0
        ? p.MemoryUsage >= 1_048_576
            ? $"{p.MemoryUsage / 1_048_576.0:F1} MB"
            : $"{p.MemoryUsage / 1024.0:F0} KB"
        : "—";
    public string Pid { get; } = p.Pid?.ToString() ?? "—";
    public int Restarts { get; } = p.Restarts;
    public NexaProcess Source { get; } = p;
}
