using System.Text.Json;
using NexaRun.Shared.Config;

namespace NexaRun.Shared;

public static class NexaRunSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string SettingsFile => Path.Combine(NexaRunPaths.DataDir, "settings.json");

    public static async Task<NexaRunSettings> LoadAsync()
    {
        NexaRunPaths.EnsureDirectories();
        if (!File.Exists(SettingsFile))
            return new NexaRunSettings();

        try
        {
            var json = await File.ReadAllTextAsync(SettingsFile);
            return JsonSerializer.Deserialize<NexaRunSettings>(json, JsonOptions) ?? new NexaRunSettings();
        }
        catch
        {
            return new NexaRunSettings();
        }
    }

    public static async Task SaveAsync(NexaRunSettings settings)
    {
        NexaRunPaths.EnsureDirectories();
        settings.FailedRecoveryIntervalMinutes = Math.Max(
            settings.FailedRecoveryIntervalMinutes,
            ProcessDefaults.MinFailedRecoveryIntervalMinutes);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(SettingsFile, json);
    }
}
