namespace NexaRun.Shared.Models;

public class StartOptions
{
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public bool AutoRestart { get; set; } = true;
    public double? MaxCpuPercent { get; set; }   // e.g. 80.0 = restart above 80% CPU
    public long? MaxMemoryMb { get; set; }         // e.g. 512 = restart above 512 MB
}
