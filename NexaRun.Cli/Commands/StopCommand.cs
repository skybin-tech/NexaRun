using System.CommandLine;
using NexaRun.Shared.Ipc;

namespace NexaRun.Cli.Commands;

public static class StopCommand
{
    public static Command Build(IpcClient client)
    {
        var nameArg = new Argument<string>("name") { Description = "Name of the process to stop" };
        var cmd = new Command("stop", "Stop a running process") { nameArg };
        return cmd;
    }
}
