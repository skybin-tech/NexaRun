using NexaRun.Shared.Config;

namespace NexaRun.Shared.Models;

public class IpcRequest
{
    public string Command { get; set; } = string.Empty;
    public string? ProcessName { get; set; }
    public StartOptions? Options { get; set; }
    public int? LogLines { get; set; }
    public string? LogStream { get; set; }
    public List<StartOptions>? BatchOptions { get; set; }
    public bool StartAfterImport { get; set; }
    /// <summary>After restart/start, wait for port settle (default true). Restart-all passes false except on the last process.</summary>
    public bool SettleAfterStart { get; set; } = true;
    public NexaRunSettings? Settings { get; set; }
}
