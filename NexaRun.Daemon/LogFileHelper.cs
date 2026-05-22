using System.Text;

namespace NexaRun.Daemon;

internal static class LogFileHelper
{
    public static void AppendLine(string path, string line, bool timestamps = true)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var text = timestamps
            ? $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {line}{Environment.NewLine}"
            : $"{line}{Environment.NewLine}";

        using var stream = new FileStream(
            path,
            FileMode.Append,
            FileAccess.Write,
            FileShare.ReadWrite);
        var bytes = Encoding.UTF8.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

    public static async Task<string> ReadTailAsync(string path, int lineCount)
    {
        if (!File.Exists(path))
            return string.Empty;

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        var lines = new List<string>();
        while (await reader.ReadLineAsync() is { } line)
            lines.Add(line);

        return string.Join(Environment.NewLine, lines.TakeLast(lineCount));
    }
}
