using System.CommandLine;
using NexaRun.Cli;
using NexaRun.Shared;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;

namespace NexaRun.Cli.Commands;

public static class StartCommand
{
    public static Command Build(IpcClient client)
    {
        var execArg = new Argument<string>("executable")
        {
            Description = "Executable or command (npm, dotnet, node, etc.)"
        };

        var nameOpt = new Option<string?>("--name")
        {
            Description = "Process name (defaults to executable name)"
        };

        var argsOpt = new Option<string?>("--args")
        {
            Description = "Arguments passed to the executable"
        };

        var cwdOpt = new Option<string?>("--cwd")
        {
            Description = "Working directory"
        };

        var noRestartOpt = new Option<bool>("--no-autorestart")
        {
            Description = "Disable auto-restart on crash"
        };

        var maxRestartsOpt = new Option<int?>("--max-restarts")
        {
            Description = "Max automatic restart attempts after crash (default: 3)"
        };

        var maxCpuOpt = new Option<double?>("--max-cpu")
        {
            Description = "Restart when CPU usage exceeds this percent"
        };

        var maxMemOpt = new Option<long?>("--max-memory")
        {
            Description = "Restart when memory exceeds this value in MB"
        };

        var outLogOpt = new Option<string?>("--out")
        {
            Description = "Stdout log file path"
        };

        var errLogOpt = new Option<string?>("--error")
        {
            Description = "Stderr log file path"
        };

        var logOpt = new Option<string?>("--log")
        {
            Description = "Combined log file path"
        };

        var timeOpt = new Option<bool>("--time")
        {
            Description = "Prefix log lines with timestamps"
        };

        var cmd = new Command("start", "Start a single process")
        {
            execArg, nameOpt, argsOpt, cwdOpt, noRestartOpt,
            maxRestartsOpt, maxCpuOpt, maxMemOpt, outLogOpt, errLogOpt, logOpt, timeOpt
        };

        cmd.SetAction(async parseResult =>
        {
            var executable = parseResult.GetValue(execArg)!;
            var name = parseResult.GetValue(nameOpt) ?? Path.GetFileNameWithoutExtension(executable);

            return await StartOneAsync(client, name, executable,
                parseResult.GetValue(argsOpt) ?? string.Empty,
                parseResult.GetValue(cwdOpt) ?? string.Empty,
                !parseResult.GetValue(noRestartOpt),
                parseResult.GetValue(maxRestartsOpt),
                parseResult.GetValue(maxCpuOpt),
                parseResult.GetValue(maxMemOpt),
                parseResult.GetValue(outLogOpt),
                parseResult.GetValue(errLogOpt),
                parseResult.GetValue(logOpt),
                parseResult.GetValue(timeOpt));
        });

        return cmd;
    }

    private static async Task<int> StartOneAsync(
        IpcClient client,
        string name,
        string executable,
        string args,
        string cwd,
        bool autoRestart,
        int? maxRestarts,
        double? maxCpu,
        long? maxMem,
        string? outLog,
        string? errLog,
        string? combinedLog,
        bool logTimestamps)
    {
        var response = await client.Send(new IpcRequest
        {
            Command = "start",
            Options = new StartOptions
            {
                Name = name,
                ExecutablePath = executable,
                Arguments = args,
                WorkingDirectory = cwd,
                AutoRestart = autoRestart,
                MaxRestartAttempts = maxRestarts ?? ProcessDefaults.MaxRestartAttempts,
                MaxCpuPercent = maxCpu,
                MaxMemoryMb = maxMem,
                OutLogFile = outLog,
                ErrorLogFile = errLog,
                CombinedLogFile = combinedLog,
                LogTimestamps = logTimestamps
            }
        });

        return CliOutput.Exit(response);
    }
}
