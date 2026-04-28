using System.CommandLine;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;
using Spectre.Console;

namespace NexaRun.Cli.Commands;

public static class StartCommand
{
    public static Command Build(IpcClient client)
    {
        var execArg = new Argument<string>("executable", "Executable path or command to run");
        var nameOpt = new Option<string?>("--name", "Friendly name for the process");
        var argsOpt = new Option<string>("--args", () => string.Empty, "Arguments to pass to the executable");
        var cwdOpt = new Option<string?>("--cwd", "Working directory");
        var noRestartOpt = new Option<bool>("--no-autorestart", "Disable auto-restart on crash");

        var cmd = new Command("start", "Start a process and register it with the daemon")
        {
            execArg, nameOpt, argsOpt, cwdOpt, noRestartOpt
        };

        cmd.SetHandler(async (string exec, string? name, string args, string? cwd, bool noRestart) =>
        {
            var processName = name ?? Path.GetFileNameWithoutExtension(exec);

            var response = await AnsiConsole.Status()
                .StartAsync($"Starting [cyan]{processName}[/]...", _ =>
                    client.Send(new IpcRequest
                    {
                        Command = "start",
                        Options = new StartOptions
                        {
                            Name = processName,
                            ExecutablePath = exec,
                            Arguments = args,
                            WorkingDirectory = cwd ?? string.Empty,
                            AutoRestart = !noRestart
                        }
                    }));

            if (response.Success)
                AnsiConsole.MarkupLine($"[green]✓[/] {response.Message}");
            else
                AnsiConsole.MarkupLine($"[red]✗[/] {response.Message}");
        }, execArg, nameOpt, argsOpt, cwdOpt, noRestartOpt);

        return cmd;
    }
}
