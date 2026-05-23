using System.IO.Pipes;
using System.Text.Json;
using NexaRun.Shared.Ipc;
using NexaRun.Shared.Models;

namespace NexaRun.Daemon;

public class IpcServer(ProcessManager processManager, ILogger<IpcServer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("IPC server listening on pipe '{Pipe}'", PipeConstants.PipeName);

        while (!stoppingToken.IsCancellationRequested)
        {
            var pipe = new NamedPipeServerStream(
                PipeConstants.PipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            try
            {
                await pipe.WaitForConnectionAsync(stoppingToken);
                _ = HandleConnection(pipe, stoppingToken);
            }
            catch (OperationCanceledException) { pipe.Dispose(); break; }
            catch (Exception ex) { logger.LogError(ex, "IPC accept error"); pipe.Dispose(); }
        }
    }

    private async Task HandleConnection(NamedPipeServerStream pipe, CancellationToken ct)
    {
        using (pipe)
        {
            try
            {
                using var reader = new StreamReader(pipe, leaveOpen: true);
                using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };

                var line = await reader.ReadLineAsync(ct);
                if (line == null) return;

                var request = JsonSerializer.Deserialize<IpcRequest>(line);
                if (request == null) return;

                var response = await Dispatch(request);
                var responseJson = JsonSerializer.Serialize(response);
                await writer.WriteLineAsync(responseJson.AsMemory(), ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "IPC handler error");
            }
        }
    }

    private async Task<IpcResponse> Dispatch(IpcRequest request)
    {
        try
        {
            return request.Command switch
            {
                "start" when request.Options != null &&
                    !string.IsNullOrWhiteSpace(request.Options.ExecutablePath) => await HandleStart(request.Options),
                "start" when !string.IsNullOrWhiteSpace(request.ProcessName) =>
                    await HandleStartExisting(request.ProcessName!),
                "update" when request.Options != null => await HandleUpdate(request.Options),
                "stop" => await HandleStop(request.ProcessName),
                "restart" => await HandleRestart(request.ProcessName, request.SettleAfterStart),
                "restart-all" => await HandleRestartAll(),
                "delete" => await HandleDelete(request.ProcessName),
                "clear-all" => await HandleClearAll(),
                "list" => await HandleList(),
                "logs" => await HandleLogs(request.ProcessName, request.LogLines ?? 50, request.LogStream),
                "history" => await HandleHistory(request.ProcessName),
                "import" when request.BatchOptions != null => await HandleImport(request.BatchOptions, request.StartAfterImport),
                "get-settings" => await HandleGetSettings(),
                "set-settings" when request.Settings != null => await HandleSetSettings(request.Settings),
                _ => new IpcResponse { Success = false, Message = $"Unknown command: {request.Command}" }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Dispatch error for command '{Command}'", request.Command);
            return new IpcResponse { Success = false, Message = ex.Message };
        }
    }

    private async Task<IpcResponse> HandleStart(StartOptions options)
    {
        var (success, message, process) = await processManager.Start(options);
        return new IpcResponse
        {
            Success = success,
            Message = message,
            Processes = process != null ? [process] : null
        };
    }

    private async Task<IpcResponse> HandleStartExisting(string target)
    {
        var (success, message, process) = await processManager.StartExisting(target);
        return new IpcResponse
        {
            Success = success,
            Message = message,
            Processes = process != null ? [process] : null
        };
    }

    private async Task<IpcResponse> HandleStop(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return new IpcResponse { Success = false, Message = "Process id or name required." };
        var (success, message) = await processManager.Stop(target);
        return new IpcResponse { Success = success, Message = message };
    }

    private async Task<IpcResponse> HandleRestart(string? target, bool settleAfterStart)
    {
        if (string.IsNullOrWhiteSpace(target))
            return new IpcResponse { Success = false, Message = "Process id or name required." };
        var (success, message, process) = await processManager.Restart(target, settleAfterStart);
        return new IpcResponse
        {
            Success = success,
            Message = message,
            Processes = process != null ? [process] : null
        };
    }

    private async Task<IpcResponse> HandleRestartAll()
    {
        var (success, message) = await processManager.RestartAllProcesses();
        var processes = await processManager.GetAll();
        return new IpcResponse { Success = success, Message = message, Processes = processes };
    }

    private async Task<IpcResponse> HandleDelete(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return new IpcResponse { Success = false, Message = "Process id or name required." };
        var (success, message) = await processManager.Delete(target);
        return new IpcResponse { Success = success, Message = message };
    }

    private async Task<IpcResponse> HandleClearAll()
    {
        var (success, message, _) = await processManager.ClearAll();
        return new IpcResponse { Success = success, Message = message, Processes = [] };
    }

    private async Task<IpcResponse> HandleList()
    {
        var processes = await processManager.GetAll();
        return new IpcResponse { Success = true, Processes = processes };
    }

    private async Task<IpcResponse> HandleLogs(string? target, int lines, string? stream)
    {
        if (string.IsNullOrWhiteSpace(target))
            return new IpcResponse { Success = false, Message = "Process id or name required." };

        var logStream = stream?.ToLowerInvariant() switch
        {
            "out" => LogStream.Out,
            "err" or "error" => LogStream.Err,
            _ => LogStream.Combined
        };

        var (success, body) = await processManager.GetLogs(target, lines, logStream);
        return new IpcResponse { Success = success, Message = success ? string.Empty : body, Logs = success ? body : null };
    }

    private async Task<IpcResponse> HandleUpdate(StartOptions options)
    {
        // Stop if running, then re-start with new settings
        var all = await processManager.GetAll();
        var existing = all.FirstOrDefault(p => p.Name == options.Name);
        if (existing?.Status == ProcessStatus.Online)
            await processManager.Stop(existing.Id.ToString());

        var (success, message, process) = await processManager.Start(options);
        return new IpcResponse { Success = success, Message = message, Processes = process != null ? [process] : null };
    }

    private async Task<IpcResponse> HandleImport(List<StartOptions> options, bool start)
    {
        var (success, message) = await processManager.ImportBatch(options, start);
        return new IpcResponse { Success = success, Message = message };
    }

    private async Task<IpcResponse> HandleGetSettings()
    {
        var settings = await processManager.GetSettings();
        return new IpcResponse { Success = true, Settings = settings };
    }

    private async Task<IpcResponse> HandleSetSettings(NexaRun.Shared.Config.NexaRunSettings settings)
    {
        var (success, message) = await processManager.SaveSettings(settings);
        return new IpcResponse { Success = success, Message = message, Settings = settings };
    }

    private async Task<IpcResponse> HandleHistory(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return new IpcResponse { Success = false, Message = "Process id or name required." };
        var (success, message, history) = await processManager.GetHistory(target);
        return new IpcResponse { Success = success, Message = message, RunHistory = history };
    }
}
