using System.CommandLine;
using NexaRun.Cli.Commands;
using NexaRun.Shared.Ipc;

var client = new IpcClient();

var root = new RootCommand("NexaRun — process manager for Windows")
{
    StartCommand.Build(client),
    ImportCommand.Build(client),
    StopCommand.Build(client),
    RestartCommand.Build(client),
    DeleteCommand.Build(client),
    ListCommand.Build(client),
    LogsCommand.Build(client),
    DaemonCommand.Build()
};

return root.Parse(args).Invoke();
