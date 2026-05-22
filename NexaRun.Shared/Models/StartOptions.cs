using NexaRun.Shared;

namespace NexaRun.Shared.Models;

public class StartOptions
{
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public bool AutoRestart { get; set; } = true;
    public int MaxRestartAttempts { get; set; } = ProcessDefaults.MaxRestartAttempts;
    public double? MaxCpuPercent { get; set; }   // e.g. 80.0 = restart above 80% CPU
    public long? MaxMemoryMb { get; set; }         // e.g. 512 = restart above 512 MB
    public string? OutLogFile { get; set; }
    public string? ErrorLogFile { get; set; }
    public string? CombinedLogFile { get; set; }
    public bool LogTimestamps { get; set; }
    public Dictionary<string, string>? Environment { get; set; }
    public string? Url { get; set; }
}
