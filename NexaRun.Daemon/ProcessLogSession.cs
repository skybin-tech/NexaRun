using NexaRun.Shared;

namespace NexaRun.Daemon;

public sealed class ProcessLogSession : IDisposable
{
    private readonly bool _timestamps;
    private readonly Dictionary<string, StreamWriter> _writersByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly string? _combinedPath;
    private readonly string? _outPath;
    private readonly string? _errPath;

    public ProcessLogSession(
        string? combinedPath,
        string? outPath,
        string? errPath,
        bool timestamps)
    {
        _timestamps = timestamps;
        _combinedPath = NormalizePath(combinedPath);
        _outPath = NormalizePath(outPath);
        _errPath = NormalizePath(errPath);

        TryOpen(_combinedPath);
        TryOpen(_outPath);
        TryOpen(_errPath);
    }

    public void WriteStdout(string line)
    {
        WriteToPath(_combinedPath, line);
        if (!PathsEqual(_combinedPath, _outPath))
            WriteToPath(_outPath, line);
    }

    public void WriteStderr(string line)
    {
        WriteToPath(_combinedPath, $"ERR {line}");
        if (!PathsEqual(_errPath, _combinedPath))
            WriteToPath(_errPath, line);
    }

    public void WriteSystem(string line) => WriteToPath(_combinedPath, line);

    private void WriteToPath(string? path, string line)
    {
        if (path == null) return;
        if (!_writersByPath.TryGetValue(path, out var writer)) return;

        try
        {
            var text = _timestamps ? $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {line}" : line;
            writer.WriteLine(text);
        }
        catch (IOException)
        {
            // Avoid crashing output handlers if the file is temporarily locked.
        }
    }

    private void TryOpen(string? path)
    {
        if (path == null || _writersByPath.ContainsKey(path)) return;

        try
        {
            _writersByPath[path] = OpenWriter(path);
        }
        catch (Exception ex)
        {
            throw new IOException($"Cannot open log file '{path}': {ex.Message}", ex);
        }
    }

    private static StreamWriter OpenWriter(string path)
    {
        RotateIfNeeded(path);

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        return new StreamWriter(stream) { AutoFlush = true };
    }

    private static void RotateIfNeeded(string path)
    {
        try
        {
            if (!File.Exists(path)) return;

            var info = new FileInfo(path);
            if (info.Length < ProcessDefaults.MaxLogFileBytes) return;

            var rotated = path + ".1";
            if (File.Exists(rotated)) File.Delete(rotated);
            File.Move(path, rotated, overwrite: true);
        }
        catch
        {
            /* rotation is best-effort */
        }
    }

    private static string? NormalizePath(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path);

    private static bool PathsEqual(string? a, string? b) =>
        a != null && b != null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    public void Dispose()
    {
        foreach (var w in _writersByPath.Values)
            w.Dispose();
        _writersByPath.Clear();
    }
}
