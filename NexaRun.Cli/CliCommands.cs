using System.CommandLine;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;

namespace NexaRun.Cli;

public static class CliCommands
{
    public static Argument<string> TargetArgument(string commandVerb) =>
        new("id|name")
        {
            Description = $"Process id or name (see nexarun list), e.g. nexarun {commandVerb} 0"
        };

    public static IpcRequest TargetRequest(string command, string target) =>
        new() { Command = command, ProcessName = target };
}
