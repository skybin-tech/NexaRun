using System.CommandLine;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;
using Spectre.Console;

namespace NexaRun.Cli.Commands;

public static class DeleteCommand
{
    public static Command Build(IpcClient client)
    {
        var nameArg = new Argument<string>("name", "Name of the process to delete");
        var cmd = new Command("delete", "Remove a process from the managed list") { nameArg };

        cmd.SetHandler(async (string name) =>
        {
            var response = await client.Send(new IpcRequest { Command = "delete", ProcessName = name });

            if (response.Success)
                AnsiConsole.MarkupLine($"[green]✓[/] {response.Message}");
            else
                AnsiConsole.MarkupLine($"[red]✗[/] {response.Message}");
        }, nameArg);

        return cmd;
    }
}
