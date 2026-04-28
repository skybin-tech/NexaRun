using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using NexaRun.Shared.Models;

namespace NexaRun.Daemon;

public class ProcessManager
{
    private readonly List<NexaProcess> _processes = [];
    private readonly Dictionary<int, Process> _runningProcesses = [];
    private readonly Dictionary<int, StreamWriter> _logWriters = [];
    private readonly Dictionary<int, TimeSpan> _lastCpuTime = [];
    private readonly Dictionary<int, DateTime> _lastCpuSample = [];
    private readonly List<ProcessRun> _runHistory = [];
    private readonly string _dataDir;
    private readonly string _logDir;
    private readonly string _persistPath;
    private readonly string _historyPath;
    private readonly ILogger<ProcessManager> _logger;
    private int _nextId = 0;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ProcessManager(ILogger<ProcessManager> logger)
    {
        _logger = logger;
        _dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nexarun");
        _logDir = Path.Combine(_dataDir, "logs");
        _persistPath = Path.Combine(_dataDir, "processes.json");
        _historyPath = Path.Combine(_dataDir, "history.json");
        Directory.CreateDirectory(_dataDir);
        Directory.CreateDirectory(_logDir);
    }

    public async Task Load()
    {
        if (File.Exists(_historyPath))
        {
            try
            {
                var hJson = await File.ReadAllTextAsync(_historyPath);
                var loaded = JsonSerializer.Deserialize<List<ProcessRun>>(hJson);
                if (loaded != null)
                {
                    var cutoff = DateTime.UtcNow.AddDays(-7);
                    _runHistory.AddRange(loaded.Where(r => r.StartedAt >= cutoff));
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to load run history"); }
        }

        if (!File.Exists(_persistPath)) return;

        try
        {
            var json = await File.ReadAllTextAsync(_persistPath);
            var loaded = JsonSerializer.Deserialize<List<NexaProcess>>(json);
            if (loaded == null) return;

            await _lock.WaitAsync();
            try
            {
                foreach (var p in loaded)
                {
                    p.Status = ProcessStatus.Stopped;
                    p.Pid = null;

                    if (p.Id >= _nextId) _nextId = p.Id + 1;

                    // Re-attach if the OS process is still running
                    if (p.Pid.HasValue)
                    {
                        try
                        {
                            var osProc = Process.GetProcessById(p.Pid.Value);
                            if (!osProc.HasExited)
                            {
                                p.Status = ProcessStatus.Online;
                                _runningProcesses[p.Id] = osProc;
                            }
                        }
                        catch { /* process gone */ }
                    }

                    _processes.Add(p);
                }
            }
            finally { _lock.Release(); }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load persisted processes");
        }
    }

    public async Task<(bool success, string message, NexaProcess? process)> Start(StartOptions options)
    {
        await _lock.WaitAsync();
        try
        {
            var existing = _processes.FirstOrDefault(p => p.Name == options.Name);
            if (existing != null && existing.Status == ProcessStatus.Online)
                return (false, $"Process '{options.Name}' is already running.", null);

            var nexaProcess = existing ?? new NexaProcess
            {
                Id = _nextId++,
                Name = options.Name,
                AutoRestart = options.AutoRestart,
                LogFile = Path.Combine(_logDir, $"{options.Name}.log")
            };

            nexaProcess.ExecutablePath = options.ExecutablePath;
            nexaProcess.Arguments = options.Arguments;
            nexaProcess.MaxCpuPercent = options.MaxCpuPercent;
            nexaProcess.MaxMemoryMb = options.MaxMemoryMb;
            nexaProcess.WorkingDirectory = string.IsNullOrWhiteSpace(options.WorkingDirectory)
                ? Directory.GetCurrentDirectory()
                : options.WorkingDirectory;
            nexaProcess.Status = ProcessStatus.Starting;

            if (existing == null) _processes.Add(nexaProcess);

            var launchError = LaunchProcess(nexaProcess);
            if (launchError != null)
            {
                nexaProcess.Status = ProcessStatus.Errored;
                return (false, launchError, nexaProcess);
            }

            nexaProcess.StartedAt = DateTime.UtcNow;
            nexaProcess.Status = ProcessStatus.Online;
            RecordRunStart(nexaProcess.Name, nexaProcess.StartedAt);

            await Persist();
            await PersistHistory();
            return (true, $"Process '{options.Name}' started.", nexaProcess);
        }
        finally { _lock.Release(); }
    }

    public async Task<(bool success, string message)> Stop(string name)
    {
        await _lock.WaitAsync();
        try
        {
            var p = _processes.FirstOrDefault(x => x.Name == name);
            if (p == null) return (false, $"No process named '{name}' found.");
            if (p.Status != ProcessStatus.Online) return (false, $"Process '{name}' is not running.");

            p.Status = ProcessStatus.Stopping;
            await KillProcess(p, graceful: true);
            p.Status = ProcessStatus.Stopped;
            p.Pid = null;
            RecordRunEnd(name, DateTime.UtcNow, exitCode: null, ProcessRunOutcome.Stopped);

            await Persist();
            await PersistHistory();
            return (true, $"Process '{name}' stopped.");
        }
        finally { _lock.Release(); }
    }

    public async Task<(bool success, string message, NexaProcess? process)> Restart(string name)
    {
        var (stopOk, stopMsg) = await Stop(name);
        if (!stopOk && stopMsg != $"Process '{name}' is not running.")
            return (false, stopMsg, null);

        await _lock.WaitAsync();
        NexaProcess? p;
        StartOptions? opts;
        try
        {
            p = _processes.FirstOrDefault(x => x.Name == name);
            if (p == null) return (false, $"No process named '{name}' found.", null);
            opts = new StartOptions
            {
                Name = p.Name,
                ExecutablePath = p.ExecutablePath,
                Arguments = p.Arguments,
                WorkingDirectory = p.WorkingDirectory,
                AutoRestart = p.AutoRestart
            };
        }
        finally { _lock.Release(); }

        return await Start(opts);
    }

    public async Task<(bool success, string message)> Delete(string name)
    {
        await _lock.WaitAsync();
        try
        {
            var p = _processes.FirstOrDefault(x => x.Name == name);
            if (p == null) return (false, $"No process named '{name}' found.");

            if (p.Status == ProcessStatus.Online)
            {
                await KillProcess(p, graceful: true);
                RecordRunEnd(name, DateTime.UtcNow, exitCode: null, ProcessRunOutcome.Stopped);
            }

            _processes.Remove(p);
            await Persist();
            await PersistHistory();
            return (true, $"Process '{name}' deleted.");
        }
        finally { _lock.Release(); }
    }

    public async Task<List<NexaProcess>> GetAll()
    {
        await _lock.WaitAsync();
        try { return [.. _processes]; }
        finally { _lock.Release(); }
    }

    public async Task<string> GetLogs(string name, int lines = 50)
    {
        await _lock.WaitAsync();
        NexaProcess? p;
        try { p = _processes.FirstOrDefault(x => x.Name == name); }
        finally { _lock.Release(); }

        if (p == null) return $"No process named '{name}' found.";
        if (!File.Exists(p.LogFile)) return "(no logs yet)";

        var all = await File.ReadAllLinesAsync(p.LogFile);
        return string.Join(Environment.NewLine, all.TakeLast(lines));
    }

    public async Task CheckAndRestartCrashed()
    {
        await _lock.WaitAsync();
        List<NexaProcess> candidates;
        try
        {
            candidates = _processes
                .Where(p => p.Status == ProcessStatus.Online && p.AutoRestart)
                .ToList();
        }
        finally { _lock.Release(); }

        foreach (var p in candidates)
        {
            if (!_runningProcesses.TryGetValue(p.Id, out var osProc) || osProc.HasExited)
            {
                _logger.LogWarning("Process '{Name}' exited unexpectedly. Restarting...", p.Name);
                var crashedAt = DateTime.UtcNow;
                RecordRunEnd(p.Name, crashedAt, exitCode: null, ProcessRunOutcome.Crashed);
                p.Restarts++;
                int delay = Math.Min((int)Math.Pow(2, Math.Min(p.Restarts - 1, 4)) * 1000, 30000);
                await Task.Delay(delay);

                await _lock.WaitAsync();
                try
                {
                    p.Status = ProcessStatus.Starting;
                    var restartError = LaunchProcess(p);
                    if (restartError == null)
                    {
                        p.StartedAt = DateTime.UtcNow;
                        p.Status = ProcessStatus.Online;
                        RecordRunStart(p.Name, p.StartedAt);
                        _logger.LogInformation("Process '{Name}' restarted (attempt {Count})", p.Name, p.Restarts);
                    }
                    else
                    {
                        p.Status = ProcessStatus.Errored;
                        _logger.LogError("Process '{Name}' failed to restart: {Error}", p.Name, restartError);
                    }
                }
                finally { _lock.Release(); }

                await Persist();
                await PersistHistory();
            }
        }
    }

    public async Task UpdateStats()
    {
        await _lock.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;
            var cpuCount = Environment.ProcessorCount;

            foreach (var p in _processes.Where(x => x.Status == ProcessStatus.Online))
            {
                if (!_runningProcesses.TryGetValue(p.Id, out var osProc) || osProc.HasExited) continue;
                try
                {
                    osProc.Refresh();
                    p.MemoryUsage = osProc.WorkingSet64;

                    var currentCpu = osProc.TotalProcessorTime;
                    if (_lastCpuTime.TryGetValue(p.Id, out var prevCpu) &&
                        _lastCpuSample.TryGetValue(p.Id, out var prevTime))
                    {
                        var elapsed = (now - prevTime).TotalSeconds;
                        if (elapsed > 0)
                            p.CpuUsage = (currentCpu - prevCpu).TotalSeconds / (elapsed * cpuCount) * 100.0;
                    }
                    _lastCpuTime[p.Id] = currentCpu;
                    _lastCpuSample[p.Id] = now;
                }
                catch { /* process may have exited between check and refresh */ }
            }
        }
        finally { _lock.Release(); }
    }

    public async Task CheckResourceLimits()
    {
        await _lock.WaitAsync();
        List<NexaProcess> candidates;
        try
        {
            candidates = _processes
                .Where(p => p.Status == ProcessStatus.Online && (p.MaxCpuPercent.HasValue || p.MaxMemoryMb.HasValue))
                .ToList();
        }
        finally { _lock.Release(); }

        foreach (var p in candidates)
        {
            var memMb = p.MemoryUsage / 1_048_576.0;
            var cpuOver = p.MaxCpuPercent.HasValue && p.CpuUsage > p.MaxCpuPercent.Value;
            var memOver = p.MaxMemoryMb.HasValue && memMb > p.MaxMemoryMb.Value;

            if (!cpuOver && !memOver) continue;

            var reason = cpuOver
                ? $"CPU {p.CpuUsage:F1}% exceeded limit {p.MaxCpuPercent}%"
                : $"Memory {memMb:F0} MB exceeded limit {p.MaxMemoryMb} MB";

            _logger.LogWarning("Restarting '{Name}': {Reason}", p.Name, reason);

            // Log the limit breach into the process log
            await _lock.WaitAsync();
            try
            {
                if (_logWriters.TryGetValue(p.Id, out var w))
                    await w.WriteLineAsync($"[{DateTime.Now:HH:mm:ss}] NEXARUN: Restarting — {reason}");
            }
            finally { _lock.Release(); }

            var (_, _, restarted) = await Restart(p.Name);
            if (restarted != null)
                _logger.LogInformation("'{Name}' restarted due to resource limit", p.Name);
        }
    }

    public async Task<List<ProcessRun>> GetHistory(string name)
    {
        await _lock.WaitAsync();
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);
            return _runHistory
                .Where(r => r.ProcessName == name && r.StartedAt >= cutoff)
                .OrderByDescending(r => r.StartedAt)
                .ToList();
        }
        finally { _lock.Release(); }
    }

    private void RecordRunStart(string name, DateTime startedAt)
    {
        _runHistory.Add(new ProcessRun
        {
            ProcessName = name,
            StartedAt = startedAt,
            Outcome = ProcessRunOutcome.Running
        });
        // Trim history older than 7 days
        var cutoff = DateTime.UtcNow.AddDays(-7);
        _runHistory.RemoveAll(r => r.StartedAt < cutoff);
    }

    private void RecordRunEnd(string name, DateTime endedAt, int? exitCode, ProcessRunOutcome outcome)
    {
        var run = _runHistory.LastOrDefault(r => r.ProcessName == name && r.Outcome == ProcessRunOutcome.Running);
        if (run != null)
        {
            run.EndedAt = endedAt;
            run.ExitCode = exitCode;
            run.Outcome = outcome;
        }
    }

    private async Task PersistHistory()
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);
            var toSave = _runHistory.Where(r => r.StartedAt >= cutoff).ToList();
            var json = JsonSerializer.Serialize(toSave, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_historyPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist run history");
        }
    }

    private string? LaunchProcess(NexaProcess p)
    {
        try
        {
            var logStream = new FileStream(p.LogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            var logWriter = new StreamWriter(logStream) { AutoFlush = true };
            _logWriters[p.Id] = logWriter;

            ProcessStartInfo psi;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // cmd /c resolves PATH + PATHEXT exactly like a terminal
                psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{p.ExecutablePath}\" {p.Arguments}",
                    WorkingDirectory = p.WorkingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
            }
            else
            {
                psi = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"{p.ExecutablePath} {p.Arguments}\"",
                    WorkingDirectory = p.WorkingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
            }

            var osProc = new Process { StartInfo = psi, EnableRaisingEvents = true };

            osProc.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) logWriter.WriteLine($"[{DateTime.Now:HH:mm:ss}] {e.Data}");
            };
            osProc.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) logWriter.WriteLine($"[{DateTime.Now:HH:mm:ss}] ERR {e.Data}");
            };

            osProc.Start();
            osProc.BeginOutputReadLine();
            osProc.BeginErrorReadLine();

            p.Pid = osProc.Id;
            _runningProcesses[p.Id] = osProc;
            return null; // null = success
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch process '{Name}'", p.Name);
            return ex.Message;
        }
    }

    private static string ResolveExecutable(string exe)
    {
        // Already absolute or relative path with extension — use as-is
        if (Path.IsPathRooted(exe) || exe.Contains(Path.DirectorySeparatorChar) || exe.Contains('/'))
            return exe;

        // Extensions to try on Windows when none given
        var extensions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD")
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
            : [""];

        var pathDirs = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var dir in pathDirs)
        {
            // Try exact name first
            var full = Path.Combine(dir, exe);
            if (File.Exists(full)) return full;

            // Then try appending each PATHEXT extension if the exe has no extension
            if (!Path.HasExtension(exe))
            {
                foreach (var ext in extensions)
                {
                    var withExt = full + ext;
                    if (File.Exists(withExt)) return withExt;
                }
            }
        }

        // Fall back — let the OS try; will throw a useful error if not found
        return exe;
    }

    private async Task KillProcess(NexaProcess p, bool graceful)
    {
        if (!_runningProcesses.TryGetValue(p.Id, out var osProc)) return;

        try
        {
            if (!osProc.HasExited)
            {
                osProc.Kill(entireProcessTree: true);
                if (graceful) await Task.WhenAny(osProc.WaitForExitAsync(), Task.Delay(5000));
            }
        }
        catch { /* already gone */ }
        finally
        {
            _runningProcesses.Remove(p.Id);
            if (_logWriters.TryGetValue(p.Id, out var w))
            {
                w.Dispose();
                _logWriters.Remove(p.Id);
            }
        }
    }

    private async Task Persist()
    {
        try
        {
            var json = JsonSerializer.Serialize(_processes, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_persistPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist process list");
        }
    }
}
