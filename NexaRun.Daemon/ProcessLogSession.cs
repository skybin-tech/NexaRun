using NexaRun.Shared;

namespace NexaRun.Daemon;

public sealed class ProcessLogSession : IDisposable
{
    private readonly bool _timestamps;
    private readonly Dictionary<string, StreamWriter> _writers = new(StringComparer.OrdinalIgnoreCase);

    public ProcessLogSession(
        string? combinedPath,
        string? outPath,
        string? errPath,
        bool timestamps)
    {
        _timestamps = timestamps;

        if (!string.IsNullOrWhiteSpace(combinedPath))
            _writers["combined"] = OpenWriter(combinedPath);

        if (!string.IsNullOrWhiteSpace(outPath))
            _writers["out"] = OpenWriter(outPath);

        if (!string.IsNullOrWhiteSpace(errPath))
            _writers["err"] = OpenWriter(errPath);
    }

    public void WriteStdout(string line)
    {
        Write("combined", line);
        Write("out", line);
    }

    public void WriteStderr(string line)
    {
        Write("combined", $"ERR {line}");
        Write("err", line);
    }

    public void WriteSystem(string line)
    {
        Write("combined", line);
    }

    private void Write(string key, string line)
    {
        if (!_writers.TryGetValue(key, out var writer)) return;

        var text = _timestamps ? $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {line}" : line;
        writer.WriteLine(text);
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

    public void Dispose()
    {
        foreach (var w in _writers.Values)
            w.Dispose();
        _writers.Clear();
    }
}
