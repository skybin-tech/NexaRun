namespace NexaRun.Shared.Models;

public class ProcessRun
{
    public string ProcessName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int? ExitCode { get; set; }
    public ProcessRunOutcome Outcome { get; set; }

    public TimeSpan? Duration => EndedAt.HasValue ? EndedAt.Value - StartedAt : null;
}

public enum ProcessRunOutcome
{
    Running,
    Clean,
    Crashed,
    Stopped
}
