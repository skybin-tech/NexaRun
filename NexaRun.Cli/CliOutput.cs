using NexaRun.Shared.Models;
using Spectre.Console;

namespace NexaRun.Cli;

public static class CliOutput
{
    public static int Exit(IpcResponse response, bool quiet = false)
    {
        if (!quiet)
        {
            if (response.Success)
                AnsiConsole.MarkupLine($"[green]{Markup.Escape(response.Message)}[/]");
            else
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(response.Message)}[/]");
        }

        return response.Success ? 0 : 1;
    }
}
