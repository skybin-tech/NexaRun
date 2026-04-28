namespace NexaRun.Daemon;

public class DaemonWorker(ProcessManager processManager, ILogger<DaemonWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("NexaRun daemon starting");
        await processManager.Load();
        logger.LogInformation("Process list loaded");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await processManager.UpdateStats();
                await processManager.CheckAndRestartCrashed();
                await processManager.CheckResourceLimits();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Monitor loop error");
            }

            await Task.Delay(5000, stoppingToken);
        }

        logger.LogInformation("NexaRun daemon stopping");
    }
}
