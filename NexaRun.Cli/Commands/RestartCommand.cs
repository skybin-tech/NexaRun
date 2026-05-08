using System.CommandLine;
using NexaRun.Shared.Ipc;

namespace NexaRun.Cli.Commands;

public static class RestartCommand
{
    public static Command Build(IpcClient client)
    {
        var nameArg = new Argument<string>("name") { Description = "Name of the process to restart" };
        var cmd = new Command("restart", "Restart a process") { nameArg };
        return cmd;
    }
}
