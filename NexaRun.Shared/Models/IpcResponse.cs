namespace NexaRun.Shared.Models;

public class IpcResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<NexaProcess>? Processes { get; set; }
    public string? Logs { get; set; }
    public List<ProcessRun>? RunHistory { get; set; }
}
