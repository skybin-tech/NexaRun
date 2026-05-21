using System.CommandLine;
using NexaRun.Cli;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;

namespace NexaRun.Cli.Commands;

public static class RestartCommand
{
    public static Command Build(IpcClient client)
    {
        var nameArg = new Argument<string>("name")
        {
            Description = "Name of the process to restart"
        };

        var cmd = new Command("restart", "Restart a process") { nameArg };

        cmd.SetAction(async parseResult =>
        {
            var name = parseResult.GetValue(nameArg)!;
            var response = await client.Send(new IpcRequest { Command = "restart", ProcessName = name });
            return CliOutput.Exit(response);
        });

        return cmd;
    }
}
