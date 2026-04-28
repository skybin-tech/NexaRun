using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace NexaRun.Cli.Commands;

public static class DaemonCommand
{
    public static Command Build()
    {
        var cmd = new Command("daemon", "Control the NexaRun daemon");

        var startCmd = new Command("start", "Start the NexaRun daemon");
        startCmd.SetHandler(() =>
        {
            try
            {
                var daemonExe = FindDaemonExe();
                if (daemonExe == null)
                {
                    AnsiConsole.MarkupLine("[red]✗[/] Could not locate NexaRun.Daemon executable.");
                    return;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = daemonExe,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(psi);
                AnsiConsole.MarkupLine("[green]✓[/] Daemon started.");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Failed to start daemon: {Markup.Escape(ex.Message)}");
            }
        });

        var stopCmd = new Command("stop", "Stop the NexaRun daemon");
        stopCmd.SetHandler(() =>
        {
            try
            {
                var procs = Process.GetProcessesByName("NexaRun.Daemon");
                if (procs.Length == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]Daemon is not running.[/]");
                    return;
                }

                foreach (var p in procs) p.Kill(entireProcessTree: true);
                AnsiConsole.MarkupLine("[green]✓[/] Daemon stopped.");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Failed to stop daemon: {Markup.Escape(ex.Message)}");
            }
        });

        cmd.AddCommand(startCmd);
        cmd.AddCommand(stopCmd);
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
