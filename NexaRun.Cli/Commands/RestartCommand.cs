using System.CommandLine;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;
using Spectre.Console;

namespace NexaRun.Cli.Commands;

public static class RestartCommand
{
    public static Command Build(IpcClient client)
    {
        var nameArg = new Argument<string>("name", "Name of the process to restart");
        var cmd = new Command("restart", "Restart a process") { nameArg };

        cmd.SetHandler(async (string name) =>
        {
            var response = await AnsiConsole.Status()
                .StartAsync($"Restarting [cyan]{name}[/]...", _ =>
                    client.Send(new IpcRequest { Command = "restart", ProcessName = name }));

            if (response.Success)
                AnsiConsole.MarkupLine($"[green]✓[/] {response.Message}");
            else
                AnsiConsole.MarkupLine($"[red]✗[/] {response.Message}");
        }, nameArg);

        return cmd;
    }
}
