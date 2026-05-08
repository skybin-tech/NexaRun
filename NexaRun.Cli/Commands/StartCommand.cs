using System.CommandLine;
using NexaRun.Shared.Ipc;

namespace NexaRun.Cli.Commands;

public static class StartCommand
{
    public static Command Build(IpcClient client)
    {
        var execArg = new Argument<string>("executable");
        execArg.Description = "Executable path or command to run";

        var nameOpt = new Option<string?>("--name");
        nameOpt.Description = "Friendly name for the process";

        var argsOpt = new Option<string>("--args");
        argsOpt.Description = "Arguments to pass to the executable";

        var cwdOpt = new Option<string?>("--cwd");
        cwdOpt.Description = "Working directory";

        var noRestartOpt = new Option<bool>("--no-autorestart");
        noRestartOpt.Description = "Disable auto-restart on crash";

        var cmd = new Command("start", "Start a process and register it with the daemon")
        {
            execArg, nameOpt, argsOpt, cwdOpt, noRestartOpt
        };

        return cmd;
    }
}
