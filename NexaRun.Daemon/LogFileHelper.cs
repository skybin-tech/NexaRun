using System.Text;

namespace NexaRun.Daemon;

internal static class LogFileHelper
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
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
        var bytes = Utf8NoBom.GetBytes(text);
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
        using var reader = new StreamReader(stream, Utf8NoBom, detectEncodingFromByteOrderMarks: true);
        var lines = new List<string>();
        while (await reader.ReadLineAsync() is { } line)
            lines.Add(line);

        return string.Join(Environment.NewLine, lines.TakeLast(lineCount));
    }

    public static void ClearFile(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.ReadWrite);
    }

    public static void DeleteRotated(string path)
    {
        var rotated = path + ".1";
        if (File.Exists(rotated))
            File.Delete(rotated);
    }

    public static IEnumerable<string> UniquePaths(params string?[] paths)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path)) continue;
            var full = Path.GetFullPath(path);
            if (seen.Add(full))
                yield return full;
        }
    }
}
