using NexaRun.Shared.Models;

namespace NexaRun.Shared;

public static class NexaProcessDisplay
{
    public static string StatusText(NexaProcess p) =>
        p.Status == ProcessStatus.Errored && !string.IsNullOrWhiteSpace(p.StatusReason)
            ? $"Errored — {p.StatusReason}"
            : p.Status.ToString();

    public static string ShortStatusText(NexaProcess p) =>
        p.Status == ProcessStatus.Errored && !string.IsNullOrWhiteSpace(p.StatusReason)
            ? "Errored"
            : p.Status.ToString();
}
