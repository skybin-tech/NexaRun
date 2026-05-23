using System.CommandLine;
using NexaRun.Cli;
using NexaRun.Cli.Display;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;
using Spectre.Console;

namespace NexaRun.Cli.Commands;

public static class RestartAllCommand
{
    public static Command Build(IpcClient client)
    {
        var cmd = new Command("restart-all", "Restart every managed process (one at a time)");

        cmd.SetAction(async _ =>
        {
            var listResponse = await client.Send(new IpcRequest { Command = "list" }, showProgress: false);
            if (!listResponse.Success)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(listResponse.Message)}[/]");
                return 1;
            }

            var processes = listResponse.Processes?.OrderBy(p => p.Id).ToList() ?? [];
            if (processes.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]No processes to restart.[/]");
                return 1;
            }
            var succeeded = 0;
            var failed = 0;

            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(
                [
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn()
                ])
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[yellow]Restart all[/]", maxValue: processes.Count);

                    for (var i = 0; i < processes.Count; i++)
                    {
                        var proc = processes[i];
                        var index = i + 1;
                        var isLast = i == processes.Count - 1;
                        task.Description = Markup.Escape(
                            CliIpc.RestartAllItemStatus(index, processes.Count, proc.Id, proc.Name, isLast));

                        var response = await client.Send(
                            new IpcRequest
                            {
                                Command = "restart",
                                ProcessName = proc.Id.ToString(),
                                SettleAfterStart = isLast
                            },
                            showProgress: false);

                        if (response.Success)
                            succeeded++;
                        else
                        {
                            failed++;
                            AnsiConsole.MarkupLine(
                                $"[red]  {index}/{processes.Count} [{proc.Id}] '{Markup.Escape(proc.Name)}': {Markup.Escape(response.Message)}[/]");
                        }

                        task.Increment(1);
                    }
                });

            var summary =
                $"Restart All finished: {succeeded} succeeded, {failed} failed (of {processes.Count}).";
            if (failed == 0)
                AnsiConsole.MarkupLine($"[green]{Markup.Escape(summary)}[/]");
            else
                AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(summary)}[/]");

            var finalList = await client.Send(new IpcRequest { Command = "list" }, showProgress: false);
            if (finalList.Processes is { Count: > 0 })
                ProcessTable.Render(finalList.Processes);

            return failed == processes.Count ? 1 : 0;
        });

        return cmd;
    }
}
