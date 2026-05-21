namespace NexaRun.Shared;

public static class ProcessDefaults
{
    public const int MaxRestartAttempts = 3;

    /// <summary>After this many seconds online, the crash restart counter resets.</summary>
    public const int MinUptimeSecondsToResetRestarts = 30;

    /// <summary>Rotate log files when they exceed this size (bytes).</summary>
    public const long MaxLogFileBytes = 10 * 1024 * 1024;
}

