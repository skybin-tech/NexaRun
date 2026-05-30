using System.CommandLine;
using NexaRun.Cli;
using NexaRun.Shared.Ipc;

namespace NexaRun.Cli.Commands;

public static class RestartAllCommand
{
    public static Command Build(IpcClient client)
    {
        var cmd = new Command("restart-all", "Restart every managed process (one at a time)");

        cmd.SetAction(async _ => await RestartAllRunner.Run(client));

        return cmd;
    }
}
