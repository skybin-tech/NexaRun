using System.CommandLine;
using NexaRun.Shared.Config;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;
using Spectre.Console;

namespace NexaRun.Cli.Commands;

public static class ImportCommand
{
    public static Command Build(IpcClient client)
    {
        var fileArg = new Argument<string>("file")
        {
            Description = "Path to nexarun-processes.json (or any NexaRun export .json)"
        };

        var onlyOpt = new Option<string?>("--only")
        {
            Description = "Import and start only this app name from the file"
        };

        var noStartOpt = new Option<bool>("--no-start")
        {
            Description = "Register processes without starting them"
        };

        var cmd = new Command("import", "Import processes from a JSON file") { fileArg, onlyOpt, noStartOpt };

        cmd.SetAction(async parseResult =>
        {
            var path = Path.GetFullPath(parseResult.GetValue(fileArg)!);
            if (!File.Exists(path))
            {
                AnsiConsole.MarkupLine($"[red]File not found: {Markup.Escape(path)}[/]");
                return 1;
            }

            try
            {
                var options = ProcessConfigTransfer.FromImportFile(path, parseResult.GetValue(onlyOpt));
                var response = await client.Send(new IpcRequest
                {
                    Command = "import",
                    BatchOptions = options,
                    StartAfterImport = !parseResult.GetValue(noStartOpt)
                });

                if (response.Success)
                    AnsiConsole.MarkupLine($"[green]{Markup.Escape(response.Message)}[/]");
                else
                    AnsiConsole.MarkupLine($"[red]{Markup.Escape(response.Message)}[/]");

                return response.Success ? 0 : 1;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
                return 1;
            }
        });

        return cmd;
    }
}
