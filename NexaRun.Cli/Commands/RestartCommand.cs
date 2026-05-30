using System.CommandLine;
using NexaRun.Cli;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;

namespace NexaRun.Cli.Commands;

public static class RestartCommand
{
    public static Command Build(IpcClient client)
    {
        var targetArg = CliCommands.TargetArgument("restart", "or `all` to restart every process (PM2-style)");
        var cmd = new Command("restart", "Restart a process by id, name, or all") { targetArg };

        cmd.SetAction(async parseResult =>
        {
            var target = parseResult.GetValue(targetArg)!;
            if (RestartAllRunner.IsAllTarget(target))
                return await RestartAllRunner.Run(client);

            var response = await client.Send(CliCommands.TargetRequest("restart", target));
            return CliOutput.Exit(response);
        });

        return cmd;
    }
}
