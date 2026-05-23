using System.CommandLine;
using System.Reflection;
using Spectre.Console;

namespace NexaRun.Cli.Commands;

public static class VersionCommand
{
    public static Command Build()
    {
        var cmd = new Command("version", "Show NexaRun CLI version");

        cmd.SetAction(_ =>
        {
            AnsiConsole.WriteLine(GetVersion());
            return 0;
        });

        return cmd;
    }

    public static string GetVersion()
    {
        var assembly = typeof(VersionCommand).Assembly;
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            var plus = informational.IndexOf('+', StringComparison.Ordinal);
            return plus >= 0 ? informational[..plus] : informational;
        }

        return assembly.GetName().Version?.ToString(3) ?? "unknown";
    }
}
