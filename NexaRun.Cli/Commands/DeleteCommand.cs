using System.CommandLine;
using NexaRun.Shared.Ipc;

namespace NexaRun.Cli.Commands;

public static class DeleteCommand
{
    public static Command Build(IpcClient client)
    {
        var nameArg = new Argument<string>("name") { Description = "Name of the process to delete" };
        var cmd = new Command("delete", "Remove a process from the managed list") { nameArg };
        return cmd;
    }
}
