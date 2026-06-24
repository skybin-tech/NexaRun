using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace NexaRun.Shared;

public static class ProcessLaunchHelper
{
    public static ProcessStartInfo CreateStartInfo(
        string executable,
        string arguments,
        string workingDirectory,
        bool redirectOutput,
        IDictionary<string, string>? environment = null)
    {
        var cwd = string.IsNullOrWhiteSpace(workingDirectory)
            ? Environment.CurrentDirectory
            : workingDirectory;

        var (fileName, mergedArgs) = WrapCmdScriptIfNeeded(executable, arguments, cwd);

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = mergedArgs,
            WorkingDirectory = cwd,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        if (redirectOutput)
        {
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;
        }

        if (environment != null)
        {
            foreach (var (key, value) in environment)
                psi.Environment[key] = value;
        }

        return psi;
    }

    /// <summary>
    /// Resolves an executable on PATH (PATHEXT). Avoids cmd.exe for .exe/.com so child processes stay windowless.
    /// </summary>
    public static string ResolveExecutable(string executable, string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(executable))
            return executable;

        if (Path.IsPathRooted(executable))
        {
            if (File.Exists(executable)) return executable;
            return executable;
        }

        if (executable.Contains(Path.DirectorySeparatorChar) || executable.Contains('/'))
        {
            var combined = Path.GetFullPath(Path.Combine(workingDirectory, executable));
            if (File.Exists(combined)) return combined;
        }

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathext = Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT;.COM";
        var extensions = pathext
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var hasExt = Path.HasExtension(executable);
        var namesToTry = hasExt
            ? new[] { executable }
            : extensions.Select(ext => executable + ext).Prepend(executable).ToArray();

        foreach (var dir in pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = dir.Trim().Trim('"');
            if (trimmed.Length == 0) continue;

            foreach (var name in namesToTry)
            {
                var candidate = Path.Combine(trimmed, name);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return executable;
    }

    private static (string FileName, string Arguments) WrapCmdScriptIfNeeded(
        string executable,
        string arguments,
        string workingDirectory)
    {
        var resolved = ResolveExecutable(executable, workingDirectory);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return (resolved, arguments);

        if (resolved.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
            resolved.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
        {
            var cmd = ResolveExecutable("cmd.exe", workingDirectory);
            var inner = string.IsNullOrWhiteSpace(arguments)
                ? $"\"{resolved}\""
                : $"\"{resolved}\" {arguments}";
            return (cmd, $"/c {inner}");
        }

        return (resolved, arguments);
    }
}
