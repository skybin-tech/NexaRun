using System.CommandLine;
using NexaRun.Cli;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;

namespace NexaRun.Cli.Commands;

public static class ClearAllCommand
{
    public static Command Build(IpcClient client)
    {
        var cmd = new Command("clear-all", "Stop and remove all managed processes");

        cmd.SetAction(async _ =>
        {
            var response = await client.Send(new IpcRequest { Command = "clear-all" });
            return CliOutput.Exit(response);
        });

        return cmd;
    }
}
