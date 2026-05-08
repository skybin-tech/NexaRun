using System.CommandLine;
using NexaRun.Shared.Ipc;

namespace NexaRun.Cli.Commands;

public static class LogsCommand
{
    public static Command Build(IpcClient client)
    {
        var nameArg = new Argument<string>("name");
        nameArg.Description = "Name of the process";

        var linesOpt = new Option<int>("--lines");
        linesOpt.Description = "Number of log lines to show";

        var cmd = new Command("logs", "Show recent log output for a process") { nameArg, linesOpt };
        return cmd;
    }
}
