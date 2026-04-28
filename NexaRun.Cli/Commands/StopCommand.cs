using System.CommandLine;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;
using Spectre.Console;

namespace NexaRun.Cli.Commands;

public static class StopCommand
{
    public static Command Build(IpcClient client)
    {
        var nameArg = new Argument<string>("name", "Name of the process to stop");
        var cmd = new Command("stop", "Stop a running process") { nameArg };

        cmd.SetHandler(async (string name) =>
        {
            var response = await AnsiConsole.Status()
                .StartAsync($"Stopping [cyan]{name}[/]...", _ =>
                    client.Send(new IpcRequest { Command = "stop", ProcessName = name }));

            if (response.Success)
                AnsiConsole.MarkupLine($"[green]✓[/] {response.Message}");
            else
                AnsiConsole.MarkupLine($"[red]✗[/] {response.Message}");
        }, nameArg);

        return cmd;
    }
}
