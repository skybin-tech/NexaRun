namespace NexaRun.Shared.Models;

public class NexaProcess
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public ProcessStatus Status { get; set; }
    public int? Pid { get; set; }
    public DateTime StartedAt { get; set; }
    public int Restarts { get; set; }
    public double CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
    public bool AutoRestart { get; set; } = true;
    public string LogFile { get; set; } = string.Empty;
    public double? MaxCpuPercent { get; set; }
    public long? MaxMemoryMb { get; set; }
}
