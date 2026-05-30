using System.CommandLine;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;

namespace NexaRun.Cli;

public static class CliCommands
{
    public static Argument<string> TargetArgument(string commandVerb, string? extraHint = null)
    {
        var hint = string.IsNullOrWhiteSpace(extraHint) ? string.Empty : $" {extraHint.Trim()}";
        return new Argument<string>("id|name")
        {
            Description = $"Process id or name (see nexarun list), e.g. nexarun {commandVerb} 0{hint}"
        };
    }

    public static IpcRequest TargetRequest(string command, string target) =>
        new() { Command = command, ProcessName = target };
}
