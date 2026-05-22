namespace NexaRun.Shared;

public static class NexaRunPaths
{
    public static string DataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NexaRun");

    public static string LogDir { get; } = Path.Combine(DataDir, "logs");

    public static string ProcessesFile { get; } = Path.Combine(DataDir, "processes.json");

    public static string HistoryFile { get; } = Path.Combine(DataDir, "history.json");

    public static string SettingsFile { get; } = Path.Combine(DataDir, "settings.json");

    public static string DaemonLogFile { get; } = Path.Combine(LogDir, "nexarun-daemon.log");

    public static string DefaultProcessLogFile(string processName) =>
        Path.Combine(LogDir, $"{processName}.log");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(LogDir);
        MigrateLegacyData();
    }

    /// <summary>Copies data from legacy %USERPROFILE%\.nexarun if AppData folder is new.</summary>
    public static void MigrateLegacyData()
    {
        var legacyDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nexarun");

        if (!Directory.Exists(legacyDir)) return;

        foreach (var file in new[] { "processes.json", "history.json" })
        {
            var legacyPath = Path.Combine(legacyDir, file);
            var newPath = Path.Combine(DataDir, file);
            if (File.Exists(legacyPath) && !File.Exists(newPath))
                File.Copy(legacyPath, newPath);
        }

        var legacyLogs = Path.Combine(legacyDir, "logs");
        if (!Directory.Exists(legacyLogs)) return;

        foreach (var logFile in Directory.GetFiles(legacyLogs))
        {
            var dest = Path.Combine(LogDir, Path.GetFileName(logFile));
            if (!File.Exists(dest))
                File.Copy(logFile, dest);
        }
    }
}
