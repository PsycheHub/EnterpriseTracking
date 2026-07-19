using EnterpriseTrackingActivityAgent;
using EnterpriseTrackingActivityAgent.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

// Configure Serilog
var logPath = Path.Combine(AppContext.BaseDirectory, "log", "agent-.log");
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        logPath,
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 2 * 1024 * 1024,
        rollOnFileSizeLimit: true)
    .CreateLogger();

try
{
    // Setup DI
    var services = new ServiceCollection();
    services.AddLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddSerilog(dispose: true);
    });
    services.AddSingleton<ApiSender>();
    services.AddSingleton<AgentService>();
    services.AddSingleton<TrayApplicationContext>();

    var serviceProvider = services.BuildServiceProvider();

    // Prevent multiple instances
    using var mutex = new Mutex(true, "EnterpriseTrackingActivityAgent_Tray", out bool createdNew);
    if (!createdNew)
    {
        MessageBox.Show("Enterprise Activity Agent is already running.", "Info",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        return;
    }

    Log.Information("Starting Enterprise Activity Agent (Tray)...");

    // Run Windows Forms tray application
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);

    var context = serviceProvider.GetRequiredService<TrayApplicationContext>();
    Application.Run(context);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    MessageBox.Show($"Fatal error: {ex.Message}", "Error",
        MessageBoxButtons.OK, MessageBoxIcon.Error);
}
finally
{
    Log.CloseAndFlush();
}