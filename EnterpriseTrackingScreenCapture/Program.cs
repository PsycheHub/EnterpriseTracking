using EnterpriseTrackingScreenCapture;
using EnterpriseTrackingScreenCapture.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Serilog;
using System.Windows.Forms;

namespace EnterpriseTrackingScreenCapture
{
    internal class Program
    {
        [STAThread]
        static void Main()
        {
            // Configure Serilog
            var logPath = Path.Combine(AppContext.BaseDirectory, "log", "service-.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: 2 * 1024 * 1024,
                    rollOnFileSizeLimit: true)
                .CreateLogger();

            // Setup Dependency Injection
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
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Starting Enterprise Screen Capture Agent (Tray Application).");

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Prevent multiple instances
                using var mutex = new Mutex(true, "EnterpriseScreenCaptureAgent_SingleInstance", out bool createdNew);
                if (!createdNew)
                {
                    MessageBox.Show("Another instance is already running.",
                        "Screen Capture Agent", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Optional: auto‑register on first run if not already set
                if (!IsAutoStartRegistered())
                {
                    RegisterAutoStart();
                    logger.LogInformation("Auto‑start registered automatically on first run.");
                }

                // Run the application with our custom tray context
                var context = serviceProvider.GetRequiredService<TrayApplicationContext>();
                Application.Run(context);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Unhandled exception caused application termination.");
                MessageBox.Show($"Fatal error: {ex.Message}",
                    "Screen Capture Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Adds the application to the current user's startup registry key.
        /// </summary>
        public static void RegisterAutoStart()
        {
            string appPath = Application.ExecutablePath;
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            key?.SetValue("EnterpriseScreenCaptureAgent", $"\"{appPath}\"");
        }

        /// <summary>
        /// Removes the application from the current user's startup registry key.
        /// </summary>
        public static void UnregisterAutoStart()
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            key?.DeleteValue("EnterpriseScreenCaptureAgent", false);
        }

        /// <summary>
        /// Checks whether the application is currently registered for auto‑start.
        /// </summary>
        public static bool IsAutoStartRegistered()
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue("EnterpriseScreenCaptureAgent") != null;
        }
    }
}