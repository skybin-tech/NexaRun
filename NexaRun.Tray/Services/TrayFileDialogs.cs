using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace NexaRun.Tray.Services;

public static class TrayFileDialogs
{
    public static async Task<string?> PickOpenFile(Window? owner, string title, string? suggestedPath = null, params string[] patterns)
    {
        var (top, hidden) = await GetTopLevel(owner);
        try
        {
            var filters = patterns.Select(p =>
            {
                var parts = p.Split('|', 2);
                var exts = parts.Length > 1
                    ? parts[1].Split(';').Select(e => e.StartsWith('*') ? e : $"*{e}").ToArray()
                    : ["*"];
                return new FilePickerFileType(parts[0]) { Patterns = exts };
            }).ToList();

            var options = new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = filters
            };

            if (!string.IsNullOrEmpty(suggestedPath))
            {
                var dir = Path.GetDirectoryName(suggestedPath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    options.SuggestedStartLocation = await top.StorageProvider.TryGetFolderFromPathAsync(dir);
            }

            var files = await top.StorageProvider.OpenFilePickerAsync(options);
            if (files.Count == 0) return null;
            return files[0].TryGetLocalPath() ?? files[0].Path.LocalPath;
        }
        finally
        {
            hidden?.Close();
        }
    }

    public static async Task<string?> PickSaveFile(Window? owner, string title, string defaultName)
    {
        var (top, hidden) = await GetTopLevel(owner);
        try
        {
            var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = defaultName,
                DefaultExtension = "json",
                FileTypeChoices =
                [
                    new FilePickerFileType("NexaRun export") { Patterns = ["*.json"] }
                ]
            });

            if (file == null) return null;
            var path = file.TryGetLocalPath() ?? file.Path.LocalPath;
            return path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? path : path + ".json";
        }
        finally
        {
            hidden?.Close();
        }
    }

    private static async Task<(TopLevel Top, Window? Hidden)> GetTopLevel(Window? owner)
    {
        if (owner != null)
            return (owner, null);

        var hidden = new Window { Width = 1, Height = 1, ShowInTaskbar = false };
        hidden.Show();
        await Task.Delay(50);
        return (hidden, hidden);
    }
}
