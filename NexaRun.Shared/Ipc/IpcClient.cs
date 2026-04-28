using System.IO.Pipes;
using System.Text.Json;
using NexaRun.Shared.Models;

namespace NexaRun.Shared.Ipc;

public class IpcClient
{
    public async Task<IpcResponse> Send(IpcRequest request)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", PipeConstants.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(PipeConstants.TimeoutMs);

            using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(pipe, leaveOpen: true);

            var json = JsonSerializer.Serialize(request);
            await writer.WriteLineAsync(json);

            var response = await reader.ReadLineAsync();
            if (response == null)
                return new IpcResponse { Success = false, Message = "Daemon returned empty response." };

            return JsonSerializer.Deserialize<IpcResponse>(response)
                   ?? new IpcResponse { Success = false, Message = "Failed to parse daemon response." };
        }
        catch (TimeoutException)
        {
            return new IpcResponse { Success = false, Message = "Daemon is not running. Start it with: nexarun daemon start" };
        }
        catch (Exception ex)
        {
            return new IpcResponse { Success = false, Message = $"IPC error: {ex.Message}" };
        }
    }
}
