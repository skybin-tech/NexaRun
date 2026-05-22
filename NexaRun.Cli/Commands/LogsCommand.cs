using System.CommandLine;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;
using Spectre.Console;

namespace NexaRun.Cli.Commands;

public static class LogsCommand
{
    public static Command Build(IpcClient client)
    {
        var nameArg = new Argument<string>("name")
        {
            Description = "Name of the process"
        };

        var linesOpt = new Option<int>("--lines")
        {
            Description = "Number of log lines to show",
            DefaultValueFactory = _ => 50
        };

        var outOpt = new Option<bool>("--out")
        {
            Description = "Show stdout log only (outLogFile)"
        };

        var errOpt = new Option<bool>("--err")
        {
            Description = "Show stderr log only (errorLogFile)"
        };

        var followOpt = new Option<bool>("--follow")
        {
            Description = "Stream logs continuously (tail -f style)"
        };

        var cmd = new Command("logs", "Show process logs") { nameArg, linesOpt, outOpt, errOpt, followOpt };

        cmd.SetAction(async parseResult =>
        {
            var name = parseResult.GetValue(nameArg)!;
            var lines = parseResult.GetValue(linesOpt);
            var stream = parseResult.GetValue(errOpt) ? "err"
                : parseResult.GetValue(outOpt) ? "out"
                : null;

            if (parseResult.GetValue(followOpt))
                return await FollowLogs(client, name, lines, stream);

            return await PrintLogs(client, name, lines, stream);
        });

        return cmd;
    }

    private static async Task<int> PrintLogs(IpcClient client, string name, int lines, string? stream)
    {
        var response = await client.Send(new IpcRequest
        {
            Command = "logs",
            ProcessName = name,
            LogLines = lines,
            LogStream = stream
        });

        if (!response.Success)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(response.Message)}[/]");
            return 1;
        }

        AnsiConsole.WriteLine(response.Logs ?? string.Empty);
        return 0;
    }

    private static async Task<int> FollowLogs(IpcClient client, string name, int lines, string? stream)
    {
        var lastLineCount = 0;

        while (true)
        {
            var response = await client.Send(new IpcRequest
            {
                Command = "logs",
                ProcessName = name,
                LogLines = lines,
                LogStream = stream
            });

            if (!response.Success)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(response.Message)}[/]");
                return 1;
            }

            var text = response.Logs ?? string.Empty;
            var currentLines = text.Length == 0 ? 0 : text.Split('\n').Length;

            if (currentLines != lastLineCount)
            {
                Console.Clear();
                AnsiConsole.WriteLine(text);
                lastLineCount = currentLines;
            }

            await Task.Delay(1000);
        }
    }
}
