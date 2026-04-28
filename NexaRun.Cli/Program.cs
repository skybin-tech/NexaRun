using System.CommandLine;
using NexaRun.Cli.Commands;
using NexaRun.Shared.Ipc;

var client = new IpcClient();

var root = new RootCommand("NexaRun — PM2-inspired process manager for .NET")
{
    StartCommand.Build(client),
    StopCommand.Build(client),
    RestartCommand.Build(client),
    DeleteCommand.Build(client),
    ListCommand.Build(client),
    LogsCommand.Build(client),
    DaemonCommand.Build()
};

return await root.InvokeAsync(args);
