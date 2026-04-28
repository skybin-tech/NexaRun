using NexaRun.Shared.Models;

namespace NexaRun.Tray.ViewModels;

public class RunHistoryRow(ProcessRun run)
{
    public string StartedLabel { get; } = run.StartedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    public string DurationLabel { get; } = run.Duration.HasValue
        ? FormatDuration(run.Duration.Value)
        : run.Outcome == ProcessRunOutcome.Running ? "running" : "—";
    public string OutcomeLabel { get; } = run.Outcome.ToString();
    public string ExitCodeLabel { get; } = run.ExitCode?.ToString() ?? "—";

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalSeconds < 60)    return $"{ts.TotalSeconds:F0}s";
        if (ts.TotalMinutes < 60)    return $"{ts.TotalMinutes:F0}m {ts.Seconds}s";
        return $"{(int)ts.TotalHours}h {ts.Minutes}m";
    }
}
