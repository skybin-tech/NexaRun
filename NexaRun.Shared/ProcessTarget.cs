using NexaRun.Shared.Models;

namespace NexaRun.Shared;

/// <summary>Resolve PM2-style process id or name from CLI / IPC input.</summary>
public static class ProcessTarget
{
    public static NexaProcess? TryResolve(IReadOnlyList<NexaProcess> processes, string target, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(target))
        {
            error = "Process id or name required.";
            return null;
        }

        target = target.Trim();

        if (int.TryParse(target, out var id))
        {
            var byId = processes.FirstOrDefault(p => p.Id == id);
            if (byId == null)
            {
                error = $"No process with id {id}.";
                return null;
            }

            return byId;
        }

        var byName = processes.FirstOrDefault(p => p.Name.Equals(target, StringComparison.Ordinal));
        if (byName == null)
        {
            error = $"No process named '{target}'.";
            return null;
        }

        return byName;
    }

    public static string Display(NexaProcess process) => $"[{process.Id}] {process.Name}";
}
