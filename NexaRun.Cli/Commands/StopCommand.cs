using System.CommandLine;
using NexaRun.Cli;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;

namespace NexaRun.Cli.Commands;

public static class StopCommand
{
    public static Command Build(IpcClient client)
    {
        var nameArg = new Argument<string>("name")
        {
            Description = "Name of the process to stop"
        };

        var cmd = new Command("stop", "Stop a running process") { nameArg };

        cmd.SetAction(async parseResult =>
        {
            var name = parseResult.GetValue(nameArg)!;
            var response = await client.Send(new IpcRequest { Command = "stop", ProcessName = name });
            return CliOutput.Exit(response);
        });

        return cmd;
    }
}
