using NexaRun.Daemon;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nexarun", "logs", "nexarun-daemon.log"),
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddWindowsService(options => options.ServiceName = "NexaRun Daemon");
    builder.Services.AddSystemd();
    builder.Services.AddSerilog();
    builder.Services.AddSingleton<ProcessManager>();
    builder.Services.AddHostedService<DaemonWorker>();
    builder.Services.AddHostedService<IpcServer>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Daemon terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
