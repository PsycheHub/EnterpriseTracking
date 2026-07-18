using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace EnterpriseTrackingScreenCapture
{
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly ILogger<TrayApplicationContext> _logger;
        private readonly AgentService _agentService;
        private readonly NotifyIcon _notifyIcon;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private ToolStripMenuItem _autoStartMenuItem;
        private Icon? _customIcon; // Keep reference for disposal

        public TrayApplicationContext(ILogger<TrayApplicationContext> logger, AgentService agentService)
        {
            _logger = logger;
            _agentService = agentService;
            _cancellationTokenSource = new CancellationTokenSource();

            // Load the embedded icon
            _customIcon = LoadEmbeddedIcon("EnterpriseTrackingScreenCapture.Resources.app.ico");

            _notifyIcon = new NotifyIcon
            {
                Icon = _customIcon ?? SystemIcons.Application,
                Text = "Enterprise Screen Capture Agent",
                Visible = true,
                ContextMenuStrip = BuildContextMenu()
            };

            // Start the background agent
            _ = Task.Run(() => RunAgentAsync(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// Loads an embedded icon resource from the assembly.
        /// </summary>
        /// <param name="resourceName">Full namespace‑qualified resource name.</param>
        /// <returns>Icon if found; otherwise null.</returns>
        private Icon? LoadEmbeddedIcon(string resourceName)
        {
            try
            {
                var assembly = typeof(TrayApplicationContext).Assembly;
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    _logger.LogInformation("Loaded embedded icon: {ResourceName}", resourceName);
                    return new Icon(stream);
                }
                _logger.LogWarning("Embedded icon resource '{ResourceName}' not found. Using default icon.", resourceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load embedded icon '{ResourceName}'.", resourceName);
            }
            return null;
        }

        private ContextMenuStrip BuildContextMenu()
        {
            var menu = new ContextMenuStrip();

            // "Run at startup" checkable menu item
            _autoStartMenuItem = new ToolStripMenuItem("Run at startup")
            {
                CheckOnClick = true,
                Checked = Program.IsAutoStartRegistered()
            };
            _autoStartMenuItem.Click += OnToggleAutoStart;

            // Open screenshots folder
            var openFolderItem = new ToolStripMenuItem("Open Screenshots Folder");
            openFolderItem.Click += (s, e) =>
            {
                var folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screens");
                if (Directory.Exists(folder))
                    System.Diagnostics.Process.Start("explorer.exe", folder);
                else
                    MessageBox.Show("Screenshots folder not found.", "Info",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Exit
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += OnExit;

            menu.Items.Add(_autoStartMenuItem);
            menu.Items.Add(openFolderItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            return menu;
        }

        private void OnToggleAutoStart(object? sender, EventArgs e)
        {
            try
            {
                if (_autoStartMenuItem.Checked)
                {
                    Program.RegisterAutoStart();
                    _logger.LogInformation("Auto‑start enabled.");
                }
                else
                {
                    Program.UnregisterAutoStart();
                    _logger.LogInformation("Auto‑start disabled.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle auto‑start setting.");
                // Revert the check state on error
                _autoStartMenuItem.Checked = Program.IsAutoStartRegistered();
                MessageBox.Show("Unable to change startup setting. Check permissions.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task RunAgentAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting background agent...");
                _agentService.Start();

                // Keep the task alive until cancellation is requested
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Agent task cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent task encountered an error.");
            }
            finally
            {
                _agentService.Stop();
            }
        }

        private void OnExit(object? sender, EventArgs e)
        {
            _logger.LogInformation("Exiting application.");
            _cancellationTokenSource.Cancel();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Cancel();
                _customIcon?.Dispose(); // Dispose the custom icon
                _notifyIcon?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}