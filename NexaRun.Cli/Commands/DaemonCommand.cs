using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace NexaRun.Cli.Commands;

public static class DaemonCommand
{
    public static Command Build()
    {
        var startCmd = new Command("start", "Start the NexaRun daemon");
        var stopCmd = new Command("stop", "Stop the NexaRun daemon");

        var cmd = new Command("daemon", "Control the NexaRun daemon")
        {
            startCmd,
            stopCmd
        };

        return cmd;
    }

    private static string? FindDaemonExe()
    {
        var cliDir = AppContext.BaseDirectory;
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "NexaRun.Daemon.exe" : "NexaRun.Daemon";

        // Side-by-side install layout
        var candidate = Path.Combine(cliDir, exeName);
        if (File.Exists(candidate)) return candidate;

        // One dir up (dev layout: bin/Debug/net10.0/)
        candidate = Path.Combine(cliDir, "..", "..", "..", "..", "NexaRun.Daemon", "bin", "Debug", "net10.0", exeName);
        candidate = Path.GetFullPath(candidate);
        if (File.Exists(candidate)) return candidate;

        return null;
    }
}
