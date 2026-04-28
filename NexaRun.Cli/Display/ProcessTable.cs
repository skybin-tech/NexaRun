using NexaRun.Shared.Models;
using Spectre.Console;

namespace NexaRun.Cli.Display;

public static class ProcessTable
{
    public static void Render(List<NexaProcess> processes)
    {
        if (processes.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No processes registered.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Id[/]")
            .AddColumn("[bold]Name[/]")
            .AddColumn("[bold]Status[/]")
            .AddColumn("[bold]Pid[/]")
            .AddColumn("[bold]Restarts[/]")
            .AddColumn("[bold]Memory[/]")
            .AddColumn("[bold]Uptime[/]");

        foreach (var p in processes)
        {
            var statusMarkup = p.Status switch
            {
                ProcessStatus.Online => "[green]Online[/]",
                ProcessStatus.Stopped => "[grey]Stopped[/]",
                ProcessStatus.Errored => "[red]Errored[/]",
                ProcessStatus.Starting => "[yellow]Starting[/]",
                ProcessStatus.Stopping => "[yellow]Stopping[/]",
                _ => p.Status.ToString()
            };

            var pid = p.Pid?.ToString() ?? "-";
            var memory = p.Status == ProcessStatus.Online ? FormatMemory(p.MemoryUsage) : "-";
            var uptime = p.Status == ProcessStatus.Online ? FormatUptime(p.StartedAt) : "-";

            table.AddRow(
                p.Id.ToString(),
                Markup.Escape(p.Name),
                statusMarkup,
                pid,
                p.Restarts.ToString(),
                memory,
                uptime
            );
        }

        AnsiConsole.Write(table);
    }

    private static string FormatMemory(long bytes)
    {
        if (bytes == 0) return "-";
        if (bytes < 1024 * 1024) return $"{bytes / 1024} KB";
        return $"{bytes / (1024 * 1024)} MB";
    }

    private static string FormatUptime(DateTime startedAt)
    {
        var span = DateTime.UtcNow - startedAt;
        if (span.TotalDays >= 1) return $"{(int)span.TotalDays}d ago";
        if (span.TotalHours >= 1) return $"{(int)span.TotalHours}h ago";
        if (span.TotalMinutes >= 1) return $"{(int)span.TotalMinutes}m ago";
        return $"{(int)span.TotalSeconds}s ago";
    }
}
