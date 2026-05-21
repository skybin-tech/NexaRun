using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using NexaRun.Shared;
using NexaRun.Shared.Models;

namespace NexaRun.Daemon;

public class ProcessManager
{
    private readonly List<NexaProcess> _processes = [];
    private readonly Dictionary<int, Process> _runningProcesses = [];
    private readonly Dictionary<int, ProcessLogSession> _logSessions = [];
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
        NexaRunPaths.EnsureDirectories();
        _dataDir = NexaRunPaths.DataDir;
        _logDir = NexaRunPaths.LogDir;
        _persistPath = NexaRunPaths.ProcessesFile;
        _historyPath = NexaRunPaths.HistoryFile;
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
                AutoRestart = options.AutoRestart
            };

            ApplyDefinition(nexaProcess, options);
            if (string.IsNullOrWhiteSpace(nexaProcess.WorkingDirectory))
                nexaProcess.WorkingDirectory = Directory.GetCurrentDirectory();
            nexaProcess.Status = ProcessStatus.Starting;

            if (existing == null) _processes.Add(nexaProcess);

            var launchError = LaunchProcess(nexaProcess, options.Environment);
            if (launchError != null)
            {
                nexaProcess.Status = ProcessStatus.Errored;
                return (false, launchError, nexaProcess);
            }

            nexaProcess.Restarts = 0;
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
                AutoRestart = p.AutoRestart,
                MaxRestartAttempts = p.MaxRestartAttempts,
                MaxCpuPercent = p.MaxCpuPercent,
                MaxMemoryMb = p.MaxMemoryMb,
                OutLogFile = p.OutLogFile,
                ErrorLogFile = p.ErrorLogFile,
                CombinedLogFile = p.LogFile,
                LogTimestamps = p.LogTimestamps,
                Environment = p.Environment
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

    public async Task<(bool success, string message)> ImportBatch(IReadOnlyList<StartOptions> options, bool start)
    {
        if (options.Count == 0)
            return (false, "No processes to import.");

        await _lock.WaitAsync();
        try
        {
            foreach (var opt in options)
            {
                var existing = _processes.FirstOrDefault(p => p.Name == opt.Name);
                if (existing != null)
                {
                    ApplyDefinition(existing, opt);
                    if (existing.Status != ProcessStatus.Online)
                    {
                        existing.Status = ProcessStatus.Stopped;
                        existing.Pid = null;
                    }
                }
                else
                {
                    var p = new NexaProcess
                    {
                        Id = _nextId++,
                        Name = opt.Name,
                        Status = ProcessStatus.Stopped,
                        AutoRestart = opt.AutoRestart
                    };
                    ApplyDefinition(p, opt);
                    _processes.Add(p);
                }
            }

            await Persist();
        }
        finally { _lock.Release(); }

        if (!start)
            return (true, $"Imported {options.Count} process(es). Use Start in the tray or CLI to run them.");

        var started = 0;
        var failed = 0;
        foreach (var opt in options)
        {
            var (ok, _, _) = await Start(opt);
            if (ok) started++;
            else failed++;
        }

        return (true, $"Imported {options.Count} process(es). Started {started}, failed {failed}.");
    }

    private static void ApplyDefinition(NexaProcess process, StartOptions options)
    {
        ApplyLogPaths(process, options);
        process.ExecutablePath = options.ExecutablePath;
        process.Arguments = options.Arguments;
        process.MaxCpuPercent = options.MaxCpuPercent;
        process.MaxMemoryMb = options.MaxMemoryMb;
        process.MaxRestartAttempts = options.MaxRestartAttempts > 0
            ? options.MaxRestartAttempts
            : ProcessDefaults.MaxRestartAttempts;
        process.LogTimestamps = options.LogTimestamps;
        process.Environment = options.Environment;
        process.AutoRestart = options.AutoRestart;
        process.WorkingDirectory = string.IsNullOrWhiteSpace(options.WorkingDirectory)
            ? process.WorkingDirectory
            : options.WorkingDirectory;
    }

    public async Task<string> GetLogs(string name, int lines = 50, LogStream stream = LogStream.Combined)
    {
        await _lock.WaitAsync();
        NexaProcess? p;
        try { p = _processes.FirstOrDefault(x => x.Name == name); }
        finally { _lock.Release(); }

        if (p == null) return $"No process named '{name}' found.";

        var path = ResolveLogPathForRead(p, stream);
        if (!File.Exists(path)) return "(no logs yet)";

        var all = await File.ReadAllLinesAsync(path);
        return string.Join(Environment.NewLine, all.TakeLast(lines));
    }

    private static void ApplyLogPaths(NexaProcess process, StartOptions options)
    {
        var defaultCombined = NexaRunPaths.DefaultProcessLogFile(options.Name);

        process.OutLogFile = options.OutLogFile;
        process.ErrorLogFile = options.ErrorLogFile;
        process.LogFile = options.CombinedLogFile
            ?? options.OutLogFile
            ?? defaultCombined;
        process.LogTimestamps = options.LogTimestamps;

        foreach (var path in new[] { process.LogFile, process.OutLogFile, process.ErrorLogFile })
        {
            if (string.IsNullOrWhiteSpace(path)) continue;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }
    }

    private static string ResolveLogPathForRead(NexaProcess p, LogStream stream) =>
        stream switch
        {
            LogStream.Out => p.OutLogFile ?? p.LogFile,
            LogStream.Err => p.ErrorLogFile ?? p.LogFile,
            _ => p.LogFile
        };

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
                var crashedAt = DateTime.UtcNow;
                RecordRunEnd(p.Name, crashedAt, exitCode: null, ProcessRunOutcome.Crashed);
                p.Restarts++;

                if (p.Restarts > p.MaxRestartAttempts)
                {
                    _logger.LogWarning(
                        "Process '{Name}' stopped after {Count} restart attempts (max {Max})",
                        p.Name, p.Restarts - 1, p.MaxRestartAttempts);
                    await _lock.WaitAsync();
                    try
                    {
                        p.Status = ProcessStatus.Errored;
                        p.Pid = null;
                        await WriteDaemonLogLine(p, $"NEXARUN: Stopped — exceeded max restart attempts ({p.MaxRestartAttempts})");
                    }
                    finally { _lock.Release(); }

                    await Persist();
                    await PersistHistory();
                    continue;
                }

                _logger.LogWarning(
                    "Process '{Name}' exited unexpectedly. Restarting ({Attempt}/{Max})...",
                    p.Name, p.Restarts, p.MaxRestartAttempts);

                int delay = Math.Min((int)Math.Pow(2, Math.Min(p.Restarts - 1, 4)) * 1000, 30000);
                await Task.Delay(delay);

                await _lock.WaitAsync();
                try
                {
                    p.Status = ProcessStatus.Starting;
                    var restartError = LaunchProcess(p, p.Environment);
                    if (restartError == null)
                    {
                        p.StartedAt = DateTime.UtcNow;
                        p.Status = ProcessStatus.Online;
                        RecordRunStart(p.Name, p.StartedAt);
                        _logger.LogInformation("Process '{Name}' restarted (attempt {Count}/{Max})", p.Name, p.Restarts, p.MaxRestartAttempts);
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

                    if (p.Restarts > 0 &&
                        (now - p.StartedAt).TotalSeconds >= ProcessDefaults.MinUptimeSecondsToResetRestarts)
                    {
                        p.Restarts = 0;
                    }
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
                if (_logSessions.TryGetValue(p.Id, out var session))
                    session.WriteSystem($"NEXARUN: Restarting — {reason}");
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

    private string? LaunchProcess(NexaProcess p, Dictionary<string, string>? environment = null)
    {
        try
        {
            var logSession = new ProcessLogSession(
                p.LogFile,
                p.OutLogFile,
                p.ErrorLogFile,
                p.LogTimestamps);
            _logSessions[p.Id] = logSession;

            ProcessStartInfo psi;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                psi = ProcessLaunchHelper.CreateStartInfo(
                    p.ExecutablePath,
                    p.Arguments,
                    p.WorkingDirectory,
                    redirectOutput: true,
                    environment);
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
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                if (environment != null)
                {
                    foreach (var (key, value) in environment)
                        psi.Environment[key] = value;
                }
            }

            var osProc = new Process { StartInfo = psi, EnableRaisingEvents = true };

            osProc.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) logSession.WriteStdout(e.Data);
            };
            osProc.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) logSession.WriteStderr(e.Data);
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
            if (_logSessions.TryGetValue(p.Id, out var session))
            {
                session.Dispose();
                _logSessions.Remove(p.Id);
            }
        }
    }

    private Task WriteDaemonLogLine(NexaProcess p, string message)
    {
        if (_logSessions.TryGetValue(p.Id, out var session))
            session.WriteSystem(message);
        else if (File.Exists(p.LogFile))
            File.AppendAllText(p.LogFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}{Environment.NewLine}");
        return Task.CompletedTask;
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
