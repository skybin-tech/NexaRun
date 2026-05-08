using System.CommandLine;
using NexaRun.Shared.Ipc;

namespace NexaRun.Cli.Commands;

public static class ListCommand
{
    public static Command Build(IpcClient client)
    {
        var cmd = new Command("list", "List all managed processes");
        return cmd;
    }
}
