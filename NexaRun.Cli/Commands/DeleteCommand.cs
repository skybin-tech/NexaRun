using System.CommandLine;
using NexaRun.Cli;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;

namespace NexaRun.Cli.Commands;

public static class DeleteCommand
{
    public static Command Build(IpcClient client)
    {
        var nameArg = new Argument<string>("name")
        {
            Description = "Name of the process to delete"
        };

        var cmd = new Command("delete", "Remove a process from the managed list") { nameArg };

        cmd.SetAction(async parseResult =>
        {
            var name = parseResult.GetValue(nameArg)!;
            var response = await client.Send(new IpcRequest { Command = "delete", ProcessName = name });
            return CliOutput.Exit(response);
        });

        return cmd;
    }
}
