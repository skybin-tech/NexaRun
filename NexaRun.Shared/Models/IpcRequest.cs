namespace NexaRun.Shared.Models;

public class IpcRequest
{
    public string Command { get; set; } = string.Empty;
    public string? ProcessName { get; set; }
    public StartOptions? Options { get; set; }
    public int? LogLines { get; set; }
}
