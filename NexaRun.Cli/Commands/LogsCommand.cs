using System.CommandLine;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;
using Spectre.Console;

namespace NexaRun.Cli.Commands;

public static class LogsCommand
{
    public static Command Build(IpcClient client)
    {
        var nameArg = new Argument<string>("name", "Name of the process");
        var linesOpt = new Option<int>("--lines", () => 50, "Number of log lines to show");

        var cmd = new Command("logs", "Show recent log output for a process") { nameArg, linesOpt };

        cmd.SetHandler(async (string name, int lines) =>
        {
            var response = await client.Send(new IpcRequest
            {
                Command = "logs",
                ProcessName = name,
                LogLines = lines
            });

            if (!response.Success)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] {Markup.Escape(response.Message)}");
                return;
            }

            AnsiConsole.WriteLine(response.Logs ?? "(no logs)");
        }, nameArg, linesOpt);

        return cmd;
    }
}
