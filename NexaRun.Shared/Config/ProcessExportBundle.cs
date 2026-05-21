using NexaRun.Shared;

namespace NexaRun.Shared.Config;

public class ProcessExportBundle
{
    public int Version { get; set; } = 1;
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public string? ExportedFrom { get; set; }
    public List<ProcessDefinition> Apps { get; set; } = [];
}

public class ProcessDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public bool AutoRestart { get; set; } = true;
    public int MaxRestartAttempts { get; set; } = ProcessDefaults.MaxRestartAttempts;
    public double? MaxCpuPercent { get; set; }
    public long? MaxMemoryMb { get; set; }
    public string? OutLogFile { get; set; }
    public string? ErrorLogFile { get; set; }
    public string? LogFile { get; set; }
    public bool LogTimestamps { get; set; }
    public Dictionary<string, string>? Environment { get; set; }
}
