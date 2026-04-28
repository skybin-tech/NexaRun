using System.CommandLine;
using NexaRun.Cli.Display;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;
using Spectre.Console;

namespace NexaRun.Cli.Commands;

public static class ListCommand
{
    public static Command Build(IpcClient client)
    {
        var cmd = new Command("list", "List all managed processes");

        cmd.SetHandler(async () =>
        {
            var response = await client.Send(new IpcRequest { Command = "list" });

            if (!response.Success)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] {Markup.Escape(response.Message)}");
                return;
            }

            ProcessTable.Render(response.Processes ?? []);
        });

        return cmd;
    }
}
